namespace WeChatNotifier.Core;

public enum Presentation { Skip, Normal, Silent }
public enum Decision { Started, Disabled, Foreground, Startup, Duplicate, GlobalCooldown, InvalidSource, SystemBlocked, Submitted, DeliveryFailed, Stopped, ActivationUnavailable, DeferredQueued, DeferredDropped }
public readonly record struct Attention(string Identity, bool Verified, bool Foreground, string? ConversationName = null, string? RouteTicket = null, string? SourceContext = null)
{ public override string ToString() => "Attention[redacted]"; }
public static class AttentionValidation
{
    public static bool ActivationIsCurrent(long captured, long current, bool enabled, bool stopping) =>
        enabled && !stopping && captured == current;
    public static bool CanEmit(Attention original, Attention? current) => original.Verified &&
        !string.IsNullOrEmpty(original.Identity) && !string.IsNullOrEmpty(original.SourceContext) &&
        current is { Verified: true, Foreground: false } fresh && fresh.Identity == original.Identity &&
        fresh.SourceContext == original.SourceContext && fresh.ConversationName == original.ConversationName &&
        ConversationParser.ValidName(fresh.ConversationName);
}
public readonly record struct PlatformState(bool NotificationsEnabled, bool QuerySucceeded, int State);
public readonly record struct Delivery(bool Submitted, string Reason);
public readonly record struct NoticeRequest(Presentation Presentation, string SubmissionId, string? ConversationName = null, string? RouteTicket = null)
{ public override string ToString() => "NoticeRequest[redacted]"; }
public sealed record CoreState(bool Running, bool Enabled, Decision Decision, long Received, long Submitted, string Detail)
{ public int Pending { get; init; } public long DeferredDropped { get; init; } public long DeferredFailed { get; init; } }
public interface IClock { long Milliseconds { get; } }
public interface IAttentionSource : IDisposable
{
    event Action<Attention>? Attention;
    event Action? Ended;
    void Start(bool enabled);
    void SetEnabled(bool enabled);
    void Stop();
}
public interface INoticeSink : IDisposable
{
    PlatformState ReadState();
    Delivery Submit(NoticeRequest request);
}
public readonly record struct ActivationResult(bool Opened, string Explanation);
public interface IAppActivation { ActivationResult Request(); }

// Legacy/generic activation remains unavailable; named routing has separate gates/tickets.
public sealed class UnavailableActivation : IAppActivation
{
    public ActivationResult Request() => new(false, "暂不可自动唤回微信，请使用微信自己的托盘图标。");
}

public sealed class NotificationCore : IDisposable
{
    readonly IAttentionSource source;
    readonly INoticeSink sink;
    readonly IClock clock;
    readonly IAppActivation activation;
    readonly IOneShotScheduler? scheduler;
    readonly Func<Attention, Attention?>? revalidate;
    sealed record Pending(Attention Event, long AddedAt, long ExpiresAt);
    readonly List<Pending> pending = [];
    IDisposable? scheduled;
    long generation, droppedDeferred, failedDeferred;
    string? pendingContext;
    readonly Dictionary<string, long> submittedByIdentity = [];
    readonly object sync = new();
    bool running, enabled, disposed;
    long startedAt, submittedAt = long.MinValue / 2, received, submitted;
    public event Action<CoreState>? Changed;
    public event Action? Stopped;
    public NotificationCore(IAttentionSource source, INoticeSink sink, IClock clock, IAppActivation activation,
        IOneShotScheduler? scheduler = null, Func<Attention, Attention?>? revalidate = null)
    { this.source = source; this.sink = sink; this.clock = clock; this.activation = activation;
      this.scheduler = scheduler; this.revalidate = revalidate; }

