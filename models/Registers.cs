public class Let
{
    public string Name;
    public string Value;
    public int Scope;

    public Let(string name, string value, int scope)
    {
        Name = name;
        Value = value;
        Scope = scope;
    }
}

public class Registers
{
    public int ActorId;
    public string ActorName = "";

    public string[] Numbered;

    public List<Let> LetScope = new();
    private int CurrentLetScope = 1;

    public Registers(List<string> registers, ZObject actor)
    {
        Numbered = registers.ToArray();
        ActorId = actor.Id;
        ActorName = actor.Name;
    }
    public Registers(ZObject actor)
    {
        Numbered = new string[0];
        ActorId = actor.Id;
        ActorName = actor.Name;
    }

    public void AdvanceLetScope()
    {
        CurrentLetScope++;
    }

    public void EndLetScope()
    {
        CurrentLetScope--;
        if (CurrentLetScope <= 0)
            throw new Exception("Let scope ended too many times");

        var letsToRemove = LetScope.Where(l => l.Scope == CurrentLetScope + 1).ToList();
        foreach (var let in letsToRemove)
        {
            LetScope.Remove(let);
        }
    }

    public void Let(string name, string value)
    {
        var current = LetScope.FirstOrDefault(l => l.Name == name);
        if (current != null)
        {
            current.Value = value;
            return;
        }

        LetScope.Add(new Let(name, value, CurrentLetScope));
    }

    public string ApplyToString(string s)
    {
        for (int i = 0; i < Numbered.Length; i++)
        {
            s = s.Replace($"%{i + 1}", Numbered[i]);
        }

        s = s.Replace("%an", ActorName);
        s = s.Replace("%a", ActorId.ToString());

        var actor = Engine.Objects[ActorId];
        s = s.Replace("%l", actor.Location.ToString());

        var pronoun = actor.Male switch
        {
            true => "he",
            false => "she",
            null => "it"
        };
        s = s.Replace("%as", pronoun);

        pronoun = actor.Male switch
        {
            true => "him",
            false => "her",
            null => "it"
        };
        s = s.Replace("%ao", pronoun);

        pronoun = actor.Male switch
        {
            true => "his",
            false => "hers",
            null => "its"
        };
        s = s.Replace("%ap", pronoun);

        return s;
    }
}