public static class Interpreter
{
    internal static string Evaluate(Token toke, ZObject context)
    {
        if (!toke.IsList) return toke.Value;

        var list = toke.Children;

        var cmd = list[0];
        if (cmd.IsList)
            return "[Exception: Keyword is a list]";

        switch (cmd.Value)
        {
            case "rev":
                if (list.Count != 2)
                    return "[Exception: 'rev' requires exactly 1 parameter]";

                var revResult = ParseValue(list[1], context);
                return new string(revResult.Reverse().ToArray());

            default:
                return $"[Exception: Unknown command '{cmd.Value}']";
        }
    }

    private static string ParseValue(Token t, ZObject context)
    {
        if (t.IsList) return Evaluate(t, context);

        return t.Value;

        // if (t.IsName)
        // {
        //     var name = t.Value.ToString().ToLower();
        //     if (!ro.GlobalVariables.Contains(name) && !ro.LocalsDuringParse.Contains(name))
        //         ro.Errors.Add(new(ErrorCodes.Undefined, $"{name} is undefined", t, t));
        // }
    }
}