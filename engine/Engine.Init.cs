public static partial class Engine
{
    public static string RootPath { get; private set; } = "";
    public static string DriverPath => Path.Combine(RootPath, "drv") + Path.DirectorySeparatorChar;
    public static string PlayerPath => Path.Combine(RootPath, "usr") + Path.DirectorySeparatorChar;
    public static string ObjectPath => Path.Combine(RootPath, "obj") + Path.DirectorySeparatorChar;
    public static string HTMLRoot => (Settings.OverrideSiteDirectory ?? Path.Combine(RootPath, "site")) + Path.DirectorySeparatorChar;

    public static void InitDirectories(string rootPath)
    {
        if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
            rootPath += Path.DirectorySeparatorChar;

        //Create the whole directory if it doesn't exist
        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
            Log($"Created root directory '{rootPath}', driver will be the system default.  Initializing for version {Version}.");
            File.WriteAllText(Path.Combine(rootPath, "Version"), Version);
        }

        RootPath = rootPath;

        //Create the driver directory if it doesn't exist
        if (!Directory.Exists(DriverPath))
        {
            Directory.CreateDirectory(DriverPath);
            Log($"No driver directory found.  Initializing to defaults...");

            File.WriteAllText(Path.Combine(DriverPath, "main.z"), Loader.GetEmbeddedResource("default.z"));
            Log($"Default driver created at '{DriverPath}main.z'.");
        }

        //Seed default settings file if it's not there, otherwise load settings
        if (File.Exists(Path.Combine(RootPath, "Settings")))
        {
            var settings = File.ReadAllText(Path.Combine(RootPath, "Settings"));
            try
            {
                var overridePath = Settings.OverrideSiteDirectory;
                if (string.IsNullOrWhiteSpace(overridePath) || !Directory.Exists(overridePath))
                    Settings.OverrideSiteDirectory = null;

                Settings = Settings.FromYaml(settings);

                Settings.OverrideSiteDirectory = overridePath ?? Settings.OverrideSiteDirectory;
            }
            catch (Exception ex)
            {
                Log("CRITICAL", $"Failed to load settings from '{Path.Combine(RootPath, "Settings")}'.  Using defaults.  Error: {ex.Message}");
            }
        }
        else
        {
            Settings.Save();
            Log($"No settings file found.  Created default settings at '{Path.Combine(RootPath, "Settings")}'.");
        }

        //If the object directory doesn't exist, create it and seed it with the basic starter items (admin user, master room, etc.)
        if (!Directory.Exists(ObjectPath))
        {
            Directory.CreateDirectory(ObjectPath);
            Log($"No object directory found.  Initializing to defaults...");

            var room = new ZObject
            {
                Id = 0,
                ZOT = ZObType.Room,
                Name = "Master Room",
                Desc = "This room is the parent of every other room (by default at least), which means anything you put here will be accessible everywhere.  This room is very powerful, so be careful what you put here.",
                Owner = 1
            };

            room.Save(true);

            room = new ZObject
            {
                Id = 2,
                ZOT = ZObType.Room,
                Name = "Starting Room",
                Parent = 0,
                Desc = "This is the default starting room for new users.",
                Owner = 1
            };

            room.Save(true);
        }

        //If the player path doesn't exist, create it and seed the default admin user (owner/owner)
        if (!Directory.Exists(PlayerPath))
        {
            Directory.CreateDirectory(PlayerPath);
            Log($"No player directory found.  Initializing to defaults...");

            var wizard = new ZObject
            {
                Id = 1,
                ZOT = ZObType.Character,
                Name = "Stimpy",
                Desc = "The default admin character.  Very powerful.  Also adorable.",
                Owner = 1,
                Location = 2,
                Parent = -1
            };

            wizard.Save(true);

            var wizUser = new User(1, "owner", Version);
            wizUser.Roles.Add("admin");
            wizUser.SetPassword("owner");
            wizUser.Save();

            Log("CRITICAL", "Created admin user.  login: owner, password: owner");
        }

        //If the HTML/site folder doesn't exist, create it and seed it with the default site files
        if (!Directory.Exists(HTMLRoot))
        {
            Directory.CreateDirectory(HTMLRoot);
            Log($"No HTML root directory found.  Initializing to default site (hope you like it ugly)");

            File.WriteAllText(Path.Combine(HTMLRoot, "index.htm"), Loader.GetEmbeddedResource("site.index.htm"));
            File.WriteAllText(Path.Combine(HTMLRoot, "site.css"), Loader.GetEmbeddedResource("site.site.css"));
            File.WriteAllText(Path.Combine(HTMLRoot, "game.js"), Loader.GetEmbeddedResource("site.game.js"));
            File.WriteAllText(Path.Combine(HTMLRoot, "editor.js"), Loader.GetEmbeddedResource("site.editor.js"));
        }

        Loader.LoadSiteContent();
        Log("Site content is loaded and cached.");
    }

    private static void CreateDefaultFormatters()
    {
        Formatters.Clear();
        Formatters.TryAdd("red", (s) => $"<span class=\"color-red\">{s}</span>");
        Formatters.TryAdd("yellow", (s) => $"<span class=\"color-yellow\">{s}</span>");
        Formatters.TryAdd("green", (s) => $"<span class=\"color-green\">{s}</span>");
        Formatters.TryAdd("blue", (s) => $"<span class=\"color-blue\">{s}</span>");
        Formatters.TryAdd("purple", (s) => $"<span class=\"color-purple\">{s}</span>");
        Formatters.TryAdd("orange", (s) => $"<span class=\"color-orange\">{s}</span>");
        Formatters.TryAdd("cyan", (s) => $"<span class=\"color-cyan\">{s}</span>");

        Formatters.TryAdd("bold", (s) => $"<b>{s}</b>");
        Formatters.TryAdd("italic", (s) => $"<i>{s}</i>");
    }

    private static void LoadDriver()
    {
        var files = Directory.GetFiles(DriverPath, "*.z", SearchOption.AllDirectories).ToList();
        var entryPoint = files.FirstOrDefault(f => f.EndsWith("main.z", StringComparison.OrdinalIgnoreCase));
        if (entryPoint == null) entryPoint = files.FirstOrDefault();
        if (entryPoint == null) return;

        files.Remove(entryPoint);

        var masterUser = Objects[0];
        if (masterUser == null)
        {
            Log("CRITICAL", "No master user found.  This is a fatal error.  Please restore your system from backup.");
            return;
        }

        var code = File.ReadAllText(entryPoint);
        var res = ZString.Eval(code, masterUser);

        foreach (var file in files)
        {
            code = File.ReadAllText(file);
            res = ZString.Eval(code, masterUser);
        }
    }

    public static void Init()
    {
        Loader.LoadZObjects();
        LoadDriver();
        CreateDefaultFormatters();
        Log("Initialization complete!  Almost there.");

        var ids = Objects.Keys.ToList();
        ids.Sort();
        NextId = ids.Last() + 1;

        int cur = -1;
        while (ids.Count > 0)
        {
            cur++;
            if (ids[0] == cur)
            {
                ids.RemoveAt(0);

                continue;
            }

            FreeIds.Add(cur);
        }
    }
}