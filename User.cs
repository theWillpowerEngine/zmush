using DevOne.Security.Cryptography.BCrypt;

public class AuthModel : BaseModel<AuthModel>
{
    public string u;
    public string p;
}

public abstract class BaseModel<T> where T : new()
{
    public static T FromYaml(string yaml)
    {
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        var obj = deserializer.Deserialize<T>(yaml);
        return obj;
    }
}

public class SessionModel : BaseModel<SessionModel>
{
    public Guid Key;
    public long UserId;

    public DateTime LastActivity = DateTime.Now;
}

public class User
{
    public string Name;
    public long Id;

    public string HashedPW;
    public string Salt;

    public string Version;

    public User(long id, string name, string version)
    {
        Id = id;
        Name = name;
        HashedPW = "";
        Salt = "";
        Version = version;
    }
    public User() { }

    public void SetPassword(string password)
    {
        Salt = BCryptHelper.GenerateSalt();
        HashedPW = BCryptHelper.HashPassword(password, Salt);
    }

    public bool IsPasswordValid(string password)
    {
        var hashed = BCryptHelper.HashPassword(password, Salt);
        return hashed == HashedPW;
    }

    public void Save()
    {
        var path = Path.Combine(Engine.PlayerPath, $"{Name}.zpc");
        var yaml = new YamlDotNet.Serialization.Serializer().Serialize(this);
        File.WriteAllText(path, yaml);
    }

    public static User? Load(string name)
    {
        var path = Path.Combine(Engine.PlayerPath, $"{name}.zpc");
        if (!File.Exists(path))
            return null;

        var yaml = File.ReadAllText(path);
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        return deserializer.Deserialize<User>(yaml);
    }
}