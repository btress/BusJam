using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BusJam.EditorTools
{
    public static class BusJamSetup
    {
        [MenuItem("BusJam/1. Setup Everything (Portrait + Levels + Add To Current Scene)")]
        public static void SetupEverything()
        {
            ApplyPortraitSettings();
            LevelGeneratorTool.GenerateAll();
            FixAnimatorController();
            AddToCurrentScene();
        }

        [MenuItem("BusJam/Apply Portrait 1080x1920 Settings")]
        public static void ApplyPortraitSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;   // khoa cung Portrait
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultWebScreenWidth = 1080;
            PlayerSettings.defaultWebScreenHeight = 1920;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.studio.busjam");
            PlayerSettings.productName = "Bus Jam Prototype";
        }

        /// <summary>Xoa cac transition tu dong (Has Exit Time) trong Ch_Anim de code toan quyen chon Idle/Running/Sitting bang CrossFade.</summary>
        [MenuItem("BusJam/Fix Ch_Anim Controller (remove auto transitions)")]
        public static void FixAnimatorController()
        {
            var guids = AssetDatabase.FindAssets("Ch_Anim t:AnimatorController");
            if (guids.Length == 0) { Debug.LogWarning("BusJam: khong tim thay Ch_Anim.controller"); return; }
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
            int removed = 0;
            foreach (var layer in ctrl.layers)
            {
                foreach (var cs in layer.stateMachine.states)
                    foreach (var t in cs.state.transitions.ToArray()) { cs.state.RemoveTransition(t); removed++; }
                foreach (var t in layer.stateMachine.anyStateTransitions.ToArray()) { layer.stateMachine.RemoveAnyStateTransition(t); removed++; }
            }
            EditorUtility.SetDirty(ctrl); AssetDatabase.SaveAssets();
            Debug.Log($"BusJam: Ch_Anim - da xoa {removed} transition. Nho bat Loop Time cho clip Idle va Running.");
        }

        [MenuItem("BusJam/Add GameManager To Current Scene")]
        public static void AddToCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            var gm = Object.FindObjectOfType<GameManager>();
            if (gm == null) gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            gm.levels = LoadLevels();
            if (gm.busModel == null) gm.busModel = FindModel("BusPrefab");
            if (gm.backgroundTexture == null) gm.backgroundTexture = ImportBackground();
            if (gm.passengerModel == null) gm.passengerModel = FindModel("Breathing Idle");

            foreach (var root in scene.GetRootGameObjects())
                if (root != gm.gameObject && (root.name.StartsWith("BusPrefab") || root.name.StartsWith("Breathing Idle")))
                { root.SetActive(false); Debug.Log("BusJam: da tat object demo '" + root.name + "' trong scene."); }

            if (gm.busModel == null) Debug.LogWarning("BusJam: khong tim thay BusPrefab. Hay keo BusPrefab vao truong Bus Model cua GameManager.");
            if (gm.passengerModel == null) Debug.LogWarning("BusJam: khong tim thay 'Breathing Idle'. Hay keo prefab nhan vat vao truong Passenger Model cua GameManager.");
            EditorUtility.SetDirty(gm);
            EditorSceneManager.MarkSceneDirty(scene);
            string path = scene.path;
            if (string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory("Assets/BusJam/Scenes");
                path = "Assets/BusJam/Scenes/Game.unity";
            }
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };

            Debug.Log($"BusJam: GameManager san sang. Bus model = {(gm.busModel ? gm.busModel.name : "CHUA GAN")}, " +
                      $"Passenger model = {(gm.passengerModel ? gm.passengerModel.name : "CHUA GAN")}, levels = {gm.levels.Count}");
        }

        static Texture2D ImportBackground()
        {
            const string path = "Assets/BusJam/Art/nen.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("BusJam: khong tim thay " + path + ". Hay keo nen.png vao truong Background Texture cua GameManager."); return null; }
            imp.textureType = TextureImporterType.Default;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static List<LevelDataSO> LoadLevels()
        {
            return AssetDatabase.FindAssets("t:LevelDataSO", new[] { LevelGeneratorTool.LevelDir })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<LevelDataSO>).ToList();
        }

        static GameObject FindModel(string name)
        {
            var paths = AssetDatabase.FindAssets(name + " t:GameObject").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith(name))
                .OrderBy(p => p.EndsWith(".prefab") ? 0 : 1).ToList();
            return paths.Count > 0 ? AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]) : null;
        }

        // ------------- Build -------------
        static string[] Scenes() => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem("BusJam/Build Android APK")]
        public static void BuildApk()
        {
            ApplyPortraitSettings();
            Directory.CreateDirectory("Builds");
            Report(BuildPipeline.BuildPlayer(new BuildPlayerOptions
            { scenes = Scenes(), locationPathName = "Builds/BusJam.apk", target = BuildTarget.Android, options = BuildOptions.None }));
        }

        [MenuItem("BusJam/Build WebGL")]
        public static void BuildWebGL()
        {
            ApplyPortraitSettings();
            Directory.CreateDirectory("Builds/WebGL");
            Report(BuildPipeline.BuildPlayer(new BuildPlayerOptions
            { scenes = Scenes(), locationPathName = "Builds/WebGL", target = BuildTarget.WebGL, options = BuildOptions.None }));
        }

        static void Report(UnityEditor.Build.Reporting.BuildReport r) =>
            Debug.Log("BusJam build: " + r.summary.result + " -> " + r.summary.outputPath);
    }
}
