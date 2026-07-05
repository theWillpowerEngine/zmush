using YamlDotNet.Serialization;

public enum ZObType
{
    Room,
    Character,
    Item,
    Exit,
}

public enum Flag
{
    Dark,
    Darksight,

    U1, U2, U3, U4, U5, U6, U7, U8, U9, U10,
    S1, S2, S3, S4, S5, S6, S7, S8, S9, S10,
}

public class ZObject
{
    public int Id;
    public ZObType ZOT;

    public int Parent = -1;
    public int Location = -1;
    public int Quota = -1;

    public HashSet<Flag> Flags = new();
    public Dictionary<string, string> Attrs = new();

    public int Owner;
    public List<(string, string)> Locks = new();

    public HashSet<byte> Subrealities = new();

    public string Name = "A Formless Form";

    public string Desc = "";

    public static ZObject FromYaml(string yaml)
    {
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        var obj = deserializer.Deserialize<ZObject>(yaml);
        return obj;
    }

    public string ToYaml()
    {
        var yaml = new YamlDotNet.Serialization.Serializer().Serialize(this);
        return yaml;
    }

    public void Save()
    {
        if (!Engine.Objects.ContainsKey(Id))
            Engine.Objects.AddOrUpdate(Id, this, (key, oldValue) => this);

        var path = Path.Combine(Engine.ObjectPath, $"{Id}.zo");
        File.WriteAllText(path, ToYaml());
    }

    public bool CheckPermissions(int userId)
    {
        var user = Engine.Objects[userId];

        if (userId == Owner)
            return true;

        if (!Locks.Any(l => l.Item1 == "full"))
        {
            if (Locks.Any(l => l.Item1 == "public"))
                return true;

            if (Locks.Any(l => l.Item1 == "pc" && l.Item2 == $"#{userId}"))
                return true;
        }

        if (Engine.Sessions.Any(s => s.Value.UserId == userId))
        {
            var session = Engine.Sessions.FirstOrDefault(s => s.Value.UserId == userId).Value;
            if (session != null && session.Roles.Contains("admin"))
                return true;
        }

        return false;
    }

    public bool HasFlag(Flag flag, bool excludeParent = false)
    {
        if (Flags.Contains(flag))
            return true;

        if (excludeParent)
            return false;

        var parentage = GetCompleteParentage();
        foreach (var parent in parentage)
            if (parent.Flags.Contains(flag))
                return true;

        return false;
    }

    public bool IsVisibleTo(int userId)
    {
        var user = Engine.Objects[userId];
        return IsVisibleTo(user);
    }

    public bool IsVisibleTo(ZObject user)
    {
        if (user.Id == Owner)
            return true;

        if (HasFlag(Flag.Dark))
        {
            if (user.HasFlag(Flag.Darksight))
                return true;

            return false;
        }

        if (Subrealities.Count == 0 || Subrealities.Contains(0))
            return true;
        //At this point we know we have subrealities and this object is not in the base reality

        if (Subrealities.Overlaps(user.Subrealities))
            return true;

        return false;
    }

    internal List<ZObject> GetCompleteParentage()
    {
        var ret = new List<ZObject>();

        var parent = Parent;

        while (parent != -1)
        {
            if (!Engine.Objects.TryGetValue(parent, out var obj))
            {
                Engine.Log("WARN", $"Object #{Id} has a parent #{parent} that does not exist.");
                break;
            }
            ret.Add(obj);
            if (obj.Parent != -1 && obj.Parent != parent)
                parent = obj.Parent;
            else
                break;
        }

        return ret;
    }

    public bool HasLock(string lockName, string? lockVal = null)
    {
        if (lockVal == null)
            return Locks.Any(l => l.Item1 == lockName);
        else
            return Locks.Any(l => l.Item1 == lockName && l.Item2 == lockVal);
    }

    internal string? GetAttrValue(string name, bool excludeParent = false)
    {
        name = name.ToLowerInvariant();
        if (Attrs.ContainsKey(name))
            return Attrs[name];

        if (excludeParent)
            return null;

        var parentage = GetCompleteParentage();
        foreach (var parent in parentage)
        {
            if (parent.Attrs.ContainsKey(name))
                return parent.Attrs[name];
        }

        return null;
    }
}

