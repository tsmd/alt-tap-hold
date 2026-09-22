using System.Runtime.InteropServices;

namespace AltTapHold;

internal static class NativeInput
{
    internal const int WhKeyboardLl = 13, WmKeyDown = 0x0100, WmKeyUp = 0x0101, WmSysKeyDown = 0x0104, WmSysKeyUp = 0x0105;
    internal const uint LlkhfExtended = 0x01, LlkhfInjected = 0x10, KeyeventfExtended = 0x0001, KeyeventfKeyUp = 0x0002;
    internal const uint InputKeyboard = 1;
    internal static readonly nuint Marker = unchecked((nuint)0x4154484D); // ATHM

    [StructLayout(LayoutKind.Sequential)] internal struct KbdLlHookStruct { public uint vkCode, scanCode, flags, time; public nuint dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { public uint type; public InputUnion U; }
    // INPUT's union is mouse-sized, not keyboard-sized: this keeps sizeof(INPUT) at 28 (x86) / 40 (x64).
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion { [FieldOffset(0)] public MouseInput mi; [FieldOffset(0)] public KeybdInput ki; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { public int dx, dy; public uint mouseData, dwFlags, time; public nuint dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] internal struct KeybdInput { public ushort wVk, wScan; public uint dwFlags, time; public nuint dwExtraInfo; }
    internal delegate nint HookProc(int code, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)] internal static extern nint SetWindowsHookExW(int idHook, HookProc callback, nint module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, [In] Input[] inputs, int size);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int vKey);
}

internal sealed class WindowsKeySender : IKeySender
{
    public bool Send(IReadOnlyList<KeyStroke> strokes)
    {
        if (strokes.Count == 0) return true;
        var inputs = new NativeInput.Input[strokes.Count];
        for (var i = 0; i < strokes.Count; i++)
        {
            var s = strokes[i];
            inputs[i].type = NativeInput.InputKeyboard;
            inputs[i].U.ki = new NativeInput.KeybdInput { wVk = (ushort)s.VirtualKey, wScan = (ushort)s.ScanCode, dwFlags = (s.IsKeyUp ? NativeInput.KeyeventfKeyUp : 0) | (s.Extended ? NativeInput.KeyeventfExtended : 0), dwExtraInfo = NativeInput.Marker };
        }
        return NativeInput.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput.Input>()) == inputs.Length;
    }
}
internal sealed class WindowsModifierReader : IModifierReader { public bool IsHeld(uint key) => (NativeInput.GetAsyncKeyState((int)key) & unchecked((short)0x8000)) != 0; }
