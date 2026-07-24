public static class Extensions
{
    public static void DoWithNewIterator(this Registers? r, string newIterator, Action action)
    {
        if (r == null)
            action();
        else
        {
            var oldIterator = r.IterativeElement;
            r.IterativeElement = newIterator;

            action();

            r.IterativeElement = oldIterator;
        }
    }

}