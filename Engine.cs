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

    public static List<SessionModel> Sessions { get; private set; } = new();

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
            Log("Press 'X' to stop the server, or 'R' to restart it.  'L' will reload the site content without disrupting the server.");

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
        Console.WriteLine($"[{component}:{DateTime.Now.Hour:00}{DateTime.Now.Minute:00}]  {message}");
    }

    internal static SessionModel MakeSessionFor(User dbUser)
    {
        var existing = Sessions.FirstOrDefault(s => s.UserId == dbUser.Id);
        if (existing != null)
        {
            existing.LastActivity = DateTime.Now;
            return existing;
        }

        var session = new SessionModel
        {
            Key = Guid.NewGuid(),
            UserId = dbUser.Id,
            LastActivity = DateTime.Now
        };

        Sessions.Add(session);
        return session;
    }
}