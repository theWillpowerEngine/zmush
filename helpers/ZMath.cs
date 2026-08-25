public static class ZMath
{
    public static bool GT(string a, string b)
    {
        if (long.TryParse(a, out var aNum) && long.TryParse(b, out var bNum))
            return aNum > bNum;

        if (double.TryParse(a, out var aDouble) && double.TryParse(b, out var bDouble))
            return aDouble > bDouble;

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase) > 0;

    }
    public static bool LT(string a, string b)
    {
        if (long.TryParse(a, out var aNum) && long.TryParse(b, out var bNum))
            return aNum < bNum;

        if (double.TryParse(a, out var aDouble) && double.TryParse(b, out var bDouble))
            return aDouble < bDouble;

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase) < 0;
    }
}