using YamlDotNet.Serialization;

public class Settings : BaseModel<Settings>
{
    public bool ShowHttpRequest = false;

    public bool LogQuotaExceeds = true;

    public int LogCount = 15;

    public Dictionary<HashSet<string>, HashSet<string>> CommandPerms = new Dictionary<HashSet<string>, HashSet<string>>()
    {
        { new HashSet<string>() { "@create", "@cr" }, new HashSet<string>() { "create" } },
    };

    public Dictionary<string, HashSet<string>> Roles = new Dictionary<string, HashSet<string>>()
    {
        { "wizard", new HashSet<string>() { "create" } }
    };

    private List<string> _protectedCommands = null;
    [YamlIgnore]
    public List<string> ProtectedCommands => _protectedCommands ??= CommandPerms.Keys.SelectMany(k => k).ToList();
}