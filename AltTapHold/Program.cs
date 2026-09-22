namespace AltTapHold;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, "Local\\AltTapHold.CSharp.Singleton", out var first);
        if (!first) return;
        ApplicationConfiguration.Initialize();
        using var hook = new KeyboardHook();
        hook.Start();
        using var icon = new NotifyIcon { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application, Text = "Alt Tap-Hold", Visible = true };
        var menu = new ContextMenuStrip(); menu.Items.Add("終了", null, (_, _) => Application.Exit()); icon.ContextMenuStrip = menu;
        Application.Run();
        icon.Visible = false;
    }
}
