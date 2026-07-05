public static class Interpreter
{
    internal static string Evaluate(Token toke, ZObject context, ref int quota)
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

        switch (cmd.Value)
        {
            case "do":
                if (list.Count < 2)
                    return "--Exception: 'do' requires at least 1 parameter--";

                var ret = "";
                for (var i = 1; i < list.Count; i++)
                {
                    ret = ParseValue(list[i], context, ref quota);
                }
                return ret;

            case "emit":
                if (list.Count != 3)
                    return "--Exception: 'emit' requires exactly 2 parameters--";

                s = ParseValue(list[1], context, ref quota);
                o = Engine.GlobalFind(context, s);
                if (o == null)
                    return $"--Exception: Object '{s}' not found--";

                var emitVal = ParseValue(list[2], context, ref quota);

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

            case "val":
            case "v":
                if (list.Count == 3)
                {
                    var oVName = ParseValue(list[1], context, ref quota);
                    var oV = Engine.Find(context, oVName);
                    if (oV == null)
                        return $"--Exception: Object '{oVName}' not found--";

                    return oV.GetAttrValue(ParseValue(list[2], context, ref quota)) ?? "";
                }
                else if (list.Count == 2)
                {
                    return context.GetAttrValue(ParseValue(list[1], context, ref quota)) ?? "";
                }
                else
                    return "--Exception: 'val' requires 1 or 2 parameters--";

            case "single":
                if (list.Count == 1)
                    return "--Exception: 'single' requires at least 1 parameter (and really it should be 2 or you're just wasting quota)--";

                for (var i = 1; i < list.Count; i++)
                {
                    var val = ParseValue(list[i], context, ref quota);
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
                return "";

            case "stg":
                if (list.Count != 2)
                    return "--Exception: 'stg' requires exactly 1 parameter--";

                s = ParseValue(list[1], context, ref quota) ?? "";
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

                s = ParseValue(list[1], context, ref quota) ?? "";
                s2 = ParseValue(list[2], context, ref quota) ?? "";
                var valIsTruthy = s2 != "0" && s2 != "" && s2.ToLower() != "false" && s2.ToLower() != "no";

                var isANumber = int.TryParse(s2, out var numVal);
                switch (s.ToLower())
                {
                    case "showhttp":
                        Engine.Settings.ShowHttpRequest = valIsTruthy;
                        return Engine.Settings.ShowHttpRequest ? "1" : "0";
                    case "logquotaexceeds":
                        Engine.Settings.LogQuotaExceeds = valIsTruthy;
                        return Engine.Settings.LogQuotaExceeds ? "1" : "0";
                    case "breakonexception":
                        Engine.Settings.BreakOnExceptionDontUseThisUnlessYoureSmart = valIsTruthy;
                        return Engine.Settings.BreakOnExceptionDontUseThisUnlessYoureSmart ? "1" : "0";
                    case "autolinkexits":
                        Engine.Settings.AutoLinkExits = valIsTruthy;
                        return Engine.Settings.AutoLinkExits ? "1" : "0";

                    case "startroom":
                        if (!isANumber)
                            return "--Exception: 'sts startroom' requires a numeric value--";
                        Engine.Settings.NewCharacterStartingRoom = numVal;
                        return Engine.Settings.NewCharacterStartingRoom.ToString();
                    case "masterroom":
                        if (!isANumber)
                            return "--Exception: 'sts masterroom' requires a numeric value--";
                        Engine.Settings.MasterRoom = numVal;
                        return Engine.Settings.MasterRoom.ToString();
                    case "masterpc":
                        if (!isANumber)
                            return "--Exception: 'sts masterpc' requires a numeric value--";
                        Engine.Settings.MasterCharacter = numVal;
                        return Engine.Settings.MasterCharacter.ToString();
                    case "masteritem":
                        if (!isANumber)
                            return "--Exception: 'sts masteritem' requires a numeric value--";
                        Engine.Settings.MasterItem = numVal;
                        return Engine.Settings.MasterItem.ToString();

                    default:
                        return $"--Exception: Unknown setting '{s}'--";
                }

            default:
                return $"--Exception: Unknown command '{cmd.Value}'--";
        }
    }

    private static string ParseValue(Token t, ZObject context, ref int quota)
    {
        if (t.TT == TokenType.Code) return Evaluate(t, context, ref quota);

        return t.Value;

        // if (t.IsName)
        // {
        //     var name = t.Value.ToString().ToLower();
        //     if (!ro.GlobalVariables.Contains(name) && !ro.LocalsDuringParse.Contains(name))
        //         ro.Errors.Add(new(ErrorCodes.Undefined, $"{name} is undefined", t, t));
        // }
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