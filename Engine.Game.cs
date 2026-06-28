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

        string? subCmd = null;
        if (kw.Contains("/") || kw.Contains("\\"))
        {
            var eles2 = kw.Split(new char[] { '/', '\\' }, 2);
            kw = eles2[0].ToLowerInvariant();
            subCmd = eles2[1].ToLowerInvariant();
        }

        //Check permissions if necessary
        if (!isAdmin && Settings.ProtectedCommands.Contains(kw))
        {
            var requiredPerms = Settings.CommandPerms.FirstOrDefault(kvp => kvp.Key.Contains(kw)).Value;
            if (requiredPerms == null || !requiredPerms.Any())
            {
                PlayerEmit(session.Key, $"You don't have permission to use the '{kw}' command.");
                Log("hax", $"User #{session.UserId} attempted to use protected command '{kw}' without permission.");
                return RenderFrame(session);
            }

            var allowedRoles = Settings.Roles.Where(r => r.Value.Any(v => requiredPerms.Contains(v))).Select(r => r.Key).ToHashSet();
            if (!session.Roles.Any(r => allowedRoles.Contains(r)))
            {
                PlayerEmit(session.Key, $"You don't have permission to use the '{kw}' command.");
                Log("hax", $"User #{session.UserId} attempted to use protected command '{kw}' without permission.  Gate 2");
                return RenderFrame(session);
            }
        }

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

                if (!o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You don't have permission to rename #{o.Id}");
                    Log("hax", $"User #{session.UserId} attempted to rename #{o.Id} without permission.");
                    break;
                }

                o.Name = s2;
                o.Save();
                PlayerEmit(session.Key, $"Renamed #{o.Id} to '{s2}'");
                break;

            case "@desc":
                (s, s2) = GetNamedValue(rest);
                o = Find(user, s);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"I can't find '{s}'");
                    break;
                }

                if (!o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You don't have permission to change the description of #{o.Id}");
                    Log("hax", $"User #{session.UserId} attempted to change the description of #{o.Id} without permission.");
                    break;
                }

                o.Desc = s2;
                o.Save();
                PlayerEmit(session.Key, $"Updated description of #{o.Id} to '{s2}'");
                break;

            case "@lock":
                (s, s2) = GetNamedValue(rest);
                o = Find(user, s);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"I can't find '{s}'");
                    break;
                }

                if (!o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You don't have permission to lock #{o.Id}");
                    Log("hax", $"User #{session.UserId} attempted to manipulate locks on #{o.Id} without permission.");
                    break;
                }

                var lockParts = s2.Split(':', 2).Select(s => s.Trim()).ToArray();
                var lp1 = lockParts[0].ToLowerInvariant();
                var lp2 = lockParts.Length > 1 ? lockParts[1] : "";

                switch (subCmd)
                {
                    case null:
                        if (string.IsNullOrEmpty(s2))
                        {
                            PlayerEmit(session.Key, $"You must specify a lock type and value.");
                            break;
                        }

                        if (o.Locks.Any(l => l.Item1 == lp1 && l.Item2 == lp2))
                        {
                            PlayerEmit(session.Key, $"Lock '{(string.IsNullOrEmpty(lp2) ? lp1 : $"{lp1}:{lp2}")}' already exists on #{o.Id}");
                            break;
                        }

                        o.Locks.Add((lp1, lp2));
                        o.Save();
                        PlayerEmit(session.Key, $"Added lock '{lp1}:{lp2}' to #{o.Id}");
                        break;

                    case "list":
                    case "l":
                        if (!o.Locks.Any())
                        {
                            PlayerEmit(session.Key, $"No locks on #{o.Id}");
                            break;
                        }
                        PlayerEmit(session.Key, $"Locks on #{o.Id}: {string.Join(", ", o.Locks.Select(l => string.IsNullOrEmpty(l.Item2) ? l.Item1 : $"{l.Item1}:{l.Item2}"))}");
                        break;

                    case "unlock":
                    case "un":
                        if (string.IsNullOrEmpty(s2))
                        {
                            PlayerEmit(session.Key, $"You must specify a lock to remove.");
                            break;
                        }

                        var lockToRemove = o.Locks.FirstOrDefault(l => l.Item1 == lp1 && l.Item2 == lp2);
                        if (lockToRemove == default)
                        {
                            PlayerEmit(session.Key, $"Lock '{(string.IsNullOrEmpty(lp2) ? lp1 : $"{lp1}:{lp2}")}' does not exist on #{o.Id}");
                            break;
                        }
                        o.Locks.Remove(lockToRemove);
                        o.Save();
                        PlayerEmit(session.Key, $"Removed lock '{(string.IsNullOrEmpty(lp2) ? lp1 : $"{lp1}:{lp2}")}' from #{o.Id}");
                        break;
                }

                break;

            case "@create":
            case "@cr":
                ZObType zot = subCmd switch
                {
                    "room" => ZObType.Room,
                    "r" => ZObType.Room,
                    "character" => ZObType.Character,
                    "c" => ZObType.Character,
                    "item" => ZObType.Item,
                    "i" => ZObType.Item,
                    _ => ZObType.Item
                };

                var loc = subCmd switch
                {
                    "room" => -1,
                    "r" => -1,
                    "character" => user.Location,
                    "c" => user.Location,
                    "item" => user.Id,
                    "i" => user.Id,
                    _ => user.Id
                };

                //TODO:  Make configurable and add master item (at least as an optional setting)
                var parent = subCmd switch
                {
                    "room" => 1,
                    "r" => 1,
                    "character" => 2,
                    "c" => 2,
                    "item" => -1,
                    "i" => -1,
                    _ => -1
                };

                var newObj = new ZObject
                {
                    Id = GetNextId(),
                    ZOT = zot,
                    Name = rest,
                    Desc = "It's still in the oven...",
                    Owner = session.UserId,
                    Location = loc,
                    Parent = parent
                };

                newObj.Save();
                PlayerEmit(session.Key, $"Created new {zot} #{newObj.Id} named '{newObj.Name}'");
                break;

            case "look":
            case "l":
                if (string.IsNullOrEmpty(rest))
                    break;

                o = Find(user, rest);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"Look at what?");
                    break;
                }
                PlayerEmit(session.Key, o.Desc);
                break;

            case "get":
            case "g":
                if (string.IsNullOrEmpty(rest))
                {
                    PlayerEmit(session.Key, $"Get what?");
                    break;
                }

                o = Find(user, rest);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"You can't find '{rest}' anywhere...");
                    break;
                }
                if (o.Location == user.Id)
                {
                    PlayerEmit(session.Key, $"You're already carrying {o.Name}");
                    break;
                }
                if (o.Location != user.Location)
                {
                    PlayerEmit(session.Key, $"You can't reach {o.Name}...");
                    break;
                }

                //TODO:  locks

                o.Location = user.Id;
                o.Save();
                RoomEmit(user.Location, $"{user.Name} picks up {o.Name}.");
                break;

            case "drop":
            case "dr":
                if (string.IsNullOrEmpty(rest))
                {
                    PlayerEmit(session.Key, $"Drop what?");
                    break;
                }

                o = Find(user, rest);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"You can't find '{rest}' anywhere...");
                    break;
                }
                if (o.Location != user.Id)
                {
                    PlayerEmit(session.Key, $"You're not carrying {o.Name}");
                    break;
                }

                //TODO:  locks

                o.Location = user.Location;
                o.Save();
                RoomEmit(user.Location, $"{user.Name} drops {o.Name} on the ground.");
                break;

            case "inventory":
            case "inv":
            case "i":
                var inv = Objects.Values.Where(o => o.Location == user.Id).ToList();
                if (!inv.Any())
                {
                    PlayerEmit(session.Key, $"You're not carrying anything.");
                    break;
                }
                PlayerEmit(session.Key, $"You are carrying: {string.Join(", ", inv.Select(o => o.Name))}");
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

        var zobs = Objects.Values.Where(o => o.Location == loc.Id && o.Id != user.Id).ToList();
        var items = zobs.Where(o => o.ZOT == ZObType.Item).ToList();
        var pcs = zobs.Where(o => o.ZOT == ZObType.Character).ToList();
        var exits = zobs.Where(o => o.ZOT == ZObType.Exit).ToList();

        ret += "<br /><br /><table width='99%'><tr><td width='33%' valign='top'>";
        if (exits.Any())
        {
            ret += "<b>Exits:</b><br />";
            foreach (var exit in exits)
            {
                ret += $"{exit.Name}<br />";
            }
        }

        ret += "</td><td width='33%' valign='top'>";
        if (pcs.Any())
        {
            ret += "<b>Players:</b><br />";
            foreach (var pc in pcs)
            {
                ret += pc.Name + "<br />";
            }
        }

        ret += "</td><td width='33%' valign='top'>";
        if (items.Any())
        {
            ret += "<b>Items:</b><br />";
            foreach (var item in items)
            {
                ret += item.Name + "<br />";
            }
        }
        ret += "</td></tr></table>";



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
        var log = Logs.GetOrAdd(session.Key, _ => new List<string>());
        var ret = log.ToArray();

        return ret;
    }
}