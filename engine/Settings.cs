using YamlDotNet.Serialization;

public class Settings : BaseModel<Settings>
{
    public string Name = "zmush";

    public bool BreakOnExceptionDontUseThisUnlessYoureSmart = false;

    public bool ShowHttpRequest = false;

    public bool LogQuotaExceeds = true;

    public int AutoSaveMinutes = 5;     //0 = save in real time
    [YamlIgnore]
    public bool AutoSaveEnabled => AutoSaveMinutes > 0;

    public int NewCharacterStartingRoom = 2;
    public int MasterRoom = 1;
    public int MasterCharacter = -1;
    public int MasterItem = -1;

    public bool AutoLinkExits = true;

    //if null, everyone can invoke the editor (this doesn't change what permissions they have to in game objects just controls the UI)
    public string? PermissionRequiredForInlineEditor = "basic";

    [YamlIgnore]
    public string? OverrideSiteDirectory = null;

    public HashSet<string> UnforceableCommands = new HashSet<string>()
    {
        "@eval", "@server", "@password", "!password", "@lock", "@unlock"
    };

    public Dictionary<HashSet<string>, HashSet<string>> CommandPerms = new Dictionary<HashSet<string>, HashSet<string>>()
    {
        { new HashSet<string>() { "@create", "@cr", "@dig" }, new HashSet<string>() { "advanced" } },
        { new HashSet<string>() { "@tel", "@password", "@user" }, new HashSet<string>() { "basic" } },
    };

    public Dictionary<HashSet<Flag>, HashSet<string>> FlagPerms = new Dictionary<HashSet<Flag>, HashSet<string>>()
    {
        { new HashSet<Flag>() { Flag.Darksight }, new HashSet<string>() { "basic" } },
        { new HashSet<Flag>() { Flag.Dark }, new HashSet<string>() { "advanced" } },
        { new HashSet<Flag>() { Flag.CanForce }, new HashSet<string>() { "advanced" } },
        { new HashSet<Flag>() { Flag.ForceMajeure }, new HashSet<string>() { "admin" } },
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

    public void Save()
    {
        File.WriteAllText(Path.Combine(Engine.RootPath, "Settings"), ToYaml());
    }

    //Built-in Permissions:
    //
    //  tpother - can @tel other

    public Dictionary<string, HashSet<string>> Roles = new Dictionary<string, HashSet<string>>()
    {
        { "wizard", new HashSet<string>() { "basic", "advanced", "tpother" } },
        { "moderatus", new HashSet<string>() { "basic", "tpother" } }
    };

    private List<string>? _protectedCommands = null;
    [YamlIgnore]
    public List<string> ProtectedCommands => _protectedCommands ??= CommandPerms.Keys.SelectMany(k => k).ToList();
}