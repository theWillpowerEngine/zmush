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
                }

                break;

            case "@unlock":
                (s, s2) = GetNamedValue(rest);
                o = Find(user, s);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"I can't find '{s}'");
                    break;
                }

                if (!o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You don't have permission to unlock #{o.Id}");
                    Log("hax", $"User #{session.UserId} attempted to unlock #{o.Id} without permission.");
                    break;
                }

                var unlockParts = s2.Split(':', 2).Select(s => s.Trim()).ToArray();
                var up1 = unlockParts[0].ToLowerInvariant();
                var up2 = unlockParts.Length > 1 ? unlockParts[1] : "";

                if (string.IsNullOrEmpty(s2))
                {
                    PlayerEmit(session.Key, $"You must specify a lock to remove.");
                    break;
                }

                var lockToRemove = o.Locks.FirstOrDefault(l => l.Item1 == up1 && l.Item2 == up2);
                if (lockToRemove == default)
                {
                    PlayerEmit(session.Key, $"Lock '{(string.IsNullOrEmpty(up2) ? up1 : $"{up1}:{up2}")}' does not exist on #{o.Id}");
                    break;
                }
                o.Locks.Remove(lockToRemove);
                o.Save();

                PlayerEmit(session.Key, $"Removed lock '{(string.IsNullOrEmpty(up2) ? up1 : $"{up1}:{up2}")}' from #{o.Id}");
                break;

            case "@flag":
                (s, s2) = GetNamedValue(rest);
                o = Find(user, s);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"I can't find '{s}'");
                    break;
                }

                if (!o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You don't have permission to flag #{o.Id}");
                    Log("hax", $"User #{session.UserId} attempted to manipulate flags on #{o.Id} without permission.");
                    break;
                }

                var unset = s2.StartsWith("!");
                if (unset)
                    s2 = s2.Substring(1);

                if (!Enum.TryParse<Flag>(s2, true, out var flag))
                {
                    PlayerEmit(session.Key, $"'{s2}' is not a valid flag.");
                    break;
                }

                var requiredRoles = Settings.RolesRequiredForFlag(flag);
                if (requiredRoles != null && !session.Roles.Any(r => requiredRoles.Contains(r)))
                {
                    PlayerEmit(session.Key, $"You don't have permission to set the '{flag}' flag.");
                    Log("hax", $"User #{session.UserId} attempted to set the '{flag}' flag on #{o.Id} without permission.");
                    break;
                }

                if (o.HasFlag(flag) && !unset)
                {
                    PlayerEmit(session.Key, $"#{o.Id} already has the '{flag}' flag.  If you want to unset it, use: @flag #{o.Id} !{flag}");
                    break;
                }
                else if (!o.HasFlag(flag) && unset)
                {
                    PlayerEmit(session.Key, $"#{o.Id} does not have the '{flag}' flag.  If you want to set it, use: @flag #{o.Id} {flag}");
                    break;
                }

                if (unset)
                {
                    o.Flags.Remove(flag);
                    o.Save();
                    PlayerEmit(session.Key, $"Removed '{flag}' flag from #{o.Id}");
                }
                else
                {
                    o.Flags.Add(flag);
                    o.Save();
                    PlayerEmit(session.Key, $"Added '{flag}' flag to #{o.Id}");
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
                    "exit" => ZObType.Exit,
                    "x" => ZObType.Exit,
                    "ex" => ZObType.Exit,
                    "item" => ZObType.Item,
                    "i" => ZObType.Item,
                    _ => ZObType.Item
                };

                var loc = subCmd switch
                {
                    "room" => -1,
                    "r" => -1,
                    "character" => Settings.NewCharacterStartingRoom,
                    "c" => Settings.NewCharacterStartingRoom,
                    "exit" => user.Location,
                    "x" => user.Location,
                    "ex" => user.Location,
                    "item" => user.Id,
                    "i" => user.Id,
                    _ => user.Id
                };

                ZObject? farSide = null;
                var name = rest;
                if (zot == ZObType.Exit)
                {
                    if (rest.StartsWith("#"))
                    {
                        if (int.TryParse(rest.Substring(1), out var id))
                        {
                            farSide = Objects.GetValueOrDefault(id);
                            if (farSide == null)
                            {
                                PlayerEmit(session.Key, $"I can't find #{id} to create an exit to.");
                                break;
                            }
                        }
                        else
                        {
                            PlayerEmit(session.Key, $"Invalid exit target '{rest}'");
                            break;
                        }
                    }
                    else
                    {
                        farSide = Objects.Values.FirstOrDefault(z => z.ZOT == ZObType.Room && z.Name.ToLowerInvariant().Contains(rest));
                        if (farSide == null)
                        {
                            PlayerEmit(session.Key, $"I can't find '{rest}' to create an exit to.");
                            break;
                        }
                    }

                    name = farSide.Name;
                }

                var parent = subCmd switch
                {
                    "room" => Settings.MasterRoom,
                    "r" => Settings.MasterRoom,
                    "character" => Settings.MasterCharacter,
                    "c" => Settings.MasterCharacter,
                    "exit" => farSide?.Id ?? -1,
                    "x" => farSide?.Id ?? -1,
                    "ex" => farSide?.Id ?? -1,
                    "item" => Settings.MasterItem,
                    "i" => Settings.MasterItem,
                    _ => Settings.MasterItem
                };

                var newObj = new ZObject
                {
                    Id = GetNextId(),
                    ZOT = zot,
                    Name = name,
                    Desc = "It's still in the oven...",
                    Owner = session.UserId,
                    Location = loc,
                    Parent = parent
                };

                newObj.Save();
                PlayerEmit(session.Key, $"Created new {zot} #{newObj.Id} named '{newObj.Name}'");
                break;

            case "@dig":
                var oneSidedExit = subCmd == "1";
                o = new ZObject
                {
                    Id = GetNextId(),
                    ZOT = ZObType.Room,
                    Name = rest,
                    Desc = "It's still in the oven...",
                    Owner = session.UserId,
                    Location = -1,
                    Parent = Settings.MasterRoom
                };
                o.Save();
                var newRoomId = o.Id;

                o2 = new ZObject
                {
                    Id = GetNextId(),
                    ZOT = ZObType.Exit,
                    Name = rest,
                    Desc = "",
                    Owner = session.UserId,
                    Location = user.Location,
                    Parent = newRoomId
                };
                o2.Save();

                if (!oneSidedExit)
                {
                    o = new ZObject
                    {
                        Id = GetNextId(),
                        ZOT = ZObType.Exit,
                        Name = Objects[user.Location].Name,
                        Desc = "",
                        Owner = session.UserId,
                        Location = newRoomId,
                        Parent = user.Location
                    };
                    o.Save();

                    PlayerEmit(session.Key, $"Created new room #{newRoomId} named '{rest}' with exits to and from #{user.Location}");
                }
                else
                    PlayerEmit(session.Key, $"Created new room #{newRoomId} named '{rest}' with exit from #{user.Location}");
                break;

            case "@tel":
                if (rest.StartsWith("#"))
                {
                    if (int.TryParse(rest.Substring(1), out var id))
                    {
                        o = Objects.GetValueOrDefault(id);
                        if (o == null)
                        {
                            PlayerEmit(session.Key, $"I can't find #{id} to teleport to.");
                            break;
                        }

                        if (o.ZOT != ZObType.Room)
                        {
                            PlayerEmit(session.Key, $"You can only teleport to rooms.");
                            break;
                        }
                    }
                    else
                    {
                        PlayerEmit(session.Key, $"Invalid teleport target '{rest}'");
                        break;
                    }
                }
                else
                {
                    o = Objects.Values.FirstOrDefault(z => z.ZOT == ZObType.Room && z.Name.ToLowerInvariant().Contains(rest));
                    if (o == null)
                    {
                        PlayerEmit(session.Key, $"I can't find '{rest}' to teleport to.");
                        break;
                    }
                }

                RoomEmit(user.Location, $"{user.Name} disappears in a puff of smoke.");
                user.Location = o.Id;
                user.Save();
                RoomEmit(user.Location, $"{user.Name} appears in a puff of smoke.");
                Log("admin", $"{user.Name} (#{user.Id}) teleported to #{o.Id} '{o.Name}'");
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

                if (o.HasLock("fixed") && !o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You can't pick up {o.Name}.");
                    break;
                }

                if (o.HasLock("static"))
                {
                    PlayerEmit(session.Key, $"You can't pick up {o.Name}, it's static.");
                    break;
                }

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

                if (o.HasLock("fixed") && !o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You can't drop {o.Name}.");
                    break;
                }

                if (o.HasLock("static"))
                {
                    PlayerEmit(session.Key, $"You can't drop {o.Name}, it's static.");
                    break;
                }

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
                var exit = Objects.Values.FirstOrDefault(o => o.ZOT == ZObType.Exit && o.Location == user.Location && o.Name.ToLowerInvariant().Contains(kw));
                if (exit != null)
                {
                    var dest = Objects.GetValueOrDefault(exit.Parent);
                    if (dest != null)
                    {
                        RoomEmit(user.Location, $"{user.Name} leaves, heading toward {dest.Name}.");
                        user.Location = dest.Id;
                        user.Save();
                        RoomEmit(user.Location, $"{user.Name} arrives.");
                    }
                    else
                    {
                        PlayerEmit(session.Key, $"The exit {exit.Name} seems to lead nowhere...");
                    }
                    break;
                }
                else
                {
                    Log("hax", $"User #{session.UserId} attempted unknown command '{kw}'");
                }

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
        var items = zobs.Where(o => o.ZOT == ZObType.Item).Where(o => o.IsVisibleTo(user)).ToList();
        var pcs = zobs.Where(o => o.ZOT == ZObType.Character).Where(o => o.IsVisibleTo(user)).ToList();
        var exits = zobs.Where(o => o.ZOT == ZObType.Exit).Where(o => o.IsVisibleTo(user)).ToList();

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
        ret += "</td></tr></table><br />";

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