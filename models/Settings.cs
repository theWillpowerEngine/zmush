using YamlDotNet.Serialization;

public class Settings : BaseModel<Settings>
{
    public bool BreakOnExceptionDontUseThisUnlessYoureSmart = false;

    public bool ShowHttpRequest = false;

    public bool LogQuotaExceeds = true;

    public int LogCount = 15;

    public int NewCharacterStartingRoom = 1;
    public int MasterRoom = 1;
    public int MasterCharacter = 2;
    public int MasterItem = -1;

    public bool AutoLinkExits = true;

    public string? OverrideSiteDirectory = null;

    public Dictionary<HashSet<string>, HashSet<string>> CommandPerms = new Dictionary<HashSet<string>, HashSet<string>>()
    {
        { new HashSet<string>() { "@create", "@cr", "@dig" }, new HashSet<string>() { "advanced" } },
        { new HashSet<string>() { "@tel", "@password" }, new HashSet<string>() { "basic" } },
    };

    public Dictionary<HashSet<Flag>, HashSet<string>> FlagPerms = new Dictionary<HashSet<Flag>, HashSet<string>>()
    {
        { new HashSet<Flag>() { Flag.Darksight }, new HashSet<string>() { "basic" } },
        { new HashSet<Flag>() { Flag.Dark }, new HashSet<string>() { "advanced" } },
    };

    public HashSet<string>? RolesRequiredForFlag(Flag flag)
    {
        var ret = new HashSet<string>();
        foreach (var kvp in FlagPerms)
        {
            if (kvp.Key.Contains(flag))
                ret.UnionWith(kvp.Value);
        }

        if (!ret.Any()) return null;
        return ret;
    }

    public Dictionary<string, HashSet<string>> Roles = new Dictionary<string, HashSet<string>>()
    {
        { "wizard", new HashSet<string>() { "basic", "advanced" } },
        { "moderatus", new HashSet<string>() { "basic" } }
    };

    private List<string> _protectedCommands = null;
    [YamlIgnore]
    public List<string> ProtectedCommands => _protectedCommands ??= CommandPerms.Keys.SelectMany(k => k).ToList();
}