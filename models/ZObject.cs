using YamlDotNet.Serialization;

public enum ZObType
{
    Room,
    Character,
    Item
}

public enum ZFlag
{

}

public class ZObject
{
    public long Id;
    public ZObType ZOT;

    public long Parent = -1;
    public long Location = -1;
    public int Quota = -1;

    public HashSet<ZFlag> Flags = new();
    public Dictionary<string, string> Attrs = new();

    public long Owner;
    public List<(string, string)> Locks = new();

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
        var path = Path.Combine(Engine.ObjectPath, $"{Id}.zo");
        File.WriteAllText(path, ToYaml());
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
}

