public static class Interpreter
{
    internal static string Evaluate(Token toke, ZObject context, ref int quota, Registers? registers = null)
    {
        var list = toke.Children;

        var cmd = list[0];

        if (cmd.TT != TokenType.Keyword)
            return "--Exception: First token must be a keyword--";

        if (quota == 0)
        {
            if (Engine.Settings.LogQuotaExceeds)
                Engine.Log("quota", $"Quota exceeded for context #{context.Id}.  Command: {string.Join(" ", list.Select(t => t.Value))}");

            return "--Limit:  Quota exceeded--";
        }
        else if (quota > 0)
            quota -= 1;

        string s, s2;
        ZObject? o, o2;

        if (cmd.Value.StartsWith("?"))
            return ParsePredicate(cmd, list.Skip(1).ToList(), context, ref quota, registers);

        switch (cmd.Value)
        {
            case "do":
                if (list.Count < 2)
                    return "--Exception: 'do' requires at least 1 parameter--";

                var ret = "";
                for (var i = 1; i < list.Count; i++)
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

                for (var i = 0; i < caseCount; i++)
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

            case "single":
                if (list.Count == 1)
                    return "--Exception: 'single' requires at least 1 parameter (and really it should be 2 or you're just wasting quota)--";

                for (var i = 1; i < list.Count; i++)
                {
                    var val = ParseValue(list[i], context, ref quota, registers);
                    if (!string.IsNullOrWhiteSpace(val))
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

                    default:
                        return $"--Exception: Unknown setting '{s}'--";
                }

            case "sts":
                if (context.Id != 0)
                    return "--Exception: 'sts' can only be used in the context of the main admin user--";
                if (list.Count != 3)
                    return "--Exception: 'sts' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota, registers) ?? "";
                s2 = ParseValue(list[2], context, ref quota, registers) ?? "";
                var valIsTruthy = Matcher.IsTruthy(s2);
                var isANumber = int.TryParse(s2, out var numVal);


                s = "";
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

        switch (cmd.Value)
        {
            case "?=":
            case "?!":
                var isNE = cmd.Value == "?!";
                if (!isValidComparison)
                    return $"--Exception: '{cmd.Value}' requires 2-4 parameters--";
                s = ParseValue(rest[1], context, ref quota, registers);
                var res = s.ToLower() == checkVal.ToLower();
                if (isNE) res = !res;
                if (res)
                {
                    if (comparisonIsSimple) return "1";
                    return ParseValue(rest[2], context, ref quota, registers);
                }
                else if (comparisonHasFalse)
                    return ParseValue(rest[3], context, ref quota, registers);
                else if (comparisonIsSimple)
                    return "0";
                else
                    return "";

            case "??":
                if (!isValidSingleton)
                    return $"--Exception: '??' requires 1-3 parameters--";
                if (Matcher.IsTruthy(checkVal))
                {
                    if (singletonIsSimple) return "1";
                    return ParseValue(rest[1], context, ref quota, registers);
                }
                else if (singletonHasFalse)
                    return ParseValue(rest[2], context, ref quota, registers);
                else if (singletonIsSimple)
                    return "0";
                else
                    return "";

            default:
                return $"--Exception: Unknown command '{cmd.Value}'--";
        }
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

                    var registerName = t.Value.Substring(1);
                    if (int.TryParse(registerName, out var idx) && registers != null && idx < registers.Numbered.Length)
                        return registers.Numbered[idx];

                    ZObject? actor;
                    switch (registerName)
                    {
                        case "a":
                            return registers?.ActorId.ToString() ?? "";
                        case "an":
                            return registers?.ActorName ?? "";

                        case "l":
                            actor = Engine.Objects[registers.ActorId];
                            return actor.Location.ToString();

                        case "as":
                            actor = Engine.Objects[registers.ActorId];
                            return actor.Male switch
                            {
                                true => "he",
                                false => "she",
                                null => "it"
                            };
                        case "As":
                            actor = Engine.Objects[registers.ActorId];
                            return actor.Male switch
                            {
                                true => "He",
                                false => "She",
                                null => "It"
                            };

                        case "ao":
                            actor = Engine.Objects[registers.ActorId];
                            return actor.Male switch
                            {
                                true => "him",
                                false => "her",
                                null => "it"
                            };
                        case "Ao":
                            actor = Engine.Objects[registers.ActorId];
                            return actor.Male switch
                            {
                                true => "Him",
                                false => "Her",
                                null => "It"
                            };

                        case "ap":
                            actor = Engine.Objects[registers.ActorId];
                            return actor.Male switch
                            {
                                true => "his",
                                false => "hers",
                                null => "its"
                            };
                        case "Ap":
                            actor = Engine.Objects[registers.ActorId];
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