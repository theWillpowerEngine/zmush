public static partial class Engine
{
    private static (string, string) GetNamedValue(string s)
    {
        var parts = s.Split('=', 2).Select(s => s.Trim()).ToArray();
        if (parts.Length != 2)
            return (s, "");

        return (parts[0], parts[1]);
    }

    internal static string Command(SessionModel session, string command)
    {
        var user = Objects[session.UserId];
        var cmd = ZString.Eval(command, user, ref user.Quota);  //TODO:  Config gate this?

        var eles = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var kw = eles[0].ToLowerInvariant();
        var rest = string.Join(' ', eles.Skip(1));

        var isAdmin = session.Roles.Contains("admin");
        ZObject o, o2;
        string s, s2;

        if (isAdmin)
        {
            switch (kw)
            {
                case "@name":
                    (s, s2) = GetNamedValue(rest);
                    o = Find(user, s);
                    if (o == null)
                    {
                        PlayerEmit(session.Key, $"I can't find '{s}'");
                        break;
                    }

                    o.Name = s2;
                    o.Save();
                    PlayerEmit(session.Key, $"Renamed #{o.Id} to '{s2}'");

                    break;
            }
        }

        switch (kw)
        {
            case "look":
                break;

            default:
                PlayerEmit(session.Key, $"Unknown command '{kw}'");
                break;
        }


        return RenderFrame(session);
    }

    internal static string RenderFrame(SessionModel session)
    {
        var user = Objects[session.UserId];
        var loc = Objects[user.Location];

        var ret = "";
        ret += $"<b>{loc.Name}</b>%n";
        ret += loc.Desc;
        ret += "%n%n";

        var log = Logs.GetOrAdd(session.Key, _ => new List<string>());

        ret = ZString.Eval(ret, loc);

        if (log.Any())
        {
            ret += "<div id='log'>";
            ret += string.Join("<br />", log) + "<br />";
            ret += "</div>";
        }
        else
        {
            ret += "<div id='log' style='display:none;'> </div>";
        }

        return ret;
    }

    internal static string[] GetLog(SessionModel session)
    {
        PlayerEmit(session.Key, "Log was checked");

        var log = Logs.GetOrAdd(session.Key, _ => new List<string>());
        var ret = log.ToArray();

        return ret;
    }
}