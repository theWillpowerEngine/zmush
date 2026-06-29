public static class Interpreter
{
    internal static string Evaluate(Token toke, ZObject context, ref int quota)
    {
        if (!toke.IsList) return toke.Value;

        var list = toke.Children;

        var cmd = list[0];
        if (cmd.IsList)
            return "[Exception: Keyword is a list]";

        if (quota == 0)
        {
            if (Engine.Settings.LogQuotaExceeds)
                Engine.Log("quota", $"Quota exceeded for context #{context.Id}.  Command: {string.Join(" ", list.Select(t => t.Value))}");

            return "[Limit:  Quota exceeded]";
        }
        else if (quota > 0)
            quota -= 1;

        switch (cmd.Value)
        {
            case "rev":
                if (list.Count != 2)
                    return "[Exception: 'rev' requires exactly 1 parameter]";

                var revResult = ParseValue(list[1], context, ref quota);
                return new string(revResult.Reverse().ToArray());

            default:
                return $"[Exception: Unknown command '{cmd.Value}']";
        }
    }

    private static string ParseValue(Token t, ZObject context, ref int quota)
    {
        if (t.IsList) return Evaluate(t, context, ref quota);

        return t.Value;

        // if (t.IsName)
        // {
        //     var name = t.Value.ToString().ToLower();
        //     if (!ro.GlobalVariables.Contains(name) && !ro.LocalsDuringParse.Contains(name))
        //         ro.Errors.Add(new(ErrorCodes.Undefined, $"{name} is undefined", t, t));
        // }
    }

    public static string ApplyFormats(string input)
    {
        var ret = "";

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
}