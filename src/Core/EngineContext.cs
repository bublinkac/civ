using System;
using System.IO;

namespace CivGame.Core;

public static class EngineContext
{
    public static bool IsGodotRuntime { get; } = CheckGodotRuntime();

    private static bool CheckGodotRuntime()
    {
        try
        {
            string? proc = Environment.ProcessPath;
            if (string.IsNullOrEmpty(proc)) return false;
            string fileName = Path.GetFileName(proc).ToLowerInvariant();
            return fileName.Contains("godot");
        }
        catch
        {
            return false;
        }
    }
}
