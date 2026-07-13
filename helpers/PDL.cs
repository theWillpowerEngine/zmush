public static class PDL
{
    public static string Add(string list, string toAdd)
    {
        var ret = $"|{list}|{toAdd}|";
        ret = ret.Replace("||", "|");
        return ret;
    }

    public static string RemoveAll(string list, string toRemove)
    {
        var ret = $"|{list}|{toRemove}|";
        ret = ret.Replace($"|{toRemove}|", "|");
        ret = ret.Replace("||", "|");
        return ret;
    }

    public static string RemoveAtIndex(string list, int index)
    {
        var parts = list.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (index < 1 || index > parts.Length)
            return list;

        var ret = string.Join("|", parts.Where((p, i) => i != index - 1));
        return ret;
    }

    public static int FindIndex(string list, string searchFor)
    {
        var parts = list.Split('|', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == searchFor)
                return i + 1;
        }
        return 0;
    }

    public static List<string> Split(string list)
    {
        var parts = list.Split('|', StringSplitOptions.RemoveEmptyEntries);
        return parts.ToList();
    }
}