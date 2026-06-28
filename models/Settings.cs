using YamlDotNet.Serialization;

public class Settings : BaseModel<Settings>
{
    public bool ShowHttpRequest = false;

    public bool LogQuotaExceeds = true;

    public int LogCount = 15;

    public int NewCharacterStartingRoom = 1;
    public int MasterRoom = 1;
    public int MasterCharacter = 2;
    public int MasterItem = -1;

    public Dictionary<HashSet<string>, HashSet<string>> CommandPerms = new Dictionary<HashSet<string>, HashSet<string>>()
    {
        { new HashSet<string>() { "@create", "@cr", "@dig" }, new HashSet<string>() { "build" } },
        { new HashSet<string>() { "@tel" }, new HashSet<string>() { "basic" } },
    };

    public Dictionary<string, HashSet<string>> Roles = new Dictionary<string, HashSet<string>>()
    {
        { "wizard", new HashSet<string>() { "build", "basic" } },
        { "moderatus", new HashSet<string>() { "basic" } }
    };

    private List<string> _protectedCommands = null;
    [YamlIgnore]
    public List<string> ProtectedCommands => _protectedCommands ??= CommandPerms.Keys.SelectMany(k => k).ToList();
}