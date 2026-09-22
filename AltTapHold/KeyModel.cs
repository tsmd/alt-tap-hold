namespace AltTapHold;

public static class Keys
{
    public const uint LAlt = 0xA4, RAlt = 0xA5, NonConvert = 0x1D, Convert = 0x1C;
    public const uint Shift = 0x10, Control = 0x11, LWin = 0x5B, RWin = 0x5C;
}

public enum AltSide { Left, Right }
public enum AltPhase { Idle, Pending, Active }

public readonly record struct KeyStroke(uint VirtualKey, uint ScanCode, bool IsKeyUp, bool Extended = false)
{
    public static KeyStroke Alt(AltSide side, bool up) => new(side == AltSide.Left ? Keys.LAlt : Keys.RAlt, 0, up, side == AltSide.Right);
    public static KeyStroke Ime(AltSide side, bool up) => new(side == AltSide.Left ? Keys.NonConvert : Keys.Convert, 0, up);
}

public interface IKeySender
{
    // True only if every supplied input was accepted by the operating system.
    bool Send(IReadOnlyList<KeyStroke> strokes);
}

public interface IModifierReader { bool IsHeld(uint virtualKey); }

