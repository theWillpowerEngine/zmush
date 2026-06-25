public static partial class Engine
{
    internal static string RenderFrame(long userId)
    {
        var user = Objects[userId];
        var loc = Objects[user.Location];

        var ret = "";
        ret += $"<b>{loc.Name}</b>%n";
        ret += loc.Desc;

        return ZString.Eval(ret, loc);
    }
}