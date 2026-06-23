public enum ZObType
{
    Room,
    Character,
    Item
}

public class ZObject
{
    public long Id;
    public ZObType ZOT;

    public long Parent = -1;
    public long Location = -1;

    public HashSet<string> Flags = new();
    public Dictionary<string, string> Attrs = new();

    public long Owner;

    public string Name;
    public string Desc;

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
}

