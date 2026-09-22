namespace AltTapHold;

/// <summary>Pure, single-threaded policy. The hook thread must call every public member.</summary>
public sealed class AltStateMachine
{
    public const long TapThresholdMs = 150;
    public const long MaximumHoldMs = 5000;
    private readonly IKeySender sender;
    private readonly IModifierReader modifiers;
    private readonly Slot[] slots = [new(), new()];
    private bool releaseLeft, releaseRight;

    private sealed class Slot { public AltPhase Phase; public long StartedAt; }
    public AltStateMachine(IKeySender sender, IModifierReader modifiers) { this.sender = sender; this.modifiers = modifiers; }
    public AltPhase Phase(AltSide side) => slots[(int)side].Phase;
    public bool HasReleasePending => releaseLeft || releaseRight;

    // Returns true when the caller must suppress the original physical event.
    public bool Handle(KeyStroke physical, long nowMs)
    {
        Expire(nowMs);
        if (HasReleasePending && !FlushReleases()) return false; // fail open: do not indefinitely eat real input.
        var side = SideOf(physical.VirtualKey);
        if (side is not null) return HandleAlt(side.Value, physical.IsKeyUp, nowMs);
        if (!physical.IsKeyUp && AnyPending())
        {
            var output = ActivatePending();
            output.Add(physical); // preserves Alt down -> other-key down ordering in one SendInput call.
            if (!SendOrRecover(output)) return true;
            return true;
        }
        return false;
    }

    public void Tick(long nowMs) { Expire(nowMs); if (HasReleasePending) FlushReleases(); }
    public void Shutdown()
    {
        var output = new List<KeyStroke>();
        for (var i = 0; i < 2; i++) if (slots[i].Phase == AltPhase.Active) output.Add(KeyStroke.Alt((AltSide)i, true));
        Array.Clear(slots); // state cannot be reused after shutdown
        if (output.Count > 0 && !sender.Send(output))
        {
            MarkRelease(output);
        }
        FlushReleases();
    }

    private bool HandleAlt(AltSide side, bool up, long now)
    {
        var slot = slots[(int)side];
        if (!up)
        {
            if (slot.Phase != AltPhase.Idle) return true; // suppress physical repeat.
            var other = side == AltSide.Left ? AltSide.Right : AltSide.Left;
            if (slots[(int)other].Phase != AltPhase.Idle)
            {
                var output = ActivatePending(); output.Add(KeyStroke.Alt(side, false));
                if (SendOrRecover(output)) { slot.Phase = AltPhase.Active; slot.StartedAt = now; }
                return true;
            }
            slot.StartedAt = now;
            if (ModifierWasAlreadyHeld())
            {
                slot.Phase = AltPhase.Active;
                SendOrRecover([KeyStroke.Alt(side, false)]);
            }
            else slot.Phase = AltPhase.Pending;
            return true;
        }
        if (slot.Phase == AltPhase.Idle) return false;
        if (slot.Phase == AltPhase.Pending && now - slot.StartedAt < TapThresholdMs)
        {
            slot.Phase = AltPhase.Idle;
            SendOrRecover([KeyStroke.Ime(side, false), KeyStroke.Ime(side, true)]);
            return true;
        }
        // A delayed timer must not turn a threshold-boundary up into a tap.
        var wasPending = slot.Phase == AltPhase.Pending;
        slot.Phase = AltPhase.Idle;
        SendOrRecover(wasPending ? [KeyStroke.Alt(side, false), KeyStroke.Alt(side, true)] : [KeyStroke.Alt(side, true)]);
        return true;
    }

    private void Expire(long now)
    {
        var output = new List<KeyStroke>();
        for (var i = 0; i < 2; i++)
        {
            var slot = slots[i];
            if (slot.Phase == AltPhase.Idle || now - slot.StartedAt < MaximumHoldMs) continue;
            if (slot.Phase == AltPhase.Active) output.Add(KeyStroke.Alt((AltSide)i, true));
            slot.Phase = AltPhase.Idle; // never create a stale Alt down after the fixed deadline.
        }
        // Deadline processing above deliberately wins if a delayed timer observes both boundaries.
        for (var i = 0; i < 2; i++)
        {
            var slot = slots[i];
            if (slot.Phase == AltPhase.Pending && now - slot.StartedAt >= TapThresholdMs && now - slot.StartedAt < MaximumHoldMs)
            {
                slot.Phase = AltPhase.Active;
                output.Add(KeyStroke.Alt((AltSide)i, false));
            }
        }
        if (output.Count > 0 && !sender.Send(output))
        {
            MarkRelease(output);
            // A failed threshold activation may have injected a prefix. Do not leave it Active.
            foreach (var down in output.Where(x => !x.IsKeyUp && SideOf(x.VirtualKey) is not null)) slots[(int)SideOf(down.VirtualKey)!.Value].Phase = AltPhase.Idle;
        }
    }
    private List<KeyStroke> ActivatePending()
    {
        var output = new List<KeyStroke>();
        for (var i = 0; i < 2; i++) if (slots[i].Phase == AltPhase.Pending) { slots[i].Phase = AltPhase.Active; output.Add(KeyStroke.Alt((AltSide)i, false)); }
        return output;
    }
    private bool SendOrRecover(IReadOnlyList<KeyStroke> output)
    {
        if (output.Count == 0 || sender.Send(output)) return true;
        // SendInput may have accepted a prefix: release every Alt down from this attempt and abandon state.
        MarkRelease(output.Where(x => !x.IsKeyUp && SideOf(x.VirtualKey) is not null).Select(x => KeyStroke.Alt(SideOf(x.VirtualKey)!.Value, true)));
        foreach (var slot in slots) slot.Phase = AltPhase.Idle;
        return false;
    }
    private void MarkRelease(IEnumerable<KeyStroke> output) { foreach (var x in output) { if (x.VirtualKey == Keys.LAlt) releaseLeft = true; if (x.VirtualKey == Keys.RAlt) releaseRight = true; } }
    private bool FlushReleases()
    {
        var output = new List<KeyStroke>(); if (releaseLeft) output.Add(KeyStroke.Alt(AltSide.Left, true)); if (releaseRight) output.Add(KeyStroke.Alt(AltSide.Right, true));
        if (output.Count == 0 || !sender.Send(output)) return false;
        releaseLeft = releaseRight = false; return true;
    }
    private bool AnyPending() => slots.Any(x => x.Phase == AltPhase.Pending);
    private bool ModifierWasAlreadyHeld() => modifiers.IsHeld(Keys.Shift) || modifiers.IsHeld(Keys.Control) || modifiers.IsHeld(Keys.LWin) || modifiers.IsHeld(Keys.RWin);
    private static AltSide? SideOf(uint key) => key == Keys.LAlt ? AltSide.Left : key == Keys.RAlt ? AltSide.Right : null;
}
