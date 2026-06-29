public class ReaderOutput
{
    public bool WasSuccessful => Errors.Count == 0;

    public List<string> Errors = new();
    public Token List;
}

public class Token
{
    public string Value = "";
    public List<Token> Children = new();
    public int Depth = 0;

    public bool IsList => Children.Count > 0;

    public Token(string toke, int depth)
    {
        Value = toke;
        Depth = depth;
    }
    public Token(List<Token> children, int depth)
    {
        Children = children;
        Depth = depth;
    }
}

public static class Reader
{
    public static readonly string[] Keywords = ["+", "-", "*", "/", "1st", "else", "extern", "global", "if", "let", "pragma", "quote", "rest", "use"];

    public static ReaderOutput Read(string code)
    {
        var retVal = new List<Token>();
        var ro = new ReaderOutput();

        code = code.Trim();

        var work = "";
        var scanToDepth = 0;
        var scope = 0;
        char stringDelim = '#';

        Action appendWork = () =>
        {
            if (!string.IsNullOrEmpty(work))
            {
                retVal.Add(new Token(work, scanToDepth));
            }
        };

        for (var i = 0; i < code.Length; i++)
        {
            var c = code[i];
            char? lookAhead = null;
            if (i + 1 < code.Length)
                lookAhead = code[i + 1];

            if (stringDelim != '#' || (scope == 0 && c != '['))
            {
                if (c == '%' && lookAhead == stringDelim)
                {
                    i += 1;
                    work += stringDelim.ToString();
                    continue;
                }
                else if (c == '%' && lookAhead == 's')
                {
                    i += 1;
                    work += "&nbsp;";
                    continue;
                }
                else if (c == '%' && lookAhead == 't')
                {
                    i += 1;
                    work += "&nbsp;&nbsp;&nbsp;&nbsp;";
                    continue;
                }
                else if (c == '%' && lookAhead == 'n')
                {
                    i += 1;
                    work += "<br />";
                    continue;
                }
                else if (c == '%' && lookAhead == '%')
                {
                    i += 1;
                    work += '%';
                    continue;
                }

                else if (stringDelim != '#' && c == stringDelim)
                {
                    stringDelim = '#';
                    if (scanToDepth > 0)
                    {
                        work += stringDelim;
                        stringDelim = '#';
                        continue;
                    }
                    else
                        retVal.Add(new Token(work, scope));
                    continue;
                }

                work += c;
                continue;
            }

            if (scanToDepth > 0)
            {
                if (c == ';')
                {
                    try
                    {
                        while (code[++i] != ';' && code[i] != '\r' && code[i] != '\n')
                        { }
                    }
                    catch (IndexOutOfRangeException)
                    { }
                    continue;
                }
                if (c == '"' || c == '`')
                {
                    stringDelim = c;
                    work += c;
                    continue;
                }
                else if (c == '\'')
                {
                    if (lookAhead != '[')
                    {
                        stringDelim = c;
                        work += c;
                        continue;
                    }
                    else
                    {
                        work += c;
                        continue;
                    }
                }
                else if (c == '[')
                    scanToDepth += 1;
                else if (c == ']')
                {
                    if (scanToDepth == 1)
                    {
                        scanToDepth = 0;

                        var scanned = Read(work);
                        retVal.Add(scanned.List);
                        work = "";
                        continue;
                    }

                    scanToDepth -= 1;
                }

                work += c;
                continue;
            }

            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    appendWork();
                    work = "";
                    break;

                case '[':
                    appendWork();
                    work = "";
                    scanToDepth = 1;
                    scope += 1;
                    break;

                case ']':
                    ro.Errors.Add("Unmatched end-bracket found");
                    break;

                case ';':
                    appendWork();
                    try
                    {
                        while (code[++i] != ';' && code[i] != '\r' && code[i] != '\n')
                        { }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        //No error, the last syntactical element can be a comment
                    }
                    break;

                case '"':
                    appendWork();
                    stringDelim = c;
                    break;

                case '\'':
                    if (code[i + 1] != '[')
                    {
                        //It's a string!
                        appendWork();
                        stringDelim = c;
                    }
                    else
                    {
                        //Quoted list, ie '[1 2 3] => [quote 1 2 3]
                        appendWork();

                        work = "quote ";
                        scanToDepth = 1;
                        i++;
                    }
                    break;

                //Reader shortcut:  auto-interpolation
                case '`':
                    appendWork();
                    stringDelim = c;
                    break;

                // case '$':
                //     appendWork();
                //     isAutoV = true;
                //     break;

                //Reader shortcut:  Arrow Lambdas
                // case '-':
                //     if (code[i + 1] == '>')
                //     {
                //         appendWork();
                //         i += 1;

                //         if (!retVal[retVal.Count - 1].IsParent)
                //             Error("Arrow Lambda shortcut must be preceded by parameter list, not " + retVal[retVal.Count - 1].ToString());

                //         isAutoLambda = true;
                //     }
                //     else
                //         work += c;
                //     break;

                default:
                    work += c;
                    break;
            }
        }

        if (stringDelim != '#')
            ro.Errors.Add("Unterminated string value: " + work);

        appendWork();

        if (scanToDepth > 0)
            ro.Errors.Add($"Unterminated list (missing {scanToDepth} end-parens)");

        ro.List = new(retVal, 0);

        return ro;
    }
}