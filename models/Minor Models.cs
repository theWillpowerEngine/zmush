public abstract class BaseModel<T> where T : new()
{
    public static T FromYaml(string yaml)
    {
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        var obj = deserializer.Deserialize<T>(yaml);
        return obj;
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

public class SessionModel : BaseModel<SessionModel>
{
    public Guid Key;
    public long UserId;

    public DateTime LastActivity = DateTime.Now;
}