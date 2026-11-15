using System;
using System.IO;
using JinlingKnot;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JinlingKnot.Editor
{
    // Editor-only preferences never change the entry point of a player build.
    [InitializeOnLoad]
    public static class PlayAndBuild
    {
        const string ScenePath = "Assets/JinlingKnot/Scenes/JinlingKnot.unity";
        const string MenuRoot = "金陵绳结/Play 启动/";
        static string PreferenceKey => "JinlingKnot.PlayLevel." + Application.dataPath;
        static int StartupLevel => EditorPrefs.GetInt(PreferenceKey, 0);

        static PlayAndBuild()
        {
            EditorApplication.delayCall += ConfigureDefaultScene;
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += _ => StopBeforeReload();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // Runtime routes, meshes and audio caches are not hot-reload serializable.
        static void StopBeforeReload()
        {
            if (!EditorApplication.isPlaying) return;
            Debug.Log("脚本重新加载：已退出 Play，请再次点击 Play 重新初始化关卡。");
            EditorApplication.isPlaying = false;
        }

        static void ConfigureDefaultScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene) EditorSceneManager.playModeStartScene = scene;
        }

        [MenuItem("金陵绳结/打开游戏场景")]
        public static void OpenGameScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem(MenuRoot + "主菜单（默认）")]
        static void StartAtMenu() { SetStartupLevel(0); }
        [MenuItem(MenuRoot + "第一关")]
        static void StartAtFirst() { SetStartupLevel(1); }
        [MenuItem(MenuRoot + "第二关")]
        static void StartAtSecond() { SetStartupLevel(2); }
        static void SetStartupLevel(int level)
        {
            EditorPrefs.SetInt(PreferenceKey, level);
            ConfigureDefaultScene();
            Debug.Log(level == 0 ? "下次 Play 进入主菜单" : "下次 Play 进入第 " + level + " 关");
        }
        [MenuItem(MenuRoot + "主菜单（默认）", true)]
        static bool ValidateMenu() { return CheckMenu("主菜单（默认）", 0); }
        [MenuItem(MenuRoot + "第一关", true)]
        static bool ValidateFirst() { return CheckMenu("第一关", 1); }
        [MenuItem(MenuRoot + "第二关", true)]
        static bool ValidateSecond() { return CheckMenu("第二关", 2); }
        static bool CheckMenu(string label, int level)
        {
            Menu.SetChecked(MenuRoot + label, StartupLevel == level);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || StartupLevel == 0) return;
            // Defer until scene Awake/OnEnable have created gameplay and UI objects.
            EditorApplication.delayCall += StartSelectedLevel;
        }
        static void StartSelectedLevel()
        {
            if (!EditorApplication.isPlaying) return;
            var game = UnityEngine.Object.FindObjectOfType<KnotGame>();
            if (!game) { Debug.LogError("游戏场景缺少 KnotGame，无法启动指定关卡。"); return; }
            game.SelectLevel(StartupLevel);
            var ui = game.GetComponent<KnotUI>();
            ui.Transition(() => game.StartGame(false));
        }

        [MenuItem("金陵绳结/构建 Windows 64 位预览")]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请退出 Play 后再构建。");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("请在 Unity Hub 安装 Windows Build Support。");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string output = "Builds/Windows/JinlingKnot.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows 构建失败：" + report.summary.result);
            Debug.Log("Windows 构建完成：" + Path.GetFullPath(output));
        }
    }
}