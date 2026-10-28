namespace WeChatNotifier.Core;

public sealed class SingleInstance : IDisposable
{
    readonly Mutex mutex;
    bool acquired;
    public bool Acquired => acquired;
    public SingleInstance(string name)
    {
        mutex = new Mutex(false, name);
        try { acquired = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { acquired = true; }
    }
    public void Dispose()
    {
        if (acquired) { mutex.ReleaseMutex(); acquired = false; }
        mutex.Dispose();
    }
}
