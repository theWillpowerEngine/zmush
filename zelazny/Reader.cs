public class ReaderOutput
{
    public bool WasSuccessful => Errors.Count == 0;

    public List<string> Errors = new();

    public List<Token> Tokes = new();

    public ReaderOutput Append(string s, TokenType tt)
    {
        Tokes.Add(new Token(s, tt));
        return this;
    }

    public ReaderOutput Append(List<Token> tokens)
    {
        Tokes.Add(new Token(tokens));
        return this;
    }
}

public enum TokenType
{
    Text,
    Code,
    Tag,

    Keyword,
    Number,
    String,
    Name,
}

public class Token
{
    public string Value = "";
    public List<Token> Children = new();

    public TokenType TT = TokenType.Text;

    public Token(string toke, TokenType tt)
    {
        Value = toke;
        TT = tt;
    }
    public Token(List<Token> children)
    {
        Children = children;
        TT = TokenType.Code;
    }
}

public static class Reader
{
    private static List<string> KWs = new()
    {
        "?=", "?!", "??", "?contains",
        "concat",
        "emit",
        "force",
        "do",
        "log",
        "match",
        "single",
        "stg", "sts",
        "string", "str",
        "val", "v",
    };

    public static string ScanTo(string s, ref int i, char terminator, char? opener = null)
    {
        var depth = 0;
        var work = "";

        //Skip the opener
        i += 1;

        for (; i < s.Length; i++)
        {
            var c = s[i];

            if (c == terminator && depth == 0)
                return work;

            if (opener != null && c == opener)
                depth += 1;
            else if (c == terminator && depth > 0)
                depth -= 1;

            work += c;
        }

        throw new IndexOutOfRangeException($"Unterminated string, missing {terminator} in {work}");
    }

    private static List<Token> ReadCode(string code, ReaderOutput ro)
    {
        var retVal = new List<Token>();

        var work = "";
        var stringDelim = '#';

        void addWork()
        {
            if (!string.IsNullOrWhiteSpace(work))
            {
                var tt = stringDelim != '#' ? TokenType.String : GetTokenType(work);
                retVal.Add(new Token(work, tt));
                work = "";
            }
        }

        for (var i = 0; i < code.Length; i++)
        {
            var c = code[i];
            char? lookAhead = null;
            if (i + 1 < code.Length)
                lookAhead = code[i + 1];

            //Are we in a string?
            if (stringDelim != '#')
            {
                //Escape Characters
                if (c == '%')
                {
                    i++;
                    work += EscapeChar(lookAhead);
                }

                //End of string?
                else if (c == stringDelim)
                {
                    if (stringDelim != '`')
                        addWork();
                    else
                    {
                        retVal.Add(new Token(new List<Token> { new Token("str", TokenType.Keyword), new Token(work, TokenType.String) }));
                        work = "";
                    }
                    stringDelim = '#';
                }

                //Carry on
                else
                    work += c;

                continue;
            }

            //Comments
            if (c == ';')
            {
                addWork();

                //Commented -out list ;{blah blah blah }
                if (lookAhead == '{')
                {
                    i++;
                    ScanTo(code, ref i, '}', '{');
                }

                //Regular comment, ; to EOL or ;
                else
                {
                    try
                    {
                        while (code[++i] != ';' && code[i] != '\r' && code[i] != '\n')
                        { }
                    }
                    catch (IndexOutOfRangeException)
                    { }
                }
                continue;
            }

            //Other stuff
            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    addWork();
                    break;

                case '{':
                    addWork();
                    var innerCode = ScanTo(code, ref i, '}', '{');
                    retVal.Add(new Token(ReadCode(innerCode, ro)));
                    break;

                case '}':
                    ro.Errors.Add("Unmatched end-bracket found");
                    break;

                case '"':
                    addWork();
                    stringDelim = c;
                    break;

                case '`':
                    addWork();
                    stringDelim = c;
                    break;

                default:
                    work += c;
                    break;
            }

        }

        if (stringDelim != '#')
            ro.Errors.Add("Unterminated string value: " + work);

        addWork();

        return retVal;
    }

    private static TokenType GetTokenType(string work)
    {
        if (KWs.Contains(work))
            return TokenType.Keyword;

        if (decimal.TryParse(work, out _))
            return TokenType.Number;

        return TokenType.Name;
    }

    public static ReaderOutput Read(string code)
    {
        var workingList = new List<Token>();
        var ro = new ReaderOutput();

        code = code.Trim();

        var work = "";

        for (var i = 0; i < code.Length; i++)
        {
            var c = code[i];
            char? lookAhead = null;
            if (i + 1 < code.Length)
                lookAhead = code[i + 1];

            //Escape characters?
            if (c == '%')
            {
                i++;
                work += EscapeChar(lookAhead);
                continue;
            }

            //Is there a tag?
            else if (c == '[')
            {
                if (!string.IsNullOrWhiteSpace(work))
                {
                    ro.Append(work, TokenType.Text);
                    work = "";
                }

                var tag = ScanTo(code, ref i, ']');

                ro.Append(tag, TokenType.Tag);
                continue;
            }

            //Code?
            if (c == '{')
            {
                if (!string.IsNullOrWhiteSpace(work))
                {
                    ro.Append(work, TokenType.Text);
                    work = "";
                }

                var codeBlock = ScanTo(code, ref i, '}', '{');
                ro.Append(ReadCode(codeBlock, ro));
                continue;
            }

            work += c;
        }

        if (!string.IsNullOrWhiteSpace(work))
            ro.Append(work, TokenType.Text);

        return ro;
    }

    private static string EscapeChar(char? lookAhead)
    {
        if (lookAhead == null)
            return "";

        switch (lookAhead)
        {
            case 's':
                return "&nbsp;";
            case 't':
                return "&nbsp;&nbsp;&nbsp;&nbsp;";
            case 'n':
                return "<br />";
            default:
                return lookAhead?.ToString() ?? "";
        }
    }
}