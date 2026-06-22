using NetCoreServer;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public static class Engine
{
    public const string Version = "0.0.1";

    public static Settings Settings { get; private set; } = new();

    public static bool Running { get; private set; }
    public static string RootPath { get; private set; }
    public static string DriverPath => Path.Combine(RootPath, "drv") + Path.DirectorySeparatorChar;
    public static string PlayerPath => Path.Combine(RootPath, "usr") + Path.DirectorySeparatorChar;
    public static string ObjectPath => Path.Combine(RootPath, "obj") + Path.DirectorySeparatorChar;
    public static string HTMLRoot => Path.Combine(RootPath, "site") + Path.DirectorySeparatorChar;

    public static void InitDirectories(string rootPath)
    {
        if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
            rootPath += Path.DirectorySeparatorChar;

        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
            Log($"Created root directory '{rootPath}', driver will be the system default.");
        }

        RootPath = rootPath;

        if (!Directory.Exists(DriverPath))
        {
            Directory.CreateDirectory(DriverPath);
            Log($"No driver directory found.  Initializing to defaults...");

            File.WriteAllText(Path.Combine(DriverPath, "default.z"), Loader.GetEmbeddedResource("default.z"));
            Log($"Default driver created at '{DriverPath}default.z'.");
        }

        if (!Directory.Exists(PlayerPath))
        {
            Directory.CreateDirectory(PlayerPath);
            Log($"No player directory found.  Initializing to defaults...");

            //File.WriteAllText(Path.Combine(PlayerPath, "owner.z"), Loader.GetEmbeddedResource("owner.z"));
            //TODO:  Create whatever object goes in this file and YAML it out.

            Log("CRITICAL", "Created default player: owner, password: owner");
        }

        if (!Directory.Exists(ObjectPath))
        {
            Directory.CreateDirectory(ObjectPath);
            Log($"No object directory found.  Initializing to defaults...");

            File.WriteAllText(Path.Combine(ObjectPath, "0.zo"), Loader.GetEmbeddedResource("0.zo"));
            File.WriteAllText(Path.Combine(ObjectPath, "1.zo"), Loader.GetEmbeddedResource("1.zo"));
        }

        if (!Directory.Exists(HTMLRoot))
        {
            Directory.CreateDirectory(HTMLRoot);
            Log($"No HTML root directory found.  Initializing to default site (hope you like it ugly)");

            File.WriteAllText(Path.Combine(HTMLRoot, "index.htm"), Loader.GetEmbeddedResource("site.index.htm").Replace("{ZMVER}", Engine.Version));
        }

        Loader.LoadSiteContent();
        Log("Site content is loaded and cached.");
    }

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
        Console.WriteLine($"{message}");
    }
    public static void Log(string component, string message)
    {
        Console.WriteLine($"[{component}]  {message}");
    }
}