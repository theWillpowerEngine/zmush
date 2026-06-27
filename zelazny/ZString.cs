using Microsoft.VisualBasic;

public class ZString
{
    private string _s = "";
    private bool dirty = true;

    public string S
    {
        get
        {
            return _s;
        }
        set
        {
            _s = value ?? "";
            dirty = true;
        }
    }

    public static ZString operator +(ZString a, ZString b)
    {
        return new ZString { S = a.S + b.S };
    }

    public static ZString operator +(ZString a, string b)
    {
        return new ZString { S = a.S + b };
    }

    public static ZString operator +(string a, ZString b)
    {
        return new ZString { S = a + b.S };
    }

    public static bool operator ==(ZString a, ZString b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
        return a.S == b.S;
    }

    public static bool operator !=(ZString a, ZString b)
    {
        return !(a == b);
    }

    public static bool operator ==(ZString a, string b)
    {
        if (ReferenceEquals(a, null)) return b == null;
        return a.S == b;
    }

    public static bool operator !=(ZString a, string b)
    {
        return !(a == b);
    }

    public static bool operator ==(string a, ZString b)
    {
        if (ReferenceEquals(b, null)) return a == null;
        return a == b.S;
    }

    public static bool operator !=(string a, ZString b)
    {
        return !(a == b);
    }

    public static implicit operator ZString(string s)
    {
        return new ZString { S = s ?? "" };
    }

    public static implicit operator string(ZString z)
    {
        return z?.S ?? "";
    }

    public override bool Equals(object obj)
    {
        if (obj is ZString zs)
            return S == zs.S;
        if (obj is string s)
            return S == s;
        return false;
    }

    public override string ToString()
    {
        return S;
    }


    ReaderOutput? ro = null;

    public string Evaluate(ZObject context)
    {
        int i = -1;
        return Evaluate(context, ref i);
    }
    public string Evaluate(ZObject context, ref int quota)
    {
        if (dirty || ro == null)
        {
            ro = Reader.Read(_s);
            dirty = false;
        }

        if (!ro.WasSuccessful)
            return $"[Errors: {string.Join(", ", ro.Errors)}]";

        var evalled = "";
        foreach (var token in ro.List.Children)
        {
            if (quota == 0)
                return "[Limit:  Quota exceeded]";

            evalled += Interpreter.Evaluate(token, context, ref quota) + " ";
        }

        return evalled.TrimEnd();
    }

    internal static string Eval(string s, ZObject context, ref int quota)
    {
        ZString zs = s;
        return zs.Evaluate(context, ref quota);
    }

    internal static string Eval(string s, ZObject context)
    {
        ZString zs = s;
        return zs.Evaluate(context);
    }
}