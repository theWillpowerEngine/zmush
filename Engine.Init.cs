public static partial class Engine
{
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
            Log($"Created root directory '{rootPath}', driver will be the system default.  Initializing for version {Version}.");
            File.WriteAllText(Path.Combine(rootPath, "Version"), Version);
        }

        RootPath = rootPath;

        if (!Directory.Exists(DriverPath))
        {
            Directory.CreateDirectory(DriverPath);
            Log($"No driver directory found.  Initializing to defaults...");

            File.WriteAllText(Path.Combine(DriverPath, "default.z"), Loader.GetEmbeddedResource("default.z"));
            Log($"Default driver created at '{DriverPath}default.z'.");
        }

        if (!Directory.Exists(ObjectPath))
        {
            Directory.CreateDirectory(ObjectPath);
            Log($"No object directory found.  Initializing to defaults...");

            var room = new ZObject
            {
                Id = 1,
                ZOT = ZObType.Room,
                Name = "Master Room",
                Desc = "This room is the master room.  It's the parent of every other room (by default at least).  This room is very powerful.",
                Owner = 0
            };

            room.Save();
        }

        if (!Directory.Exists(PlayerPath))
        {
            Directory.CreateDirectory(PlayerPath);
            Log($"No player directory found.  Initializing to defaults...");

            var wizard = new ZObject
            {
                Id = 0,
                ZOT = ZObType.Character,
                Name = "Stimpy",
                Desc = "The default admin character.  Very powerful.  Also adorable.",
                Owner = 0
            };

            wizard.Save();

            var wizUser = new User(0, "owner", Version);
            wizUser.SetPassword("owner");
            wizUser.Save();

            Log("CRITICAL", "Created admin user.  login: owner, password: owner");
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

    public static void Init()
    {
        Loader.LoadZObjects();
        Log("Initialization complete!  Almost there.");
    }
}