using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CI
{
    public static class CaptureCurrentUiScreenshot
    {
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[CaptureCurrentUiScreenshot] failed to open Main scene");
                EditorApplication.Exit(2);
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[CaptureCurrentUiScreenshot] Main Camera not found");
                EditorApplication.Exit(3);
                return;
            }

            var controller = Object.FindFirstObjectByType<PetController>();
            if (controller == null)
            {
                var go = new GameObject("PetController_Capture");
                controller = go.AddComponent<PetController>();
                var awake = typeof(PetController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                awake?.Invoke(controller, null);
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }

            Canvas.ForceUpdateCanvases();

            var width = 1080;
            var height = 1920;
            var rt = new RenderTexture(width, height, 24);
            var prev = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            RenderTexture.active = rt;
            cam.Render();

            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../reports/unity_main_real.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllBytes(outPath, tex.EncodeToPNG());

            cam.targetTexture = prev;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log($"[CaptureCurrentUiScreenshot] wrote: {outPath}");
            EditorApplication.Exit(0);
        }
    }
}
