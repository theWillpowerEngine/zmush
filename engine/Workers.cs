public static class Workers
{
    private static HashSet<ZObject> _dirtyZObs = new HashSet<ZObject>();
    private static object _lock = new();

    private static Guid WorkerJobId;

    public static void QueueForSave(ZObject z)
    {
        lock (_lock)
        {
            if (_dirtyZObs.Contains(z))
                return;

            _dirtyZObs.Add(z);
        }
    }

    public static void AutoSave()
    {
        if (!Engine.Settings.AutoSaveEnabled)
            return;

        //Self-adjust the timer so the server doesn't have to reboot if the admin 'sts autosavemins'
        if (Engine.Settings.AutoSaveMinutes != Engine.Scheduler.GetJobInterval(WorkerJobId))
            Engine.Scheduler.UpdateJobInterval(WorkerJobId, Engine.Settings.AutoSaveMinutes);

        List<ZObject> toSave;
        lock (_lock)
        {
            if (!_dirtyZObs.Any())
                return;

            toSave = _dirtyZObs.ToList();
        }

        if (toSave.Count == 0)
        {
            Engine.Log("Nothing to autosave.");
            return;
        }

        Engine.Log($"Saving {toSave.Count} dirty objects...");
        var start = DateTime.Now;
        foreach (var z in toSave)
        {
            z.Save(true);
        }
        Engine.Log($"Saved {toSave.Count} dirty objects in {(DateTime.Now - start).TotalMilliseconds} ms.");

        lock (_lock)
            _dirtyZObs.Clear();
    }

    internal static void RegisterInitialWorkers(Scheduler scheduler)
    {
        var autoSaveDur = Engine.Settings.AutoSaveMinutes;
        if (autoSaveDur <= 0)
            autoSaveDur = 5;

        WorkerJobId = scheduler.ScheduleJob(AutoSave, autoSaveDur);
        Engine.Log("cron", $"Registered AutoSave worker to run every {autoSaveDur} minutes.");
    }
}