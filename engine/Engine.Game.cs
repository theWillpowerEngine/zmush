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
        var cmd = command;    //ZString.Eval(command, user, ref user.Quota);  //TODO:  Config gate this?

        var eles = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var kw = eles[0].ToLowerInvariant();
        var rest = string.Join(' ', eles.Skip(1));

        //Command aliases
        if (command.StartsWith("\""))
        {
            kw = "say";
            rest = command.Substring(1);
        }
        else if (command.StartsWith(":"))
        {
            kw = "emote";
            rest = command.Substring(1);
        }
        else if (command.StartsWith(";"))
        {
            kw = "emote/ns";
            rest = command.Substring(1);
        }

        var isAdmin = session.Roles.Contains("admin");
        ZObject? o, o2;
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
                    if (s2.ToLowerInvariant() == "male" || s2.ToLowerInvariant() == "m")
                    {
                        o.Male = true;
                        PlayerEmit(session.Key, $"#{o.Id} is now male.");
                        break;
                    }
                    else if (s2.ToLowerInvariant() == "female" || s2.ToLowerInvariant() == "f")
                    {
                        o.Male = false;
                        PlayerEmit(session.Key, $"#{o.Id} is now female.");
                        break;
                    }
                    else if (s2.ToLowerInvariant() == "neuter" || s2.ToLowerInvariant() == "n")
                    {
                        o.Male = null;
                        PlayerEmit(session.Key, $"#{o.Id} is now neuter.");
                        break;
                    }

                    PlayerEmit(session.Key, $"'{s2}' is not a valid flag.");
                    break;
                }

                var requiredRoles = Settings.RolesRequiredForFlag(flag);
                if (requiredRoles != null && !session.Roles.Contains("admin") && !session.Roles.Any(r => requiredRoles.Contains(r)))
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

            case "@attr":
                var clear = !rest.Contains("=") || subCmd == "clear" || subCmd == "c";

                (s, s2) = GetNamedValue(rest);
                var attrParts = s.Split(".", 2).Select(s => s.Trim()).ToArray();

                o = Find(user, attrParts[0]);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"I can't find '{attrParts[0]}'");
                    break;
                }

                if (!o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You don't have permission to set attributes on #{o.Id}");
                    Log("hax", $"User #{session.UserId} attempted to set the '{attrParts[1]}' attribute on #{o.Id} without permission.");
                    break;
                }

                if (subCmd == "list" || subCmd == "l")
                {
                    var attrList = "{bold Attributes for #" + o.Id + ":}%n";
                    foreach (var a in o.Attrs)
                    {
                        attrList += $"%t{a.Name} = {a.Value}%n";
                    }
                    session.SpecialOutput.AppendLine(attrList.Replace("{", "%{").Replace("}", "%}"));
                    break;
                }

                var attrName = attrParts.Length > 1 ? attrParts[1].ToLowerInvariant() : "";
                if (string.IsNullOrWhiteSpace(attrName))
                {
                    PlayerEmit(session.Key, $"You must specify an attribute name.  Example: @attr object.attribute=value");
                    break;
                }

                Attr? attr;

                switch (subCmd)
                {
                    case "val":
                    case "v":
                        attr = o.Attrs.FirstOrDefault(attr => attr.Name == attrName);
                        if (attr != null)
                        {
                            PlayerEmit(session.Key, $"#{o.Id} attribute '{attrName}' = '{attr.Value}'");
                        }
                        else
                        {
                            PlayerEmit(session.Key, $"#{o.Id} does not have an attribute named '{attrName}'");
                        }
                        break;

                    case "clear":
                    case "c":
                    default:
                        attr = o.Attrs.FirstOrDefault(a => a.Name == attrName);
                        if (attr != null)
                        {
                            if (clear)
                            {
                                o.Attrs.Remove(attr);
                                o.Save();
                                PlayerEmit(session.Key, $"Removed '{attrName}' attribute from #{o.Id}");
                            }
                            else
                            {
                                attr.Value = s2;
                                o.Save();
                                PlayerEmit(session.Key, $"Set '{attrName}' attribute on #{o.Id} to '{s2}'");
                            }
                        }
                        else
                        {
                            if (clear)
                            {
                                PlayerEmit(session.Key, $"#{o.Id} does not have an attribute named '{attrName}'.  If you want to set it, use: @attr object.attribute=value");
                            }
                            else
                            {
                                var newAttr = new Attr { Name = attrName, Value = s2 };
                                o.Attrs.Add(newAttr);
                                o.Save();
                                PlayerEmit(session.Key, $"Set '{attrName}' attribute on #{o.Id} to '{s2}'");
                            }
                        }
                        break;
                }
                break;

            case "@create":
            case "@cr":
                //@cr/u <name>[:<pwd>]=#<character id>
                if (subCmd == "user" || subCmd == "u")
                {
                    (s, s2) = GetNamedValue(rest);
                    if (!s2.StartsWith("#") || !int.TryParse(s2.Substring(1), out var charId))
                    {
                        PlayerEmit(session.Key, $"You must specify a character to create the user with.  Example: @cr/u <name>[:<pwd>]=#<character id>");
                        break;
                    }
                    var character = Objects.GetValueOrDefault(charId);
                    if (character == null || character.ZOT != ZObType.Character)
                    {
                        PlayerEmit(session.Key, $"I can't find character #{charId}");
                        break;
                    }

                    if (s.Contains(":"))
                    {
                        var parts = s.Split(':', 2);
                        s = parts[0];
                        s2 = parts[1];
                    }
                    else
                    {
                        s2 = s;
                    }

                    if (User.Load(s) != null)
                    {
                        PlayerEmit(session.Key, $"A user with the name '{s}' already exists.");
                        break;
                    }

                    var newUser = new User(charId, s, Version);
                    newUser.SetPassword(s2);
                    newUser.Save();
                    PlayerEmit(session.Key, $"Created new user '{s}' with character #{charId}, password: '{s2}'");
                    Log("admin", $"User #{session.UserId} created new user '{s}' with character #{charId}");
                    break;
                }

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
                var oneSidedExit = subCmd?.Contains("1") ?? false;
                var tpAfterDig = subCmd?.Contains("t") ?? false;

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

                    if (tpAfterDig)
                    {
                        user.Location = newRoomId;
                        user.Save();
                    }
                }
                else
                    PlayerEmit(session.Key, $"Created new room #{newRoomId} named '{rest}' with exit from #{user.Location}");
                break;

            case "@nuke":
                o = subCmd == "global" || subCmd == "g" ? GlobalFind(user, rest) : Find(user, rest);
                if (o == null)
                {
                    PlayerEmit(session.Key, $"I can't find '{rest}'");
                    break;
                }

                if (!isAdmin && !o.CheckPermissions(session.UserId))
                {
                    PlayerEmit(session.Key, $"You don't have permission to nuke #{o.Id}");
                    Log("hax", $"User #{session.UserId} attempted to nuke #{o.Id} without permission.");
                    break;
                }

                if (o.HasFlag(Flag.NukeSafe))
                {
                    PlayerEmit(session.Key, $"#{o.Id} is marked as nuke-safe.");
                    break;
                }

                DeleteObject(o.Id);
                PlayerEmit(session.Key, $"Nuked #{o.Id} '{o.Name}'");
                Log($"User #{session.UserId} nuked {o.ZOT} #{o.Id}, '{o.Name}'");
                break;

            case "@tel":
                int telId = int.MinValue;
                if (rest.StartsWith("#") || int.TryParse(rest, out telId))
                {
                    if (telId == int.MinValue)
                        if (!int.TryParse(rest.Substring(1), out telId))
                        {
                            PlayerEmit(session.Key, $"Invalid Id:  {rest}");
                            break;
                        }

                    o = Objects.GetValueOrDefault(telId);
                    if (o == null)
                    {
                        PlayerEmit(session.Key, $"I can't find #{telId} to teleport to.");
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
                    o = Objects.Values.FirstOrDefault(z => z.ZOT == ZObType.Room && z.Name.ToLowerInvariant().Contains(rest.ToLowerInvariant()));
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

            case "@password":
                (s, s2) = GetNamedValue(rest);
                var dbU = User.Load(s);
                if (dbU == null)
                {
                    PlayerEmit(session.Key, $"I can't find user '{s}'");
                    break;
                }
                dbU.SetPassword(s2);
                dbU.Save();
                PlayerEmit(session.Key, $"Updated password for user '{s}'");
                Log("admin", $"User #{session.UserId} updated password for user '{s}'");
                break;

            case "@user":
                (s, s2) = GetNamedValue(rest);
                var dbU3 = User.Load(s);

                //Creating a user?
                if (string.IsNullOrEmpty(subCmd))
                {
                    if (dbU3 != null)
                    {
                        PlayerEmit(session.Key, $"A user with the name '{s}' already exists.");
                        break;
                    }

                    var newPC = new ZObject
                    {
                        Id = GetNextId(),
                        ZOT = ZObType.Character,
                        Name = s,
                        Desc = "A brand new character, be nice to them!",
                        Owner = session.UserId,
                        Location = Settings.NewCharacterStartingRoom,
                        Parent = Settings.MasterCharacter
                    };

                    newPC.Owner = newPC.Id;  //Owner is self for new characters
                    newPC.Save(true);

                    dbU3 = new User(newPC.Id, s, Version);
                    dbU3.SetPassword(s2);
                    dbU3.Save();
                    PlayerEmit(session.Key, $"Created new user '{s}' with character #{newPC.Id}, password: '{s2}'");
                    Log($"User #{session.UserId} created new user '{s}' with character #{newPC.Id}");
                    break;
                }

                if (dbU3 == null && subCmd != "list" && subCmd != "l")
                {
                    PlayerEmit(session.Key, $"I can't find user '{s}'.  Try @user/list [<search>]");
                    break;
                }

                switch (subCmd)
                {
                    case "roles":
                    case "r":
                        PlayerEmit(session.Key, $"User '{s}' has roles: {string.Join(", ", dbU3.Roles)}");
                        break;

                    case "enrole":
                    case "en":
                        if (string.IsNullOrEmpty(s2))
                        {
                            PlayerEmit(session.Key, $"You must specify a role to add.  Example: @user/en <name>=<role>");
                            break;
                        }

                        if (!Settings.Roles.ContainsKey(s2))
                        {
                            PlayerEmit(session.Key, $"Role '{s2}' does not exist.");
                            break;
                        }

                        if (!isAdmin && !session.Roles.Contains(s2))
                        {
                            PlayerEmit(session.Key, $"You don't have permission to add the '{s2}' role to other users.");
                            Log("hax", $"User #{session.UserId} attempted to add the '{s2}' role to user '{s}' without permission.");
                            break;
                        }

                        dbU3.Roles.Add(s2);
                        dbU3.Save();
                        PlayerEmit(session.Key, $"Added role '{s2}' to user '{s}'");
                        Log($"User #{session.UserId} added role '{s2}' to user '{s}'");
                        break;

                    case "derole":
                    case "unrole":
                    case "un":
                    case "de":
                        if (string.IsNullOrEmpty(s2))
                        {
                            PlayerEmit(session.Key, $"You must specify a role to remove.  Example: @user/un <name>=<role>");
                            break;
                        }

                        if (!Settings.Roles.ContainsKey(s2))
                        {
                            PlayerEmit(session.Key, $"Role '{s2}' does not exist.");
                            break;
                        }

                        if (!isAdmin && !session.Roles.Contains(s2))
                        {
                            PlayerEmit(session.Key, $"You don't have permission to remove the '{s2}' role from other users.");
                            Log("hax", $"User #{session.UserId} attempted to remove the '{s2}' role from user '{s}' without permission.");
                            break;
                        }

                        if (!dbU3.Roles.Contains(s2))
                        {
                            PlayerEmit(session.Key, $"User '{s}' does not have the '{s2}' role.");
                            break;
                        }

                        dbU3.Roles.Remove(s2);
                        dbU3.Save();
                        PlayerEmit(session.Key, $"Removed role '{s2}' from user '{s}'");
                        Log($"User #{session.UserId} removed role '{s2}' from user '{s}'");
                        break;

                    case "list":
                    case "l":
                        var users = Directory.GetFiles(Engine.PlayerPath, "*.zpc").Select(f => Path.GetFileNameWithoutExtension(f)).OrderBy(f => f).ToList();
                        if (!string.IsNullOrEmpty(s))
                            users = users.Where(u => u.ToLowerInvariant().Contains(s.ToLowerInvariant())).ToList();
                        PlayerEmit(session.Key, $"Users: {string.Join(", ", users)}");
                        break;

                    default:
                        PlayerEmit(session.Key, $"Unknown subcommand '{subCmd}'.  Valid subcommands: roles, enrole, derole, list");
                        break;
                }

                break;

            case "@eval":
                if (!rest.StartsWith("{"))
                    rest = "{" + rest + "}";

                var evaled = ZString.Eval(rest, user, ref user.Quota);
                PlayerEmit(session.Key, $"Result: {evaled}");
                break;

            case "@examine":
            case "@ex":
                o = Find(user, rest);
                if (o == null)
                    o = GlobalFind(user, rest);

                if (o == null)
                {
                    PlayerEmit(session.Key, $"I can't find '{rest}'");
                    break;
                }

                session.SpecialOutput.AppendLine($"[bold {o.Name} (#{o.Id})]%n");
                session.SpecialOutput.AppendLine(o.Desc + "%n");
                if (o.Parent >= 0)
                    session.SpecialOutput.AppendLine($"%t[bold Parent:] #{o.Parent}%n");
                session.SpecialOutput.AppendLine($"%t[bold Owner:] #{o.Owner}%n");
                if (o.Flags.Any())
                    session.SpecialOutput.AppendLine($"%t[bold Flags:] {string.Join(", ", o.Flags)}%n");
                break;

            case "!password":
                (s, s2) = GetNamedValue(rest);
                var dbU2 = User.Load(session.LoginName);
                if (dbU2 == null)
                {
                    PlayerEmit(session.Key, $"I can't find your user record.  This should never happen.");
                    Log("CRITICAL", $"User #{session.UserId} has no user record.  This should never happen.");
                    break;
                }
                if (!dbU2.IsPasswordValid(s))
                {
                    PlayerEmit(session.Key, $"Current password is incorrect.");
                    break;
                }

                dbU2.SetPassword(s2);
                dbU2.Save();
                PlayerEmit(session.Key, $"Updated your password");
                break;

            case "!exit":
            case "!ex":
                PlayerEmit(session.Key, $"Signed out!  The screen will refresh in a moment...");
                Sessions.TryRemove(session.Key, out _);
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

            case "say":
                if (string.IsNullOrEmpty(rest))
                {
                    PlayerEmit(session.Key, $"Say what?");
                    break;
                }
                RoomEmit(user.Location, $"{user.Name} says, \"{rest}\"");
                break;

            case "emote":
            case "em":
                if (string.IsNullOrEmpty(rest))
                {
                    PlayerEmit(session.Key, $"Emote what?");
                    break;
                }
                if (subCmd == "ns")
                    RoomEmit(user.Location, $"{user.Name}{rest}");
                else
                    RoomEmit(user.Location, $"{user.Name} {rest}");
                break;

            default:
                //Exits name?
                var exit = Objects.Values.FirstOrDefault(o => o.ZOT == ZObType.Exit && o.Location == user.Location && o.Name.ToLowerInvariant().Contains(kw));
                if (exit != null)
                {
                    var registers = new Registers(user);

                    var lockedMessage = exit.GetAttrValue("lockMsg") ?? "It's locked!";
                    var dest = Objects.GetValueOrDefault(exit.Parent);
                    var leaveMessage = exit.GetAttrValue("leaveMsg") ?? $"{user.Name} leaves, heading toward {dest?.Name ?? "somewhere"}.";
                    var arriveMessage = exit.GetAttrValue("arriveMsg") ?? $"{user.Name} arrives from {Objects[user.Location].Name}.";

                    if (exit.Locks.Any(l => l.Item1 == "allow"))
                    {
                        if (!exit.Locks.Any(l => l.Item1 == "allow" && l.Item2 == "#" + session.UserId.ToString()))
                        {
                            PlayerEmit(session.Key, registers.ApplyToString(lockedMessage));
                            break;
                        }
                    }
                    else if (exit.Locks.Any(l => l.Item1 == "deny" && l.Item2 == "#" + session.UserId.ToString()))
                    {
                        PlayerEmit(session.Key, registers.ApplyToString(lockedMessage));
                        break;
                    }

                    if (dest != null)
                    {
                        var oldLoc = user.Location;

                        //The emits are backwards so we don't spam the user, don't worry about it, lol.
                        RoomEmit(dest.Id, registers.ApplyToString(arriveMessage));
                        user.Location = dest.Id;
                        RoomEmit(oldLoc, registers.ApplyToString(leaveMessage));
                        user.Save();
                    }
                    else
                    {
                        PlayerEmit(session.Key, $"The exit {exit.Name} seems to lead nowhere...");
                    }
                    break;
                }

                //Custom Command Handlers
                var potentialHandlers = GetObjectsInScope(user, true).Where(z => z.HasFlag(Flag.Handler)).ToList();
                bool handled = false;

                foreach (var h in potentialHandlers)
                {
                    var attrs = h.Attrs.Where(nvp => nvp.Name.StartsWith("$") && Matcher.DoesInputMatchCommandHandler(command, nvp.Name.Substring(1))).ToList();
                    if (attrs.Any())
                    {
                        foreach (var a in attrs)
                        {
                            var handlerVal = a.Value;
                            if (!handlerVal.StartsWith("{"))
                                handlerVal = "{" + handlerVal + "}";

                            var registers = Matcher.ExtractCommandHandlerRegisters(command, a.Name.Substring(1));
                            s = ZString.Eval(handlerVal, h, ref user.Quota, new Registers(registers, user));
                            handled = true;
                        }
                    }
                }
                if (handled) break;

                PlayerEmit(session.Key, $"I don't know how to '{command}'");
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

        ret = ZString.Eval(ret, loc);

        ret += "<br /><br /><table width='99%'><tr><td width='33%' valign='top'>";
        if (exits.Any())
        {
            var exitStr = "<b>Exits:</b><br />";
            foreach (var exit in exits)
            {
                if (Settings.AutoLinkExits)
                    exitStr += "[" + exit.Name + "]<br />";
                else
                    exitStr += exit.Name + "<br />";
            }

            ret += Interpreter.ApplyAllTags(exitStr, loc);
        }

        ret += "</td><td width='33%' valign='top'>";
        if (pcs.Any())
        {
            ret += "<b>Players:</b><br />";
            foreach (var pc in pcs)
            {
                if (Sessions.Values.Any(s => s.UserId == pc.Id))
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

        if (session.SpecialOutput.Length > 0)
        {
            ret += "<div id='specialOutput'>";
            ret += ZString.Eval(session.SpecialOutput.ToString(), loc);
            ret += "</div>";
            session.SpecialOutput.Clear();
        }

        ret += "<div id='log'> </div>";

        return ret;
    }

    internal static string[] GetLog(SessionModel session, bool clear = false)
    {
        var user = Objects[session.UserId];

        var log = Logs.GetOrAdd(session.Key, _ => new List<string>());
        var ret = log.Select(s => Interpreter.ApplyAllTags(s, user)).ToArray();

        if (clear)
            log.Clear();

        return ret;
    }
}