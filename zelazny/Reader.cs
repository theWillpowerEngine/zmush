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

            if (stringDelim != '#' || (scope == 0 && c != '[' && c != '{'))
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

        // var current = new List<Token>();
        // code = code.Trim();

        // var work = "";
        // var parenDepth = 0;
        // char stringDelim = '#';

        // bool quotedList = false;

        // int? scanToParenDepth = null;

        // if (ro == null) ro = new();

        // code = code.Replace("\r", "") + "\n";

        // int addedThisLine = 0;
        // Action appendWork = () =>
        // {
        //     //Just add strings as-is (as strings)
        //     if (stringDelim != '#')
        //     {
        //         current.Add(new Token(work, parenDepth));
        //         addedThisLine++;
        //     }

        //     //Otherwise parse the token -- any "strings" we end up with are not strings (they should be names or keywords)
        //     else if (!string.IsNullOrEmpty(work))
        //     {
        //         if (scanToParenDepth != null)
        //         {
        //             if (parenDepth == scanToParenDepth)
        //                 scanToParenDepth = null;
        //             else
        //             {
        //                 work = "";
        //                 return;
        //             }
        //         }

        //         addedThisLine++;
        //         current.Add(new Token(work, parenDepth));
        //     }
        //     work = "";
        // };

        // bool beginningOfLine = true;
        // bool implicitOuterList = true;

        // bool nextListIsCommentedOut = false;

        // string? scanToBuffer = "";
        // Action<string>? scanToBufferAction = null;
        // char? scanTo = null;

        // Stack<List<Token>> parenStack = new();

        // for (var i = 0; i < code.Length; i++)
        // {
        //     var c = code[i];
        //     char? lookAhead = null;
        //     if (i + 1 < code.Length)
        //         lookAhead = code[i + 1];

        //     //Handle ScanTo
        //     if (scanTo != null)
        //     {
        //         if (scanToBufferAction != null)
        //             scanToBuffer += c;

        //         if (c == scanTo)
        //         {
        //             scanTo = null;
        //             if (scanToBufferAction != null)
        //                 scanToBufferAction(scanToBuffer);
        //             scanToBuffer = "";
        //             scanToBufferAction = null;
        //             continue;
        //         }
        //         else
        //             continue;
        //     }

        //     //We're inside a string
        //     if (stringDelim != '#')
        //     {
        //         //Escape characters
        //         if (c == '%')
        //         {
        //             if (lookAhead == stringDelim)
        //             {
        //                 i += 1;
        //                 work += stringDelim.ToString();
        //                 continue;
        //             }

        //             else if (lookAhead == 's')
        //             {
        //                 i += 1;
        //                 work += " ";
        //                 continue;
        //             }
        //             else if (lookAhead == 't')
        //             {
        //                 i += 1;
        //                 work += "\t";
        //                 continue;
        //             }
        //             else if (lookAhead == 'n')
        //             {
        //                 i += 1;
        //                 work += Environment.NewLine;
        //                 continue;
        //             }
        //             else if (lookAhead == '%')
        //             {
        //                 i += 1;
        //                 work += '%';
        //                 continue;
        //             }

        //             else if (lookAhead == '[')
        //             {
        //                 i += 1;
        //                 work += '[';
        //                 continue;
        //             }
        //             else if (lookAhead == ']')
        //             {
        //                 i += 1;
        //                 work += ']';
        //                 continue;
        //             }

        //             else if (lookAhead == '{')
        //             {
        //                 i += 1;
        //                 work += '{';
        //                 continue;
        //             }
        //             else if (lookAhead == '}')
        //             {
        //                 i += 1;
        //                 work += '}';
        //                 continue;
        //             }
        //         }

        //         else if (c == stringDelim)
        //         {
        //             appendWork();
        //             stringDelim = '#';
        //             continue;
        //         }

        //         work += c;
        //         continue;
        //     }

        //     //////////////////////////////////////////////////
        //     //Newlines (including implicit outer lists)
        //     if (c == '\n')
        //     {
        //         appendWork();
        //         if (addedThisLine > 0 && implicitOuterList)
        //         {
        //             if (parenDepth == 0)
        //                 throw new Exception("Unexpected ']'");
        //             var newCur = current;
        //             current = parenStack.Pop();
        //             current.Add(new(newCur, parenDepth));
        //             parenDepth--;
        //         }

        //         beginningOfLine = true;
        //         addedThisLine = 0;
        //         implicitOuterList = parenDepth == 0;
        //     }

        //     /////////////////////////////////
        //     //General whitespace
        //     else if (char.IsWhiteSpace(c))
        //     {
        //         appendWork();
        //         continue;
        //     }

        //     ///////////////
        //     //Comments
        //     else if (c == ';')
        //     {
        //         appendWork();

        //         //;[...] -- comment out a list
        //         if (lookAhead == '[')
        //         {
        //             nextListIsCommentedOut = true;
        //             continue;
        //         }

        //         scanTo = '\n';
        //         continue;
        //     }

        //     //////////////////
        //     //List Start
        //     else if (c == '[')
        //     {
        //         if (beginningOfLine)
        //         {
        //             beginningOfLine = false;
        //             implicitOuterList = false;
        //         }

        //         appendWork();

        //         if (!nextListIsCommentedOut)
        //         {
        //             var newList = new List<Token>();
        //             if (quotedList)
        //             {
        //                 newList.Add(new Token("quote", parenDepth));
        //                 quotedList = false;
        //             }
        //             parenStack.Push(current);
        //             current = newList;
        //         }
        //         else
        //         {
        //             scanToParenDepth = parenDepth;
        //             nextListIsCommentedOut = false;
        //         }
        //         parenDepth++;
        //     }

        //     /////////////////
        //     //List End
        //     else if (c == ']')
        //     {
        //         if (beginningOfLine)
        //         {
        //             beginningOfLine = false;
        //         }
        //         appendWork();
        //         if (parenDepth == 0)
        //             throw new Exception("Unexpected ']'");
        //         var newCur = current;
        //         current = parenStack.Pop();
        //         parenDepth--;
        //     }

        //     ///////////////////////////////////////////////////////////////
        //     //Strings (and some ambiguous read cases like quoted lists)
        //     else if (c == '"' || c == '\'' || c == '`')
        //     {
        //         appendWork();

        //         //Auto-quoted list
        //         if (c == '\'' && lookAhead == '[')
        //         {
        //             if (beginningOfLine)
        //             {
        //                 beginningOfLine = false;
        //                 implicitOuterList = false;
        //             }

        //             quotedList = true;
        //             continue;
        //         }

        //         beginningOfLine = false;
        //         stringDelim = c;
        //     }

        //     /////////////////////////
        //     //Everything else
        //     else
        //     {
        //         if (beginningOfLine && implicitOuterList)
        //         {
        //             var newList = new List<Token>();
        //             parenStack.Push(current);
        //             current = newList;
        //             parenDepth++;
        //         }
        //         beginningOfLine = false;
        //         work += c;
        //     }
        // }

        // appendWork();

        // ro.List = new(current[0].Children, 0);

        // if (stringDelim != '#')
        //     ro.Errors.Add("Unterminated string:  " + work);

        // if (parenDepth != 0)
        //     ro.Errors.Add("Unclosed List, missing ']' x" + parenDepth);

        // return ro;
    }
}