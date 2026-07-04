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

        switch (cmd.Value)
        {
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

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '{')
            {
                var end = input.IndexOf('}', i);
                if (end == -1)
                    ret += c;
                else
                {
                    var key = input.Substring(i + 1, end - i - 1);
                    var kw = key.Split(" ")[0];

                    if (Engine.Formatters.TryGetValue(kw, out var formatter))
                        ret += formatter(key.Substring(kw.Length).Trim());
                    else
                    {
                        if (!key.Contains(":"))
                            ret += $"<a class='action-link' data-action='{key}'>{key}</a>";
                        else
                        {
                            var parts = key.Split(":", 2);
                            ret += $"<a class='action-link' data-action='{parts[1]}'>{parts[0]}</a>";
                        }
                    }

                    i = end;
                }
            }
            else
                ret += c;
        }


        return ret;
    }

    internal static string ApplyAllTags(string text, ZObject context)
    {
        var res = ZString.ApplyTags(text, context);
        return res;
    }

}