    public void Start(bool enable)
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (running) return;
            enabled = enable; startedAt = clock.Milliseconds; running = true;
            source.Attention += Receive; source.Ended += SourceEnded;
            try { source.Start(enabled); if (running) Publish(Decision.Started, "通用关注提醒，不保证逐消息"); }
            catch { Stop(); throw; }
        }
    }
    public void SetEnabled(bool value)
    {
        lock (sync)
        {
            if (!running || enabled == value) return;
            // Switch off first so queued events cannot escape while deregistration runs.
            enabled = value;
            ClearPending();
            if (value) startedAt = clock.Milliseconds;
            try { source.SetEnabled(value); }
            catch { Stop(); throw; }
            Publish(value ? Decision.Started : Decision.Disabled, value ? "已启用" : "已停用");
        }
    }
    void Receive(Attention e)
    {
        lock (sync)
        {
            if (!running || disposed) return;
            received++;
            long now = clock.Milliseconds;
            if (!enabled) { Publish(Decision.Disabled); return; }
            if (!e.Verified || string.IsNullOrEmpty(e.Identity)) { Publish(Decision.InvalidSource); return; }
            if (e.SourceContext != null && pendingContext != null && e.SourceContext != pendingContext) ClearPending();
            if (e.Foreground) { RemoveIdentity(e.Identity); Publish(Decision.Foreground); return; }
            if (now - startedAt < 3000) { Publish(Decision.Startup); return; }
            if (submittedByIdentity.TryGetValue(e.Identity, out long prior) && now - prior < 30000)
            { Defer(e, now); Publish(Decision.Duplicate); return; }
            if (now - submittedAt < 10000) { Defer(e, now); Publish(Decision.GlobalCooldown); return; }
            RemoveIdentity(e.Identity);
            try
            {
                var system = sink.ReadState();
                var mode = SelectPresentation(system);
                if (mode == Presentation.Skip) { ClearPending(); Publish(Decision.SystemBlocked); return; }
                var result = sink.Submit(new(mode, Guid.NewGuid().ToString("N"), e.ConversationName, e.RouteTicket));
                if (!result.Submitted) { ClearPending(); Publish(Decision.DeliveryFailed, result.Reason); return; }
                if (submittedByIdentity.Count >= 32 && !submittedByIdentity.ContainsKey(e.Identity)) submittedByIdentity.Clear();
                submittedByIdentity[e.Identity] = clock.Milliseconds;
                submittedAt = clock.Milliseconds; submitted++;
                Publish(Decision.Submitted, mode.ToString());
                Arm();
            }
            catch (Exception ex) { ClearPending(); Publish(Decision.DeliveryFailed, ex.GetType().Name); }
        }
    }
    public void InvalidateDeferred() { lock (sync) ClearPending(); }
    void ClearPending()
    {
        ++generation; scheduled?.Dispose(); scheduled = null;
        droppedDeferred += pending.Count; pending.Clear(); pendingContext = null;
    }
    void RemoveIdentity(string identity)
    {
        droppedDeferred += pending.RemoveAll(x => x.Event.Identity == identity);
        Arm();
    }
    long Due(Pending item) => Math.Max(submittedAt + 10000,
        submittedByIdentity.TryGetValue(item.Event.Identity, out long prior) ? prior + 30000 : long.MinValue / 2);
    void Defer(Attention e, long now)
    {
        if (scheduler == null || revalidate == null || !ConversationParser.ValidName(e.ConversationName) || string.IsNullOrEmpty(e.SourceContext)) return;
        try { if (SelectPresentation(sink.ReadState()) == Presentation.Skip) { ClearPending(); Publish(Decision.SystemBlocked); return; } }
        catch { ClearPending(); failedDeferred++; Publish(Decision.DeliveryFailed, "DEFERRED_PLATFORM_FAILED"); return; }
        if (pendingContext != null && pendingContext != e.SourceContext) ClearPending();
        pendingContext = e.SourceContext;
        droppedDeferred += pending.RemoveAll(x => x.ExpiresAt <= now || now < x.AddedAt);
        int index = pending.FindIndex(x => x.Event.Identity == e.Identity);
        var safe = e with { RouteTicket = null }; // Never retain, refresh, or replay an old route ticket.
        if (index >= 0) pending[index] = pending[index] with { Event = safe };
        else if (pending.Count < 8) pending.Add(new(safe, now, now + 90000));
        else { droppedDeferred++; Publish(Decision.DeferredDropped, "CAPACITY"); return; }
        Publish(Decision.DeferredQueued); Arm();
    }
    void Arm()
    {
        ++generation; scheduled?.Dispose(); scheduled = null;
        if (!running || !enabled || disposed || pending.Count == 0 || scheduler == null) return;
        long ticket = generation, now = clock.Milliseconds;
        long due = pending.Min(x => Math.Min(x.ExpiresAt, Due(x)));
        try { scheduled = scheduler.Schedule(TimeSpan.FromMilliseconds(Math.Max(1, due - now)), () => Flush(ticket)); }
        catch { ClearPending(); Publish(Decision.DeferredDropped, "SCHEDULER_FAILED"); }
    }
    void Flush(long ticket)
    {
        lock (sync)
        {
            if (ticket != generation || !running || !enabled || disposed) return;
            scheduled?.Dispose(); scheduled = null;
            long now = clock.Milliseconds;
            droppedDeferred += pending.RemoveAll(x => x.ExpiresAt <= now || now < x.AddedAt);
            try
            {
                var mode = SelectPresentation(sink.ReadState());
                if (mode == Presentation.Skip) { ClearPending(); Publish(Decision.SystemBlocked); return; }
                foreach (var item in pending.ToArray())
                {
                    if (Due(item) > now) continue;
                    pending.Remove(item); // At most one delivery attempt, including exceptions/failures.
                    Attention? fresh;
                    try { fresh = revalidate?.Invoke(item.Event); } catch { fresh = null; }
                    if (fresh is not { } e || !e.Verified || e.Foreground || e.Identity != item.Event.Identity ||
                        e.SourceContext != item.Event.SourceContext || e.ConversationName != item.Event.ConversationName ||
                        !ConversationParser.ValidName(e.ConversationName))
                    { droppedDeferred++; Publish(Decision.DeferredDropped, "REVALIDATION_REJECTED"); continue; }
                    var delivery = sink.Submit(new(mode, Guid.NewGuid().ToString("N"), e.ConversationName, null));
                    if (!delivery.Submitted) { failedDeferred++; ClearPending(); Publish(Decision.DeliveryFailed, "DEFERRED_DELIVERY_FAILED"); return; }
                    if (submittedByIdentity.Count >= 32 && !submittedByIdentity.ContainsKey(e.Identity)) submittedByIdentity.Clear();
                    submittedByIdentity[e.Identity] = submittedAt = clock.Milliseconds; submitted++;
                    Publish(Decision.Submitted, mode.ToString()); break;
                }
            }
            catch { failedDeferred++; ClearPending(); Publish(Decision.DeliveryFailed, "DEFERRED_DELIVERY_FAILED"); return; }
            Arm();
        }
    }
    public static Presentation SelectPresentation(PlatformState system) =>
        !system.NotificationsEnabled || !system.QuerySucceeded ? Presentation.Skip : system.State switch
        { 5 => Presentation.Normal, 2 or 3 or 4 or 6 or 7 => Presentation.Silent, _ => Presentation.Skip };
    public ActivationResult RequestActivation()
    {
        lock (sync)
        {
            if (!running || disposed) return new(false, "通知核心未运行；不会启动微信。");
            var result = activation.Request();
            if (!result.Opened) Publish(Decision.ActivationUnavailable, result.Explanation);
            return result;
        }
    }
    void SourceEnded() => Stop();
    public void Stop()
    {
        lock (sync)
        {
            if (disposed) return;
            running = false; enabled = false;
            ClearPending();
            disposed = true;
            source.Attention -= Receive; source.Ended -= SourceEnded;
            try { source.Stop(); }
            finally
            {
                try { source.Dispose(); }
                finally { try { sink.Dispose(); } finally { Publish(Decision.Stopped); Stopped?.Invoke(); } }
            }
        }
    }
    void Publish(Decision decision, string detail = "")
    {
        // Optional diagnostics must never change delivery or cleanup behavior.
        if (Changed is not { } listeners) return;
        var state = new CoreState(running, enabled, decision, received, submitted, detail)
        { Pending = pending.Count, DeferredDropped = droppedDeferred, DeferredFailed = failedDeferred };
        foreach (Action<CoreState> listener in listeners.GetInvocationList())
            try { listener(state); } catch { }
    }
    public void Dispose() => Stop();
}
