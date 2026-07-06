public enum Job
{
    SaveZObject,
}

public class Scheduler
{
    public Scheduler()
    {

    }

    private List<Action> oneOffWorkQueue = new List<Action>();
    private Dictionary<Guid, (Action, int)> JobActiveQueue = new Dictionary<Guid, (Action, int)>();
    private Dictionary<Guid, int> AllJobs = new Dictionary<Guid, int>();

    private DateTime NextMinute = DateTime.MinValue;

    private object workQueueLock = new object();
    private object jobQueueLock = new object();

    public void QueueWork(Action work)
    {
        if (StopOrchestrator)
            return;

        lock (workQueueLock)
        {
            oneOffWorkQueue.Add(work);
        }
    }

    private void Worker()
    {
        Action worker;
        lock (workQueueLock)
        {
            if (oneOffWorkQueue.Count == 0)
                return;

            worker = oneOffWorkQueue[0];
            oneOffWorkQueue.RemoveAt(0);
        }

        try
        {
            worker();
        }
        catch (Exception ex)
        {
            Engine.Log("cron", $"Scheduler worker exception: {ex}");
            if (Engine.Settings.BreakOnExceptionDontUseThisUnlessYoureSmart)
                throw;
        }
    }

    private void JobWorker()
    {
        List<Guid> keys;
        lock (jobQueueLock)
            keys = JobActiveQueue.Keys.ToList();

        foreach (var key in keys)
        {
            var kvp = JobActiveQueue[key];
            var mins = kvp.Item2 - 1;
            if (mins <= 0)
            {
                QueueWork(kvp.Item1);
                mins = AllJobs[key];
            }
            else
                mins -= 1;

            lock (jobQueueLock)
                JobActiveQueue[key] = (kvp.Item1, mins);
        }

    }

    private bool StopOrchestrator = false;
    private bool Running = false;
    private void Orchestrator()
    {
        lock (workQueueLock)
        {
            if (Running)
                return;
            Running = true;
            Engine.Log("cron", "Scheduler started.");
        }

        while (!StopOrchestrator)
        {
            bool isMinute = false;

            if (DateTime.Now >= NextMinute)
            {
                NextMinute = DateTime.Now.AddMinutes(1);
                isMinute = true;
            }

            if (isMinute)
                JobWorker();

            Worker();

            Thread.Sleep(100);
        }

        lock (workQueueLock)
        {
            Running = false;
            StopOrchestrator = false;
        }

        Engine.Log("cron", "Scheduler stopped.");
    }


    public void Start()
    {
        var orchestratorThread = new Thread(Orchestrator);
        orchestratorThread.Start();
    }

    public void Stop(bool finishCurrentQueue, bool wait)
    {
        StopOrchestrator = true;

        if (finishCurrentQueue)
        {
            Engine.Log("cron", $"Finishing {oneOffWorkQueue.Count} workers before stopping scheduler (new jobs will not be queued)...");
            while (oneOffWorkQueue.Count > 0)
            {
                Thread.Sleep(100);
            }
        }

        if (wait)
        {
            if (Running)
                Engine.Log("cron", $"Waiting for scheduler to stop fully.");

            while (Running)
            {
                Thread.Sleep(100);
            }
        }
    }

    internal Guid ScheduleJob(Action autoSave, int autoSaveDur)
    {
        var jobId = Guid.NewGuid();

        lock (jobQueueLock)
        {
            AllJobs.Add(jobId, autoSaveDur);
            JobActiveQueue.Add(jobId, (autoSave, autoSaveDur));
        }

        return jobId;
    }

    internal int GetJobInterval(Guid workerJobId)
    {
        int interval;

        lock (workQueueLock)
        {
            if (!AllJobs.TryGetValue(workerJobId, out interval))
                return -1;
        }

        return interval;
    }

    internal bool UpdateJobInterval(Guid workerJobId, int autoSaveMinutes)
    {
        lock (workQueueLock)
        {
            if (!AllJobs.ContainsKey(workerJobId))
                return false;

            AllJobs[workerJobId] = autoSaveMinutes;
        }

        return true;
    }
}