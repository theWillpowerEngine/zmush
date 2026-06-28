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
    public const string Version = "0.0.1";

    public static Settings Settings { get; private set; } = new();

    public static bool Running { get; private set; }

    public static ConcurrentDictionary<string, SessionModel> Sessions { get; private set; } = new();

    public static ConcurrentDictionary<string, List<string>> Logs { get; private set; } = new();

    public static ConcurrentDictionary<int, ZObject> Objects { get; private set; } = new();

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

        server.Start();
        Log("net", "Server running!  You can now access the server at http://localhost:" + port);

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
                break;
            }
        }

        Log("net", "Server stopping...");
        server.Stop();

        Log("ZMush has shut down safely, good night!");
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

        while (log.Count > Settings.LogCount)
            log.RemoveAt(0);
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

    public static ZObject Find(int userId, string name)
    {
        if (!Objects.TryGetValue(userId, out var user))
            return null;

        return Find(user, name);
    }

    public static ZObject? Find(ZObject user, string name)
    {
        name = name.ToLower().Trim();

        var location = user.Location;
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

        if (name == "here")
            return room;

        if (room != null && room.Name.ToLowerInvariant().StartsWith(name))
            return room;

        if (name == "me")
            return user;

        name = name.ToLowerInvariant();
        var found = Objects.Values.FirstOrDefault(o => o.Location == location && o.Name.ToLower().StartsWith(name));

        var parentage = Objects[location].GetCompleteParentage();
        for (var i = 0; i < parentage.Count; i++)
        {
            var parent = parentage[i];
            found = Objects.Values.FirstOrDefault(o => o.Location == parent.Id && o.Name.ToLower().StartsWith(name));
            if (found != null) return found;
        }

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
            Roles = dbUser.Roles.ToHashSet()
        };

        Sessions.AddOrUpdate(session.Key, session, (key, oldValue) => session);
        return session;
    }
}