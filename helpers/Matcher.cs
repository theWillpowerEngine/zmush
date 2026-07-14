public static class Matcher
{
    public static bool DoesInputMatchCommandHandler(string input, string handler)
    {
        var ii = 0;
        for (var i = 0; i < handler.Length && i < handler.Length; i++)
        {
            var c = handler[i];

            if (ii >= input.Length)
                return false;

            var cc = input[ii++];

            switch (c)
            {
                case '*':
                    if (i == handler.Length - 1)
                        return true;

                    var breaker = handler[++i];

                    while (ii < input.Length && input[ii] != breaker)
                        ii++;

                    if (ii >= input.Length)
                        return false;

                    break;

                default:
                    if (c != cc)
                        return false;
                    break;
            }
        }
        return true;
    }

    //Call DoesInputMatchCommandHandler first or this might barf, I don't want to double-up the parse by rechecking
    internal static List<string> ExtractCommandHandlerRegisters(string input, string handler)
    {
        var ret = new List<string>();
        var ii = 0;

        for (var i = 0; i < handler.Length && i < handler.Length; i++)
        {
            var c = handler[i];
            switch (c)
            {
                case '*':
                    if (i == handler.Length - 1)
                    {
                        ret.Add(input.Substring(ii + (ret.Any() ? 1 : 0)));
                        return ret;
                    }

                    var breaker = handler[++i];

                    var start = ii;
                    while (ii < input.Length && input[ii] != breaker)
                        ii++;

                    ret.Add(input.Substring(start, ii - start));
                    break;

                default:
                    ii++;
                    break;
            }
        }
        return ret;
    }

    public static bool IsTruthy(string val)
    {
        var valIsTruthy = val != "0" && val != "" && val.ToLower() != "false" && val.ToLower() != "no";
        return valIsTruthy;
    }

    public static string Escape(string message)
    {
        var s = message;

        //Removing escapes
        s = s.Replace("%n", "<br />").Replace("%s", "&nbsp;").Replace("%t", "&emsp;");

        s = s.Replace("%", "%%");

        s = s.Replace("{", "%{").Replace("}", "%}");
        s = s.Replace("[", "%[").Replace("]", "%]");

        return s;
    }

    public static bool IsMatchingFunction(string check, string name, int parms)
    {
        var key = $">{name}";

        if (!check.StartsWith(key))
            return false;

        check = check.Substring(key.Length).Trim();
        var parts = check.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != parms)
            return false;

        return true;
    }

    public static List<string> ExtractParameterNames(ZObject o, string name, int parms)
    {
        var parmS = name.TrimEnd(')').Split('(', StringSplitOptions.RemoveEmptyEntries)[1];
        var eles = parmS.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        return eles.ToList();
    }
}