public class Attr
{
    public string Name = "";
    public string Value = "";

    public List<(string, string)> Locks = new();

    public bool HasLock(string lockName, string? lockVal = null)
    {
        if (lockVal == null)
            return Locks.Any(l => l.Item1 == lockName);
        else
            return Locks.Any(l => l.Item1 == lockName && l.Item2 == lockVal);
    }

    public void AddOrSetLock(string name, string val)
    {
        if (HasLock(name))
            Locks.RemoveAll(l => l.Item1 == name);

        Locks.Add((name, val));
    }

    public bool RemoveLock(string name)
    {
        if (!HasLock(name))
            return false;

        Locks.RemoveAll(l => l.Item1 == name);
        return true;
    }

    public bool CanSet(ZObject target, ZObject actor, ZObject? executor = null)
    {
        if (!Locks.Any()) return true;

        if (HasLock("owner"))
            return actor.Id == target.Owner;

        if (HasLock("id"))
        {
            if (PDL.FindIndex(Locks.Single(l => l.Item1 == "id").Item2, actor.Id.ToString()) > 0)
                return true;

            if (executor != null)
                if (PDL.FindIndex(Locks.Single(l => l.Item1 == "id").Item2, executor.Id.ToString()) > 0)
                    return true;

            return false;
        }

        // if (Engine.IsAdminUser(actor.Id))
        //     return true;

        return true;
    }
}