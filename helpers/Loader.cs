using System.Text;

public static class Loader
{
    public static string GetEmbeddedResource(string lastPartOfName)
    {
        string name = $"zmush.res.{lastPartOfName}";

        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream == null)
            throw new Exception($"Embedded resource '{name}' not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static Dictionary<string, byte[]> _cache = new Dictionary<string, byte[]>();
    private static Dictionary<string, string> _cachedMimes = new Dictionary<string, string>();
    public static HashSet<string> CachedURLs { get; private set; } = new();

    public static void LoadZObjects()
    {
        var files = Directory.GetFiles(Engine.ObjectPath, "*.zo", SearchOption.AllDirectories);

        Engine.Log($"Loading {files.Length} ZObjects...");
        var start = DateTime.Now;
        Engine.Objects.Clear();

        //TODO:  Parallel foreach this?
        foreach (var file in files)
        {
            var yaml = File.ReadAllText(file);
            var obj = ZObject.FromYaml(yaml);

            var retry = 0;
            while (!Engine.Objects.TryAdd(obj.Id, obj))
            {
                retry++;
                if (retry > 3)
                    throw new Exception($"Failed to add object with ID {obj.Id} to Engine.Objects after 10 retries.  Check for duplicate IDs in your .zo files.  Object: {Environment.NewLine}{yaml}");
            }
        }
        Engine.Log($"Load Complete.  Total ZObjects loaded: {Engine.Objects.Count} in {(DateTime.Now - start).TotalMilliseconds} ms.");
    }

    public static void LoadSiteContent()
    {
        _cache.Clear();
        CachedURLs.Clear();
        _cachedMimes.Clear();

        var files = Directory.GetFiles(Engine.HTMLRoot, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = file.Substring(Engine.HTMLRoot.Length).Replace(Path.DirectorySeparatorChar, '/');
            var ext = Path.GetExtension(relativePath).ToLowerInvariant();

            var content = File.ReadAllBytes(file);

            //Replace merge tags in text files
            if (new[] { ".html", ".htm", ".css", ".js", ".txt" }.Contains(ext))
            {
                var contentAsString = Encoding.UTF8.GetString(content);
                contentAsString = contentAsString ?? "";

                contentAsString = contentAsString.Replace("~~ZMVER~~", Engine.Version);
                contentAsString = contentAsString.Replace("~~Title~~", Engine.Settings.Name);
                contentAsString = contentAsString.Replace("~~CanEditByDefault~~", string.IsNullOrWhiteSpace(Engine.Settings.PermissionRequiredForInlineEditor) ? "1" : "0");

                content = Encoding.UTF8.GetBytes(contentAsString);
            }

            _cache.Add(relativePath, content);
            CachedURLs.Add(relativePath);

            string mime = "application/octet-stream";
            switch (ext)
            {
                case ".html":
                case ".htm": mime = "text/html"; break;
                case ".css": mime = "text/css"; break;
                case ".js": mime = "application/javascript"; break;
                case ".png": mime = "image/png"; break;
                case ".jpg":
                case ".jpeg": mime = "image/jpeg"; break;
                case ".gif": mime = "image/gif"; break;
                case ".svg": mime = "image/svg+xml"; break;
                case ".json": mime = "application/json"; break;
            }
            _cachedMimes.Add(relativePath, mime);

            //Cache index as "/"
            if (relativePath.StartsWith("index.htm"))
            {
                _cache.Add("", content);
                _cachedMimes.Add("", "text/html");
                CachedURLs.Add("");
            }
        }
    }

    internal static (byte[] content, string mime) GetContentAndMime(string route)
    {
        if (_cache.TryGetValue(route, out var content) && _cachedMimes.TryGetValue(route, out var mime))
            return (content, mime);
        else
            throw new Exception($"Content for route '{route}' not found in cache.  Check Loader.CachedURLs first before you call GetContentAndMime");
    }
}