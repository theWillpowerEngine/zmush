using System.Text;

public static class Interpreter
{
    internal static string Evaluate(Token toke, ZObject context, ref int quota, Registers? registers = null)
    {
        var list = toke.Children;

        if (registers == null)
            registers = new Registers(context);

        var cmd = list[0];

        if (quota == 0)
        {
            if (Engine.Settings.LogQuotaExceeds)
                Engine.Log("quota", $"Quota exceeded for context #{context.Id}.  Command: {string.Join(" ", list.Select(t => t.Value))}");

            return "--Limit:  Quota exceeded--";
        }
        else if (quota > 0)
            quota -= 1;

        string? s, s2;
        ZObject? o, o2;
        int i = 0;

        if (cmd.TT == TokenType.Name)
        {
            if (cmd.Value.Contains("."))
            {
                var eles = cmd.Value.Split('.');
                if (eles.Length != 2)
                    return $"--Exception: Invalid dotted notation: {cmd.Value}--";

                o = Engine.GlobalFind(context, eles[0]);

                if (o == null)
                    return $"--Exception: Object '{eles[0]}' not found--";

                var name = eles[1];
                s = o.GetMatchingFunctionAttr(name, list.Count - 1);

                //Function call
                if (s != null)
                {
                    var f = o.GetAttrValue(s);
                    if (f == null)
                        return $"--Exception: Attribute '{s}' not found on #{o.Id}--";
                    if (registers == null)
                        return $"--Exception: No registers available for function call--";

                    registers.AdvanceLetScope(o);
                    var parmNames = Matcher.ExtractParameterNames(context, s, list.Count - 1);

                    i = 0;
                    foreach (var pn in parmNames)
                    {
                        registers.Let(pn, ParseValue(list[++i], context, ref quota, registers));
                    }

                    if (!f.StartsWith("{"))
                        f = "{" + f + "}";

                    var result = ZString.Eval(f, context, ref quota, registers);
                    registers.EndLetScope();
                    return result;
                }

                //Auto-V
                s = o.GetAttrValue(name);
                if (s == null)
                    return $"--Exception: Attribute '{name}' not found on #{o.Id} (possible function argument-count mismatch?)--";
                else
                    return s;
            }

            //Just a single name at root level
            if (list.Count == 1)
                return ParseValue(cmd, context, ref quota, registers);

        }

        if (cmd.Value.StartsWith("?") || cmd.Value == "if")
            return ParsePredicate(cmd, list.Skip(1).ToList(), context, ref quota, registers);

        if (cmd.TT != TokenType.Keyword)
            return "--Exception: First token must be a keyword--";

        switch (cmd.Value)
        {
            //PDL Keywords
            case "add":
                if (list.Count != 3)
                    return "--Exception: 'list-add' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);
                s2 = ParseValue(list[2], context, ref quota, registers);

                s = PDL.Add(s, s2);
                return s;

            case "index":
                if (list.Count != 3)
                    return "--Exception: 'list-index' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);
                s2 = ParseValue(list[2], context, ref quota, registers);

                var idx = PDL.FindIndex(s, s2);
                return idx.ToString();

            case "map":
                if (list.Count != 3)
                    return "--Exception: 'list-map' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);


                var oldIterator = registers?.IterativeElement ?? "";
                registers?.IterativeElement = s;

                var items = PDL.Split(s);
                s2 = "";
                foreach (var item in items)
                {
                    registers?.IterativeElement = item;
                    s2 = PDL.Add(s2, ParseValue(list[2], context, ref quota, registers));
                }

                registers?.IterativeElement = oldIterator;
                return s2;

            case "remove":
                if (list.Count != 3)
                    return "--Exception: 'list-remove' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);
                s2 = ParseValue(list[2], context, ref quota, registers);

                if (!int.TryParse(s2, out var idx2))
                    return "--Exception: 'list-remove' requires the second parameter to be a numeric index (1-based)--";

                s = PDL.RemoveAtIndex(s, idx2);
                return s;

            case "remove-all":
                if (list.Count != 3)
                    return "--Exception: 'list-remove-all' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);
                s2 = ParseValue(list[2], context, ref quota, registers);

                s = PDL.RemoveAll(s, s2);
                return s;

            //Other Keywords
            case "concat":
                if (list.Count < 3)
                    return "--Exception: 'concat' requires at least 2 parameters--";

                s = "";
                for (i = 1; i < list.Count; i++)
                {
                    s += ParseValue(list[i], context, ref quota, registers);
                }
                return s;

            case "do":
                if (list.Count < 2)
                    return "--Exception: 'do' requires at least 1 parameter--";

                var ret = "";
                for (i = 1; i < list.Count; i++)
                {
                    ret = ParseValue(list[i], context, ref quota, registers);
                }
                return ret;

            case "emit":
                if (list.Count != 3)
                    return "--Exception: 'emit' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);
                o = Engine.GlobalFind(context, s);
                if (o == null)
                    return $"--Exception: Object '{s}' not found--";

                var emitVal = ParseValue(list[2], context, ref quota, registers);

                switch (o.ZOT)
                {
                    case ZObType.Room:
                        Engine.RoomEmit(o.Id, emitVal);
                        break;

                    case ZObType.Character:
                        Engine.PlayerEmit(o, emitVal);
                        break;

                    case ZObType.Item:
                        o2 = Engine.Objects[o.Location];
                        switch (o2.ZOT)
                        {
                            case ZObType.Room:
                                Engine.RoomEmit(o2.Id, emitVal);
                                break;
                            case ZObType.Character:
                                Engine.PlayerEmit(o2, emitVal);
                                break;
                            default:
                                return $"--Exception: Cannot emit to item located in object type {o2.ZOT}--";
                        }
                        break;

                    default:
                        return $"--Exception: Cannot emit to object type {o.ZOT}--";
                }
                return emitVal;

            case "force":
                if (list.Count != 3)
                    return "--Exception: 'force' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);
                o = Engine.GlobalFind(context, s);
                if (o == null)
                    return $"--Exception: Object '{s}' not found--";

                if (!o.HasFlag(Flag.CanForce))
                    return $"--Exception: Object '{s}' does not have the CanForce flag--";

                var fakeSession = new SessionModel
                {
                    Key = $"force-{o.Id}-{DateTime.UtcNow.Ticks}",
                    UserId = o.Id,
                    Roles = new HashSet<string> { }
                };

                var forceVal = ParseValue(list[2], context, ref quota, registers);
                s = forceVal.Split(' ', '/', '\\')[0].ToLower();
                if (Engine.Settings.UnforceableCommands.Contains(s) && !o.HasFlag(Flag.ForceMajeure, true))
                    return $"--Exception: Command '{s}' is not allowed to be forced--";

                return Engine.Command(fakeSession, forceVal);

            case "id":
                if (list.Count != 2)
                    return "--Exception: 'id' requires exactly 1 parameter--";

                s = ParseValue(list[1], context, ref quota, registers);
                o = Engine.GlobalFind(context, s);
                if (o == null)
                    return $"--Exception: Object '{s}' not found--";

                return o.Id.ToString();

            case "let":
                if (list.Count < 4)
                    return "--Exception: 'let' requires at least 3 parameters--";
                if (list.Count % 2 != 0)
                    return "--Exception: 'let' requires an odd number of parameters--";

                Dictionary<string, string> scopeVars = new();

                for (i = 1; i < list.Count - 1; i += 2)
                {
                    if (list[i].TT != TokenType.Name)
                        return $"--Exception: '{list[i].Value}' is not a name in let--";
                    s = list[i].Value;
                    s2 = ParseValue(list[i + 1], context, ref quota, registers);
                    scopeVars.Add(s, s2);
                }

                registers.AdvanceLetScope(null);
                foreach (var key in scopeVars.Keys)
                    registers.Let(key, scopeVars[key]);

                s = ParseValue(list[list.Count - 1], context, ref quota, registers);
                registers.EndLetScope();

                return s;

            case "log":
                if (list.Count != 2)
                    return "--Exception: 'log' requires exactly 1 parameter--";

                s = ParseValue(list[1], context, ref quota, registers);
                Engine.Log("zelazny", s);
                return s;

            case "match":
                if (list.Count < 4)
                    return "--Exception: 'match' requires at least 3 parameters--";

                //s = check
                s = ParseValue(list[1], context, ref quota, registers);

                var hasDefault = list.Count % 2 == 1;
                var caseCount = (hasDefault ? (list.Count - 2) : (list.Count - 1)) / 2;
                var caseI = 2;

                for (i = 0; i < caseCount; i++)
                {
                    var compare = ParseValue(list[caseI], context, ref quota, registers);
                    if (s == compare)
                    {
                        var val = ParseValue(list[caseI + 1], context, ref quota, registers);
                        return val;
                    }

                    caseI += 2;
                }

                if (hasDefault)
                {
                    var defaultVal = ParseValue(list[list.Count - 1], context, ref quota, registers);
                    return defaultVal;
                }

                return "";

            case "move":
                if (list.Count != 3)
                    return "--Exception: 'move' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);
                o = Engine.GlobalFind(context, s);
                if (o == null)
                    return $"--Exception: 'move' could not find object '{s}'--";

                if (!context.HasFlag(Flag.Teleporter, true) && !(registers?.Executor?.HasFlag(Flag.Teleporter, true) ?? false) && !Engine.IsAdminUser(context.Id))
                    if (!o.CheckPermissions(context, registers?.Executor))
                        return $"--Exception: 'move' permission denied for object '{s}'--";

                s2 = ParseValue(list[2], context, ref quota, registers);
                var dest = Engine.GlobalFind(context, s2);
                if (dest == null)
                    return $"--Exception: 'move' could not find destination '{s2}'--";

                switch (o.ZOT)
                {
                    case ZObType.Room:
                        return $"--Exception: 'move' cannot move a room and '{s}' is a room--";

                    case ZObType.Item:
                        if (dest.ZOT != ZObType.Room && dest.ZOT != ZObType.Character)
                            return $"--Exception: 'move' destination for ZOT {o.ZOT} must be a room or a character and '{s2}' is not a room or a character--";
                        break;

                    case ZObType.Exit:
                    case ZObType.Character:
                        if (dest.ZOT != ZObType.Room)
                            return $"--Exception: 'move' destination for ZOT {o.ZOT} must be a room and '{s2}' is not a room--";
                        break;

                    default:
                        return $"--Exception: 'move' cannot move object of type {o.ZOT}--";
                }

                o.Location = dest.Id;
                o.Save();

                return dest.Id.ToString();

            case "roll":
                if (list.Count == 2)
                {
                    s = ParseValue(list[1], context, ref quota, registers);
                    if (!int.TryParse(s, out var sides))
                        return "--Exception: 'roll' parameter must be an integer--";
                    return (Engine.R.Next(sides) + 1).ToString();
                }
                else if (list.Count == 3)
                {
                    s = ParseValue(list[1], context, ref quota, registers);
                    if (!int.TryParse(s, out var count))
                        return "--Exception: 'roll' first parameter must be an integer--";
                    s = ParseValue(list[2], context, ref quota, registers);
                    if (!int.TryParse(s, out var sides))
                        return "--Exception: 'roll' second parameter must be an integer--";
                    var result = 0;
                    for (var j = 0; j < count; j++)
                        result += Engine.R.Next(sides) + 1;
                    return result.ToString();
                }
                return "--Exception: 'roll' requires 1 or 2 parameters--";

            case "roll-pool":
                if (list.Count != 3)
                    return "--Exception: 'roll-pool' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers);
                if (!int.TryParse(s, out var count2))
                    return "--Exception: 'roll-pool' first parameter must be an integer--";
                s = ParseValue(list[2], context, ref quota, registers);
                if (!int.TryParse(s, out var sides2))
                    return "--Exception: 'roll-pool' second parameter must be an integer--";
                var result2 = "";
                for (var j = 0; j < count2; j++)
                    result2 = PDL.Add(result2, (Engine.R.Next(sides2) + 1).ToString());
                return result2;

            case "set":
                if (list.Count != 3 && list.Count != 4)
                    return "--Exception: 'set' requires 2 or 3 parameters--";
                if (list.Count == 3)
                    list.Insert(1, new Token("this", TokenType.Name));

                s = ParseValue(list[1], context, ref quota, registers);
                o = Engine.Find(context, s);
                if (o == null)
                    return $"--Exception: Object '{s}' not found--";

                s2 = ParseValue(list[2], context, ref quota, registers);
                if (list[3].TT == TokenType.Keyword)
                    list[3].TT = TokenType.Name;
                s = ParseValue(list[3], context, ref quota, registers);

                if (!o.CheckPermissions(context, registers?.Executor))
                    return $"--Exception: You do not have permission to set attributes on #{o.Id}--";

                var attr = o.Attrs.FirstOrDefault(a => a.Name.ToLowerInvariant() == s2.ToLowerInvariant());
                if (attr != null && !attr.CanSet(o, context, registers?.Executor))
                    return $"--Exception: You do not have permission to set the '{s2}' attribute on #{o.Id}--";

                o.SetAttrValue(s2, s);
                o.Save();
                return s;

            case "setv":
                if (list.Count != 3)
                    return "--Exception: 'setv' requires exactly 2 parameters--";
                if (list[1].TT != TokenType.Name)
                    return "--Exception: 'setv' requires the first parameter to be a name--";

                s = list[1].Value;
                s2 = ParseValue(list[2], context, ref quota, registers);
                registers.Let(s, s2);
                return s2;

            case "single":
                if (list.Count == 1)
                    return "--Exception: 'single' requires at least 1 parameter (and really it should be 2 or you're just wasting quota)--";

                for (i = 1; i < list.Count; i++)
                {
                    var val = ParseValue(list[i], context, ref quota, registers);
                    if (Matcher.IsTruthy(val))
                        return val;
                }
                return "";

            case "stg":
                if (list.Count != 2)
                    return "--Exception: 'stg' requires exactly 1 parameter--";

                s = ParseValue(list[1], context, ref quota, registers) ?? "";
                switch (s.ToLower())
                {
                    case "showhttp":
                        return Engine.Settings.ShowHttpRequest ? "1" : "0";
                    case "logquotaexceeds":
                        return Engine.Settings.LogQuotaExceeds ? "1" : "0";
                    case "breakonexception":
                        return Engine.Settings.BreakOnExceptionDontUseThisUnlessYoureSmart ? "1" : "0";
                    case "autolinkexits":
                        return Engine.Settings.AutoLinkExits ? "1" : "0";

                    case "startroom":
                        return Engine.Settings.NewCharacterStartingRoom.ToString();
                    case "masterroom":
                        return Engine.Settings.MasterRoom.ToString();
                    case "masterpc":
                        return Engine.Settings.MasterCharacter.ToString();
                    case "masteritem":
                        return Engine.Settings.MasterItem.ToString();

                    case "autosaveminutes":
                    case "autosavemins":
                        return Engine.Settings.AutoSaveMinutes.ToString();

                    default:
                        return $"--Exception: Unknown setting '{s}'--";
                }

            case "string":
            case "str":
                if (list.Count != 2)
                    return "--Exception: 'string' requires exactly 1 parameter--";

                s = ParseValue(list[1], context, ref quota, registers);

                //foreach [...] in the string, replace the value inside the brackets with the evaluated value
                var sb = new StringBuilder();
                i = 0;
                while (i < s.Length)
                {
                    var c = s[i];
                    if (c == '[')
                    {
                        var inner = Reader.ScanTo(s, ref i, ']', '[');
                        inner = "{" + inner + "}";
                        var res = ZString.Eval(inner, context, ref quota, registers);
                        sb.Append(res);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    i++;
                }
                return sb.ToString();

            case "sts":
                if (context.Id != 0 && context.Id != 1)
                    return "--Exception: 'sts' can only be used in the context of the main admin user--";
                if (list.Count != 3)
                    return "--Exception: 'sts' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers) ?? "";
                s2 = ParseValue(list[2], context, ref quota, registers) ?? "";
                var valIsTruthy = Matcher.IsTruthy(s2);
                var isANumber = int.TryParse(s2, out var numVal);

                switch (s.ToLower())
                {
                    case "showhttp":
                        Engine.Settings.ShowHttpRequest = valIsTruthy;
                        s = Engine.Settings.ShowHttpRequest ? "1" : "0";
                        break;
                    case "logquotaexceeds":
                        Engine.Settings.LogQuotaExceeds = valIsTruthy;
                        s = Engine.Settings.LogQuotaExceeds ? "1" : "0";
                        break;
                    case "breakonexception":
                        Engine.Settings.BreakOnExceptionDontUseThisUnlessYoureSmart = valIsTruthy;
                        s = Engine.Settings.BreakOnExceptionDontUseThisUnlessYoureSmart ? "1" : "0";
                        break;
                    case "autolinkexits":
                        Engine.Settings.AutoLinkExits = valIsTruthy;
                        s = Engine.Settings.AutoLinkExits ? "1" : "0";
                        break;

                    case "startroom":
                        if (!isANumber)
                            return "--Exception: 'sts startroom' requires a numeric value--";
                        Engine.Settings.NewCharacterStartingRoom = numVal;
                        s = Engine.Settings.NewCharacterStartingRoom.ToString();
                        break;
                    case "masterroom":
                        if (!isANumber)
                            return "--Exception: 'sts masterroom' requires a numeric value--";
                        Engine.Settings.MasterRoom = numVal;
                        s = Engine.Settings.MasterRoom.ToString();
                        break;
                    case "masterpc":
                        if (!isANumber)
                            return "--Exception: 'sts masterpc' requires a numeric value--";
                        Engine.Settings.MasterCharacter = numVal;
                        s = Engine.Settings.MasterCharacter.ToString();
                        break;
                    case "masteritem":
                        if (!isANumber)
                            return "--Exception: 'sts masteritem' requires a numeric value--";
                        Engine.Settings.MasterItem = numVal;
                        s = Engine.Settings.MasterItem.ToString();
                        break;

                    case "autosaveminutes":
                    case "autosavemins":
                        if (!isANumber)
                            return "--Exception: 'sts autosaveminutes' requires a numeric value--";
                        Engine.Settings.AutoSaveMinutes = numVal;
                        s = Engine.Settings.AutoSaveMinutes.ToString();
                        break;

                    default:
                        return $"--Exception: Unknown setting '{s}'--";
                }
                Engine.Settings.Save();
                return s;

            case "val":
            case "v":
                if (list.Count == 3)
                {
                    var oVName = ParseValue(list[1], context, ref quota, registers);
                    var oV = Engine.Find(context, oVName);
                    if (oV == null)
                        return $"--Exception: Object '{oVName}' not found--";

                    if (!oV.CheckPermissions(context, registers?.Executor))
                        return $"--Exception: You do not have permission to get attributes on #{oV.Id}--";

                    return oV.GetAttrValue(ParseValue(list[2], context, ref quota, registers)) ?? "";
                }
                else if (list.Count == 2)
                {
                    return context.GetAttrValue(ParseValue(list[1], context, ref quota, registers)) ?? "";
                }
                else
                    return "--Exception: 'val' requires 1 or 2 parameters--";

            default:
                return $"--Exception: Unknown command '{cmd.Value}'--";
        }
    }

    private static string ParsePredicate(Token cmd, List<Token> rest, ZObject context, ref int quota, Registers? registers = null)
    {
        string s;
        //ZObject? o, o2;

        var checkVal = ParseValue(rest[0], context, ref quota, registers);
        var singletonHasFalse = rest.Count == 3;
        var singletonIsSimple = rest.Count == 1;
        var comparisonHasFalse = rest.Count == 4;
        var comparisonIsSimple = rest.Count == 2;
        var isValidSingleton = singletonHasFalse || singletonIsSimple || rest.Count == 2;
        var isValidComparison = comparisonHasFalse || comparisonIsSimple || rest.Count == 3;

        ZObject? o;

        bool res;
        bool wasSingleton = false;

        switch (cmd.Value)
        {
            case "?=":
            case "?!":
                var isNE = cmd.Value == "?!";
                if (!isValidComparison)
                    return $"--Exception: '{cmd.Value}' requires 2-4 parameters--";
                s = ParseValue(rest[1], context, ref quota, registers);
                res = s.ToLower() == checkVal.ToLower();
                if (isNE) res = !res;
                break;

            case "?contains":
                if (!isValidComparison)
                    return $"--Exception: '?contains' requires 2-4 parameters--";
                s = ParseValue(rest[1], context, ref quota, registers);

                if (checkVal.StartsWith("|"))
                    res = PDL.FindIndex(checkVal, s) > 0;
                else
                    res = (checkVal + " ").Contains(s);
                break;

            case "if":
            case "??":
                if (!isValidSingleton)
                    return $"--Exception: '??' requires 1-3 parameters--";
                res = Matcher.IsTruthy(checkVal);
                wasSingleton = true;
                break;

            case "?num":
                if (!isValidSingleton)
                    return $"--Exception: '?num' requires 1-3 parameters--";
                res = int.TryParse(checkVal, out var numVal2);
                wasSingleton = true;
                break;

            case "?oid":
                if (!isValidSingleton)
                    return $"--Exception: '?oid' requires 1-3 parameters--";
                res = int.TryParse(checkVal, out var oidVal2) && Engine.Objects.ContainsKey(oidVal2);
                wasSingleton = true;
                break;

            case "?flag":
                if (!isValidComparison)
                    return $"--Exception: '?flag' requires 2-4 parameters--";
                s = ParseValue(rest[1], context, ref quota, registers);
                o = Engine.Find(context, s);
                if (o == null)
                    o = Engine.GlobalFind(context, s);
                if (o == null)
                    return $"--Exception: Object '{s}' not found--";

                if (!Enum.TryParse<Flag>(checkVal, true, out var fl))
                    return $"--Exception: Flag '{checkVal}' is not a valid flag--";

                res = o.HasFlag(fl, true);
                break;

            default:
                return $"--Exception: Unknown command '{cmd.Value}'--";
        }

        var wasSimple = (wasSingleton && singletonIsSimple) || (!wasSingleton && comparisonIsSimple);
        var hasFalse = (wasSingleton && singletonHasFalse) || (!wasSingleton && comparisonHasFalse);

        if (res)
        {
            if (wasSimple) return "1";
            return ParseValue(rest[rest.Count - (hasFalse ? 2 : 1)], context, ref quota, registers);
        }
        else if (hasFalse)
            return ParseValue(rest[rest.Count - 1], context, ref quota, registers);
        else if (wasSimple)
            return "0";
        else
            return "";
    }

    private static string ParseValue(Token t, ZObject context, ref int quota, Registers? registers = null)
    {
        switch (t.TT)
        {
            case TokenType.Code:
                return Evaluate(t, context, ref quota, registers);

            case TokenType.Tag:
                return GetTagValue(t);

            case TokenType.Name:
                if (t.Value.StartsWith("%"))
                {
                    if (registers == null) return "";

                    var actorId = registers.ActorId;

                    var registerName = t.Value.Substring(1);
                    if (int.TryParse(registerName, out var idx) && registers != null && idx > 0 && idx <= registers.Numbered.Length)
                        return registers.Numbered[idx - 1];

                    ZObject? actor;
                    switch (registerName)
                    {
                        case "i":
                            return registers?.IterativeElement ?? "";

                        case "a":
                            return registers?.ActorId.ToString() ?? "";
                        case "an":
                            return registers?.ActorName ?? "";

                        case "l":
                            actor = Engine.Objects[actorId];
                            return actor.Location.ToString();

                        case "as":
                            actor = Engine.Objects[actorId];
                            return actor.Male switch
                            {
                                true => "he",
                                false => "she",
                                null => "it"
                            };
                        case "As":
                            actor = Engine.Objects[actorId];
                            return actor.Male switch
                            {
                                true => "He",
                                false => "She",
                                null => "It"
                            };

                        case "ao":
                            actor = Engine.Objects[actorId];
                            return actor.Male switch
                            {
                                true => "him",
                                false => "her",
                                null => "it"
                            };
                        case "Ao":
                            actor = Engine.Objects[actorId];
                            return actor.Male switch
                            {
                                true => "Him",
                                false => "Her",
                                null => "It"
                            };

                        case "ap":
                            actor = Engine.Objects[actorId];
                            return actor.Male switch
                            {
                                true => "his",
                                false => "hers",
                                null => "its"
                            };
                        case "Ap":
                            actor = Engine.Objects[actorId];
                            return actor.Male switch
                            {
                                true => "His",
                                false => "Hers",
                                null => "Its"
                            };

                        default:
                            return "";
                    }
                }

                //Auto-V
                else if (t.Value.Contains("."))
                {
                    var parts = t.Value.Split('.', 2);
                    if (parts.Length != 2)
                        return t.Value;

                    var o = Engine.GlobalFind(context, parts[0]);
                    if (o == null)
                        return t.Value;
                    var attr = o.GetAttrValue(parts[1]);
                    if (attr == null)
                        return "";
                    return attr;
                }

                else
                {
                    var checkName = t.Value.ToLower();
                    var letVar = registers?.LetScope.FirstOrDefault(l => l.Name.ToLower() == checkName);
                    if (letVar != null)
                        return letVar.Value;
                }
                return t.Value;

            default:
                return t.Value;
        }
    }

    public static string GetTagValue(Token t)
    {
        var ret = "";
        var input = t.Value;

        var kw = input.Split(" ")[0];
        var rest = input.Substring(kw.Length).Trim();

        if (Engine.Formatters.TryGetValue(kw, out var formatter))
            ret += formatter(rest);
        else
        {
            if (!input.Contains(":"))
                ret += $"<a class='action-link' data-action='{input}'>{input}</a>";
            else
            {
                var parts = input.Split(":", 2);
                ret += $"<a class='action-link' data-action='{parts[1]}'>{parts[0]}</a>";
            }
        }

        return ret;
    }

    internal static string ApplyAllTags(string text, ZObject context)
    {
        var res = ZString.ApplyTags(text, context);
        return res;
    }

}