public static class PDL
{
    public static string Add(string s1, string s2)
    {
        var ret = $"|{s1}|{s2}|";
        ret = ret.Replace("||", "|");
        return ret;
    }

    public static string RemoveAll(string s1, string s2)
    {
        var ret = $"|{s1}|{s2}|";
        ret = ret.Replace($"|{s2}|", "|");
        ret = ret.Replace("||", "|");
        return ret;
    }

    public static string RemoveAtIndex(string s1, int index)
    {
        var parts = s1.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (index < 1 || index > parts.Length)
            return s1;

        var ret = string.Join("|", parts.Where((p, i) => i != index - 1));
        return ret;
    }

    public static int FindIndex(string s1, string s2)
    {
        var parts = s1.Split('|', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == s2)
                return i + 1;
        }
        return 0;
    }
}