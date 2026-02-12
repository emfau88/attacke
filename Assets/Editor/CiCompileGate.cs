using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CI
{
    public static class CompileGate
    {
        public static void Run()
        {
            Debug.Log("[CI.CompileGate] START");

            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var logPath = Application.consoleLogPath;
                if (!string.IsNullOrWhiteSpace(logPath) && File.Exists(logPath))
                {
                    var log = File.ReadAllText(logPath);
                    if (log.IndexOf("error CS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        log.IndexOf("Compilation failed", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.LogError("[CI.CompileGate] FAIL: compile markers found in log");
                        EditorApplication.Exit(20);
                        return;
                    }
                }

                Debug.Log("[CI.CompileGate] END OK");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CI.CompileGate] CRASH: {ex}");
                EditorApplication.Exit(21);
            }
        }
    }
}
