#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InstallTutorialPlaytestBatch
{
    private const string TestSandboxScenePath = "Assets/_Project/Scenes/TestSandbox.unity";
    private const string RunningKey = "InstallTutorialPlaytestBatch.Running";
    private const string PageIndexKey = "InstallTutorialPlaytestBatch.PageIndex";
    private const string FailedKey = "InstallTutorialPlaytestBatch.Failed";
    private static readonly int[] ProblemPages = { 2, 4, 6, 7, 8, 10, 12, 14 };

    private static GameRuntimeUIController controller;
    private static int pageIndex;
    private static int waitFrames;
    private static bool failed;
    private static bool ticking;

    static InstallTutorialPlaytestBatch()
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            RegisterCallbacks();
        }
    }

    public static void VerifyProblemTutorialPages()
    {
        failed = false;
        pageIndex = 0;
        waitFrames = 0;
        controller = null;
        ticking = false;

        SessionState.SetBool(RunningKey, true);
        SessionState.SetInt(PageIndexKey, 0);
        SessionState.SetBool(FailedKey, false);

        Debug.Log("[InstallTutorialPlaytestBatch] Opening TestSandbox for tutorial rect verification.");
        EditorSceneManager.OpenScene(TestSandboxScenePath);
        RegisterCallbacks();
        EditorApplication.isPlaying = true;
    }

    private static void RegisterCallbacks()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.update -= BootstrapTick;
        EditorApplication.update += BootstrapTick;
    }

    private static void BootstrapTick()
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            EditorApplication.update -= BootstrapTick;
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            return;
        }

        StartTickingInPlayMode();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            StartTickingInPlayMode();
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            Debug.Log("[InstallTutorialPlaytestBatch] Exited Play Mode.");
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.update -= BootstrapTick;
            EditorApplication.update -= Tick;
            failed = SessionState.GetBool(FailedKey, failed);
            SessionState.EraseBool(RunningKey);
            SessionState.EraseInt(PageIndexKey);
            SessionState.EraseBool(FailedKey);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(failed ? 1 : 0);
            }
        }
    }

    private static void StartTickingInPlayMode()
    {
        if (ticking)
        {
            return;
        }

        ticking = true;
        failed = SessionState.GetBool(FailedKey, false);
        pageIndex = SessionState.GetInt(PageIndexKey, 0);
        waitFrames = 0;
        controller = null;
        Debug.Log("[InstallTutorialPlaytestBatch] Entered Play Mode.");
        EditorApplication.update -= BootstrapTick;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        waitFrames++;
        if (waitFrames < 30)
        {
            return;
        }

        if (controller == null)
        {
            controller = Object.FindFirstObjectByType<GameRuntimeUIController>();
            if (controller == null)
            {
                if (waitFrames > 240)
                {
                    failed = true;
                    SessionState.SetBool(FailedKey, true);
                    Debug.LogError("[InstallTutorialPlaytestBatch] GameRuntimeUIController not found.");
                    StopPlayMode();
                }

                return;
            }
        }

        if (pageIndex >= ProblemPages.Length)
        {
            Debug.Log("[InstallTutorialPlaytestBatch] Completed problem tutorial page verification jumps.");
            StopPlayMode();
            return;
        }

        int page = ProblemPages[pageIndex];
        Debug.Log($"[InstallTutorialPlaytestBatch] Jumping to tutorial page {page}.");
        controller.JumpToTutorialStep(page);
        controller.LogCurrentFocusRect();
        pageIndex++;
        SessionState.SetInt(PageIndexKey, pageIndex);
        waitFrames = 20;
    }

    private static void StopPlayMode()
    {
        ticking = false;
        EditorApplication.update -= Tick;
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
        else if (Application.isBatchMode)
        {
            EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
#endif
