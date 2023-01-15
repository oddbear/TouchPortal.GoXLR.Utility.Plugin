using System.Diagnostics;
using System.Reflection;

namespace TouchPortal.GoXLR.Utility.Plugin.Plugin;

public static class PluginInfo
{
    public static string AppDataRoaming { get; }
    public static string TpPluginsDirectory { get; }
    public static string AssemblyName { get; }
    public static string PluginDirectory { get; }
    public static string EntryTpPath { get; }

    static PluginInfo()
    {
        AppDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        TpPluginsDirectory = Path.Combine(AppDataRoaming, "TouchPortal\\plugins");

        AssemblyName = Assembly.GetEntryAssembly()?.GetName().Name!;
        PluginDirectory = Path.Combine(TpPluginsDirectory, AssemblyName);
        EntryTpPath = Path.Combine(PluginDirectory, "entry.tp");
    }
}

public static class EntryCopy
{
    [Conditional("DEBUG")]
    public static void RefreshEntryFile()
    {
        if (!EntryFileChanged())
            return;

        KillTouchPortal();

        if (!Directory.Exists(PluginInfo.PluginDirectory))
            Directory.CreateDirectory(PluginInfo.PluginDirectory);

        if (File.Exists(PluginInfo.EntryTpPath))
            File.Delete(PluginInfo.EntryTpPath);

        File.Copy("entry.tp", PluginInfo.EntryTpPath);

        StartTouchPortal();
    }

    private static bool EntryFileChanged()
    {
        if (!File.Exists(PluginInfo.EntryTpPath))
            return true;

        return !File.ReadAllBytes("entry.tp")
            .SequenceEqual(File.ReadAllBytes(PluginInfo.EntryTpPath));
    }

    private static void KillTouchPortal()
    {
        foreach (var process in Process.GetProcessesByName("TouchPortal"))
        {
            process.Kill();
        }
    }

    private static void StartTouchPortal()
    {
        Process.Start(@"C:\Program Files (x86)\Touch Portal\TouchPortal.exe");
        Thread.Sleep(3000);
    }
}