using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
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

                int errors = 0;
                var assemblies = CompilationPipeline.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var msgs = assembly.compilerMessages;
                    if (msgs == null) continue;

                    foreach (var msg in msgs)
                    {
                        if (msg.type == CompilerMessageType.Error)
                        {
                            errors++;
                            Debug.LogError($"[CI.CompileGate] {assembly.name}: {msg.message} ({msg.file}:{msg.line})");
                        }
                    }
                }

                if (errors > 0)
                {
                    Debug.LogError($"[CI.CompileGate] FAIL: {errors} compile error(s)");
                    EditorApplication.Exit(20);
                    return;
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
