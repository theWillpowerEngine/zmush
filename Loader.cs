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

    public static void LoadSiteContent()
    {
        _cache.Clear();
        CachedURLs.Clear();

        var files = Directory.GetFiles(Engine.HTMLRoot, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = file.Substring(Engine.HTMLRoot.Length).Replace(Path.DirectorySeparatorChar, '/');
            _cache.Add(relativePath, File.ReadAllBytes(file));
            CachedURLs.Add(relativePath);

            string mime = "application/octet-stream";
            var ext = Path.GetExtension(relativePath).ToLowerInvariant();
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
                _cache.Add("", File.ReadAllBytes(file));
                _cachedMimes.Add("", mime);
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