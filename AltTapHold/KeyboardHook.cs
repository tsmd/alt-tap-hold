using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AltTapHold;

internal sealed class KeyboardHook : IDisposable
{
    private readonly AltStateMachine machine = new(new WindowsKeySender(), new WindowsModifierReader());
    private readonly NativeInput.HookProc callback;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 10 };
    private nint handle;
    internal KeyboardHook() { callback = Proc; timer.Tick += (_, _) => machine.Tick(ClockMs()); }
    internal void Start()
    {
        handle = NativeInput.SetWindowsHookExW(NativeInput.WhKeyboardLl, callback, 0, 0);
        if (handle == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Keyboard hook registration failed.");
        timer.Start();
    }
    private nint Proc(int code, nint wParam, nint lParam)
    {
        try
        {
            if (code < 0) return NativeInput.CallNextHookEx(handle, code, wParam, lParam);
            var data = Marshal.PtrToStructure<NativeInput.KbdLlHookStruct>(lParam);
            if ((data.flags & NativeInput.LlkhfInjected) != 0 || data.dwExtraInfo == NativeInput.Marker) return NativeInput.CallNextHookEx(handle, code, wParam, lParam);
            var message = unchecked((int)wParam);
            if (message is not (NativeInput.WmKeyDown or NativeInput.WmKeyUp or NativeInput.WmSysKeyDown or NativeInput.WmSysKeyUp)) return NativeInput.CallNextHookEx(handle, code, wParam, lParam);
            var up = message is NativeInput.WmKeyUp or NativeInput.WmSysKeyUp;
            var physical = new KeyStroke(data.vkCode, data.scanCode, up, (data.flags & NativeInput.LlkhfExtended) != 0);
            if (machine.Handle(physical, ClockMs())) return 1;
        }
        catch { /* Never allow managed exceptions to escape the hook callback. */ }
        return NativeInput.CallNextHookEx(handle, code, wParam, lParam);
    }
    private static long ClockMs() => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
    public void Dispose() { timer.Stop(); machine.Shutdown(); if (handle != 0) { NativeInput.UnhookWindowsHookEx(handle); handle = 0; } timer.Dispose(); }
}
