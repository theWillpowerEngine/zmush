public class Registers
{
    public int ActorId;
    public string ActorName = "";

    public string[] Numbered;

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

    public string ApplyToString(string s)
    {
        for (int i = 0; i < Numbered.Length; i++)
        {
            s = s.Replace($"%{i + 1}", Numbered[i]);
        }

        s = s.Replace("%an", ActorName);
        s = s.Replace("%a", ActorId.ToString());

        return s;
    }
}