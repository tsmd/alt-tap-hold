using AltTapHold;
using System.Runtime.InteropServices;

var suite = new StateTests();
suite.Run();
Console.WriteLine("All state-machine tests passed.");

sealed class StateTests
{
    private readonly FakeSender sender = new();
    private readonly FakeModifiers modifiers = new();
    private AltStateMachine New() { sender.Events.Clear(); sender.Succeed = true; modifiers.Held.Clear(); return new(sender, modifiers); }
    public void Run()
    {
        Tap(AltSide.Left, Keys.NonConvert); Tap(AltSide.Right, Keys.Convert);
        BoundaryIsHold(); FastAltTab(); PreHeldModifier(); OverlappingAlts(); Deadline(); SendFailureReleases(); NativeLayout();
    }
    private void Tap(AltSide side, uint ime)
    {
        var m = New(); var vk = side == AltSide.Left ? Keys.LAlt : Keys.RAlt;
        Equal(true, m.Handle(new(vk, 0, false), 0)); Equal(true, m.Handle(new(vk, 0, true), 30));
        Events(new(ime, 0, false), new(ime, 0, true));
    }
    private void BoundaryIsHold()
    {
        var m = New(); m.Handle(new(Keys.LAlt, 0, false), 0); m.Handle(new(Keys.LAlt, 0, true), 150);
        Events(KeyStroke.Alt(AltSide.Left, false), KeyStroke.Alt(AltSide.Left, true));
    }
    private void FastAltTab()
    {
        var m = New(); m.Handle(new(Keys.LAlt, 0, false), 0); Equal(true, m.Handle(new(0x09, 0x0F, false), 10));
        Events(KeyStroke.Alt(AltSide.Left, false), new(0x09, 0x0F, false)); Equal(true, m.Handle(new(Keys.LAlt, 0, true), 20));
        Events(KeyStroke.Alt(AltSide.Left, true));
    }
    private void PreHeldModifier()
    {
        var m = New(); modifiers.Held.Add(Keys.Shift); m.Handle(new(Keys.RAlt, 0, false), 0);
        Events(KeyStroke.Alt(AltSide.Right, false)); m.Handle(new(Keys.RAlt, 0, true), 5); Events(KeyStroke.Alt(AltSide.Right, true));
    }
    private void OverlappingAlts()
    {
        var m = New(); m.Handle(new(Keys.LAlt, 0, false), 0); m.Handle(new(Keys.RAlt, 0, false), 5);
        Events(KeyStroke.Alt(AltSide.Left, false), KeyStroke.Alt(AltSide.Right, false));
        m.Handle(new(Keys.LAlt, 0, true), 10); Events(KeyStroke.Alt(AltSide.Left, true));
        m.Handle(new(Keys.RAlt, 0, true), 20); Events(KeyStroke.Alt(AltSide.Right, true));
    }
    private void Deadline()
    {
        var m = New(); m.Handle(new(Keys.LAlt, 0, false), 0); m.Tick(5000); Events();
        Equal(false, m.Handle(new(Keys.LAlt, 0, true), 5001)); Events();
        m.Handle(new(Keys.RAlt, 0, false), 6000); m.Handle(new(Keys.RAlt, 0, true), 6010); Events(new(Keys.Convert, 0, false), new(Keys.Convert, 0, true));
    }
    private void SendFailureReleases()
    {
        var m = New(); m.Handle(new(Keys.LAlt, 0, false), 0); sender.Succeed = false; m.Tick(150); sender.Succeed = true; m.Tick(151);
        Events(KeyStroke.Alt(AltSide.Left, true));
    }
    private static void NativeLayout()
    {
        var expectedInput = IntPtr.Size == 8 ? 40 : 28;
        var expectedHook = IntPtr.Size == 8 ? 24 : 20;
        Equal(expectedInput, Marshal.SizeOf<NativeInput.Input>());
        Equal(expectedHook, Marshal.SizeOf<NativeInput.KbdLlHookStruct>());
        Equal(16, (int)Marshal.OffsetOf<NativeInput.KbdLlHookStruct>(nameof(NativeInput.KbdLlHookStruct.dwExtraInfo)));
        Equal(IntPtr.Size == 8 ? 16 : 12, (int)Marshal.OffsetOf<NativeInput.KeybdInput>(nameof(NativeInput.KeybdInput.dwExtraInfo)));
    }
    private void Events(params KeyStroke[] expected) { Equal(string.Join('|', expected), string.Join('|', sender.Events)); sender.Events.Clear(); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}"); }
    private sealed class FakeSender : IKeySender { public readonly List<KeyStroke> Events = []; public bool Succeed = true; public bool Send(IReadOnlyList<KeyStroke> keys) { if (Succeed) Events.AddRange(keys); return Succeed; } }
    private sealed class FakeModifiers : IModifierReader { public readonly HashSet<uint> Held = []; public bool IsHeld(uint key) => Held.Contains(key); }
}
