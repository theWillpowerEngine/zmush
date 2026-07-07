using NetCoreServer;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public static partial class Engine
{
    public const string Version = "0.0.2";

    public static Settings Settings { get; private set; } = new();

    public static Scheduler Scheduler { get; private set; } = new();

    public static bool Running { get; private set; }

    public static ConcurrentDictionary<string, SessionModel> Sessions { get; private set; } = new();

    public static ConcurrentDictionary<string, List<string>> Logs { get; private set; } = new();

    public static ConcurrentDictionary<int, ZObject> Objects { get; private set; } = new();

    public static ConcurrentDictionary<string, Func<string, string>> Formatters { get; private set; } = new();

    private static HashSet<int> FreeIds = new();
    private static int NextId;

    public static void Run(int port)
    {
        if (string.IsNullOrEmpty(RootPath))
            throw new Exception("Server root path not initialized.  Call Server.InitDirectories() before starting the server.");

        Log("net", $"Starting server on port {port} in working directory '{RootPath}'...");

        // Create a new HTTP server
        var server = new HttpGameServer(IPAddress.Any, port);
        server.AddStaticContent(HTMLRoot);

        Workers.RegisterInitialWorkers(Scheduler);
        Scheduler.Start();

        server.Start();
        Log("net", "Server running!  You can now access the server at http://localhost:" + port);

        var deleted = false;

        for (; ; )
        {
            Log("Press 'X' to stop the server, or 'R' to restart it.  'L' will reload the site content without disrupting the server.  '\\' will shut down AND delete all your files so don't press that unless you really want to.");

            string cmd = Console.ReadKey(true).KeyChar.ToString().ToUpperInvariant();

            if (cmd == "X")
                break;

            if (cmd == "R")
            {
                server.Restart();
                Log("net", "Server was manually restarted.");
                continue;
            }

            if (cmd == "L")
            {
                Loader.LoadSiteContent();
                Log("dev", "Site content reloaded.");
                continue;
            }

            if (cmd == "\\")
            {
                Log("dev", "Deleting driver directory before shutting down...");
                Directory.Delete(RootPath, true);
                deleted = true;
                break;
            }
        }

        Log("WARN", "zmush is shutting down!");
        Log("net", "HTTP Server stopping...");
        server.Stop();

        Scheduler.Stop(true, true);

        if (!deleted && Settings.AutoSaveEnabled)
        {
            Log("Performing final AutoSave");
            Workers.AutoSave();
        }

        Log("zmush has shut down safely, good night!");
    }

    public static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now.Hour:00}{DateTime.Now.Minute:00}]  {message}");
    }
    public static void Log(string component, string message)
    {
        Console.WriteLine($"[{DateTime.Now.Hour:00}{DateTime.Now.Minute:00}:{component}]  {message}");
    }

    public static void PlayerEmit(string sessionId, string message)
    {
        var log = Logs.GetOrAdd(sessionId.ToString(), _ => new List<string>());
        log.Add(message);

        while (log.Count > 100)
            log.RemoveAt(0);
    }
    public static void PlayerEmit(ZObject pc, string message)
    {
        if (pc.ZOT != ZObType.Character)
            throw new Exception($"PlayerEmit called with non-PC object {pc.Id} of type {pc.ZOT}");

        var session = Sessions.Values.FirstOrDefault(s => s.UserId == pc.Id);
        if (session == null)
        {
            Log("dev", $"PlayerEmit called for PC {pc.Id} but no session was found for that user.");
            return;
        }

        var log = Logs.GetOrAdd(session.Key, _ => new List<string>());
        log.Add(message);

        while (log.Count > 100)
            log.RemoveAt(0);
    }

    public static void RoomEmit(int roomId, string message)
    {
        var sessions = Sessions.Values.Where(s => Objects.TryGetValue(s.UserId, out var obj) && obj.Location == roomId).ToList();
        sessions.ForEach(s => PlayerEmit(s.Key, message));
    }

    private static object IdLock = new object();
    public static int GetNextId()
    {
        lock (IdLock)
        {
            if (FreeIds.Count > 0)
            {
                var id = FreeIds.First();
                FreeIds.Remove(id);
                return id;
            }
            else
            {
                return NextId++;
            }
        }
    }
    public static void DeleteObject(int id)
    {
        lock (IdLock)
        {
            if (Objects.TryRemove(id, out var obj))
            {
                File.Delete(Path.Combine(Engine.ObjectPath, $"{id}.zo"));
                FreeIds.Add(id);
            }
            else
                Log("WARN", $"Failed to delete object {id}.");
        }
    }

    public static List<ZObject> GetObjectsInScope(ZObject scope, bool includeRoomHierarchy = false)
    {
        var ret = new List<ZObject>();
        ret.Add(scope);

        List<int> locationsInScope = new List<int>() { scope.Id };

        switch (scope.ZOT)
        {
            case ZObType.Room:
                if (includeRoomHierarchy)
                    locationsInScope.AddRange(scope.GetCompleteParentage().Select(p => p.Id));
                break;

            case ZObType.Character:
                locationsInScope.Add(scope.Location);
                if (includeRoomHierarchy)
                    locationsInScope.AddRange(Objects[scope.Location].GetCompleteParentage().Select(p => p.Id));
                break;

            case ZObType.Item:
                locationsInScope.Add(scope.Location);
                var loc = Objects[scope.Location];
                if (loc.ZOT == ZObType.Character)
                {
                    locationsInScope.Add(loc.Location);
                    if (includeRoomHierarchy)
                        locationsInScope.AddRange(Objects[loc.Location].GetCompleteParentage().Select(p => p.Id));
                }
                break;

            case ZObType.Exit:
                locationsInScope.Add(scope.Location);
                locationsInScope.Add(scope.Parent);
                if (includeRoomHierarchy)
                {
                    locationsInScope.AddRange(Objects[scope.Location].GetCompleteParentage().Select(p => p.Id));
                    locationsInScope.AddRange(Objects[scope.Parent].GetCompleteParentage().Select(p => p.Id));
                }
                break;

            default:
                throw new Exception($"Unknown ZObType {scope.ZOT} for object {scope.Id} in GetObjectsInScope");
        }

        ret.AddRange(Objects.Values.Where(o => locationsInScope.Contains(o.Id) || locationsInScope.Contains(o.Location)));
        return ret;
    }

    public static ZObject? Find(int userId, string name, bool localRoomOnly = false)
    {
        if (!Objects.TryGetValue(userId, out var user))
            return null;

        return Find(user, name, localRoomOnly);
    }

    public static ZObject? Find(ZObject context, string name, bool localRoomOnly = false)
    {
        name = name.ToLower().Trim();

        var location = context.ZOT == ZObType.Room ? context.Id : context.Location;
        if (!Objects.TryGetValue(location, out var room))
            room = null;

        if (name.StartsWith("#"))
        {
            if (int.TryParse(name.Substring(1), out var id))
            {
                if (Objects.TryGetValue(id, out var obj))
                    return obj;
            }
        }
        else if (int.TryParse(name, out var id))
        {
            if (Objects.TryGetValue(id, out var obj))
                return obj;
        }

        if (name == "this" || name == "self" || name == "me")
            return context;

        if (name == "here")
            return room;

        if (room != null && room.Name.ToLowerInvariant().StartsWith(name))
            return room;

        var found = Objects.Values.FirstOrDefault(o => o.Location == location && o.Name.ToLower().StartsWith(name));
        if (found != null) return found;

        var inScope = GetObjectsInScope(context, !localRoomOnly);
        found = inScope.FirstOrDefault(o => o.Name.ToLower().StartsWith(name));
        if (found != null) return found;

        return null;
    }

    public static ZObject? GlobalFind(ZObject context, string name)
    {
        name = name.ToLower().Trim();

        if (name.StartsWith("#"))
        {
            if (int.TryParse(name.Substring(1), out var id))
            {
                if (Objects.TryGetValue(id, out var obj))
                    return obj;
            }
        }
        else if (int.TryParse(name, out var id))
        {
            if (Objects.TryGetValue(id, out var obj))
                return obj;
        }

        if (name == "this" || name == "self" || name == "me" || name == "here")
            return context;

        var found = Objects.Values.FirstOrDefault(o => o.Name.ToLower() == name);
        if (found != null) return found;

        return null;
    }

    internal static SessionModel MakeSessionFor(User dbUser)
    {
        var existing = Sessions.Values.FirstOrDefault(s => s.UserId == dbUser.Id);
        if (existing != null)
        {
            existing.LastActivity = DateTime.Now;
            return existing;
        }

        var session = new SessionModel
        {
            Key = Guid.NewGuid().ToString(),
            UserId = dbUser.Id,
            LastActivity = DateTime.Now,
            LoginName = dbUser.Name,
            Roles = dbUser.Roles.ToHashSet()
        };

        Sessions.AddOrUpdate(session.Key, session, (key, oldValue) => session);
        return session;
    }
}