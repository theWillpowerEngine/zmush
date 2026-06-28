public abstract class BaseModel<T> where T : new()
{
    public static T FromYaml(string yaml)
    {
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        var obj = deserializer.Deserialize<T>(yaml);
        return obj;
    }

    public string ToYaml()
    {
        var yaml = new YamlDotNet.Serialization.Serializer().Serialize(this);
        return yaml;
    }
}

public class FrameModel : BaseModel<FrameModel>
{
    public string text;
}

public class AuthModel : BaseModel<AuthModel>
{
    public string u;
    public string p;
}

//Not a BaseModel (this one gets JSON deserialized, not YAMLfied)
public class CommandModel
{
    public string sessionId { get; set; }
    public string command { get; set; }
}

public class SessionModel : BaseModel<SessionModel>
{
    public string Key;
    public int UserId;
    public HashSet<string> Roles = new HashSet<string>();

    public DateTime LastActivity = DateTime.Now;
}