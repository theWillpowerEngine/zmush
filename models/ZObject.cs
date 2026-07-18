using System.Runtime.InteropServices;
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
    Handler,
    CanForce,
    ForceMajeure,
    NukeSafe,
    SetAndGet,
    Sealed,
    Teleporter,

    U1, U2, U3, U4, U5, U6, U7, U8, U9, U10,
    S1, S2, S3, S4, S5, S6, S7, S8, S9, S10,
}

public class ZObject
{
    public int Id;
    public ZObType ZOT;

    public bool? Male = null;       //null = it

    public int Parent = -1;
    public int Location = -1;
    public int Quota = -1;

    public HashSet<Flag> Flags = new();
    public List<Attr> Attrs = new();

    public int Owner;
    public List<(string, string)> Locks = new();

    public HashSet<byte> Subrealities = new();

    public string Name = "A Formless Form";

    public string Desc = "";

    public static ZObject FromYaml(string yaml)
    {
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        try
        {
            var obj = deserializer.Deserialize<ZObject>(yaml);
            return obj;
        }
        catch (Exception ex)
        {
            Engine.Log("CRITICAL", $"Failed to deserialize ZObject from YAML: {ex.Message}");
            Engine.Log("CRITICAL", $"YAML Content: {yaml}");
            throw;
        }
    }

    public string ToYaml()
    {
        var yaml = new YamlDotNet.Serialization.Serializer().Serialize(this);
        return yaml;
    }

    public void Save(bool force = false)
    {
        if (!Engine.Objects.ContainsKey(Id))
            Engine.Objects.AddOrUpdate(Id, this, (key, oldValue) => this);

        if (force || !Engine.Settings.AutoSaveEnabled)
        {
            var path = Path.Combine(Engine.ObjectPath, $"{Id}.zo");
            File.WriteAllText(path, ToYaml());
        }
        else
            Workers.QueueForSave(this);
    }

    public bool CheckPermissions(int objectId, ZObject? overrideZO = null)
    {
        if (!Engine.Objects.TryGetValue(objectId, out var o))
            return false;

        return CheckPermissions(o, overrideZO);
    }

    public bool CheckPermissions(ZObject o, ZObject? executor = null)
    {
        if (o.Id == Owner)
            return true;

        if (!Locks.Any(l => l.Item1 == "full"))
        {
            if (Locks.Any(l => l.Item1 == "public"))
                return true;

            if (Locks.Any(l => l.Item1 == "id" && l.Item2.Split('|', ' ').Any(s => s.TrimStart("#").ToString() == o.Id.ToString())))
                return true;
        }

        if (Engine.IsAdminUser(o.Id))
            return true;

        if (o.HasFlag(Flag.SetAndGet))
            return true;

        if (executor != null)
        {
            if (CheckPermissions(executor, null))
                return true;
        }

        //Not sure if this does anything since IsAdminUser looks at sessions, but we might need this logic at some point...
        // if (o.ZOT != ZObType.Character)
        //     return CheckPermissions(o.Owner, executor);

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

    internal void ApplyParentage()
    {
        if (Parent < 0) return;
        if (!Engine.Objects.TryGetValue(Parent, out var parent))
            return;

        var templateAttrs = parent.Attrs.Where(a => a.HasLock("template")).ToList();
        foreach (var templateAttr in templateAttrs)
        {
            if (!Attrs.Any(a => a.Name == templateAttr.Name))
            {
                Attrs.Add(new Attr
                {
                    Name = templateAttr.Name,
                    Value = templateAttr.Value,
                    Locks = [.. templateAttr.Locks.ToList()]
                });
            }
        }
    }

    public bool HasLock(string lockName, string? lockVal = null)
    {
        if (lockVal == null)
            return Locks.Any(l => l.Item1 == lockName);
        else
            return Locks.Any(l => l.Item1 == lockName && l.Item2 == lockVal);
    }

    internal string? GetMatchingFunctionAttr(string name, int parms, bool excludeParent = false)
    {
        name = name.ToLowerInvariant();

        var attr = Attrs.FirstOrDefault(a => Matcher.IsMatchingFunction(a.Name.ToLowerInvariant(), name, parms));
        if (attr != null)
            return attr.Name;

        if (excludeParent)
            return null;

        var parentage = GetCompleteParentage();
        foreach (var parent in parentage)
        {
            attr = parent.Attrs.FirstOrDefault(a => Matcher.IsMatchingFunction(a.Name.ToLowerInvariant(), name, parms));
            if (attr != null)
                return attr.Name;
        }
        return null;
    }

    internal string? GetAttrValue(string name, bool excludeParent = false)
    {
        name = name.ToLowerInvariant();

        var attr = Attrs.FirstOrDefault(a => a.Name.ToLowerInvariant() == name);
        if (attr != null)
            return attr.Value;

        if (excludeParent)
            return null;

        var parentage = GetCompleteParentage();

        foreach (var parent in parentage)
        {
            attr = parent.Attrs.FirstOrDefault(a => a.Name.ToLowerInvariant() == name);
            if (attr != null)
            {
                //Check if we're out of sync with the template
                if (attr.HasLock("template"))
                    ApplyParentage();

                return attr.Value;
            }
        }

        return null;
    }

    internal string? GetAndEvalAttrValue(string name, Registers? registers = null, bool excludeParent = false)
    {
        int quota = -1;
        return GetAndEvalAttrValue(name, ref quota, registers, excludeParent);
    }

    internal string? GetAndEvalAttrValue(string name, ref int quota, Registers? registers = null, bool excludeParent = false)
    {
        name = name.ToLowerInvariant();

        var attr = Attrs.FirstOrDefault(a => a.Name.ToLowerInvariant() == name);
        if (attr != null)
            return ZString.Eval(attr.Value, this, ref quota, registers);

        if (excludeParent)
            return null;

        var parentage = GetCompleteParentage();
        foreach (var parent in parentage)
        {
            attr = parent.Attrs.FirstOrDefault(a => a.Name.ToLowerInvariant() == name);
            if (attr != null)
                return ZString.Eval(attr.Value, this, ref quota, registers);
        }

        return null;
    }

    internal void SetAttrValue(string name, string value)
    {
        var attr = Attrs.FirstOrDefault(a => a.Name.ToLowerInvariant() == name.ToLowerInvariant());
        if (attr != null)
            attr.Value = value;
        else
            Attrs.Add(new Attr { Name = name.ToLowerInvariant(), Value = value });
    }
}

