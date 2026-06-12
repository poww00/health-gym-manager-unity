#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RelocationPopupPlaytestBatch
{
    private const string TestSandboxScenePath = "Assets/_Project/Scenes/TestSandbox.unity";
    private const string RunningKey = "RelocationPopupPlaytestBatch.Running";
    private const string FailedKey = "RelocationPopupPlaytestBatch.Failed";

    private enum TestPhase
    {
        BootAndOpen,
        Interactions
    }

    private static GameRuntimeUIController controller;
    private static InGameMenuManager menuManager;
    private static bool failed;
    private static bool ticking;
    private static int waitFrames;
    private static string targetBeforeNext;
    private static string targetAfterNext;
    private static TestPhase phase;

    static RelocationPopupPlaytestBatch()
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            RegisterCallbacks();
        }
    }

    [MenuItem("_Project/UI/Verify Relocation Popup Playtest")]
    public static void VerifyTestSandboxRelocationPopup()
    {
        failed = false;
        ticking = false;
        waitFrames = 0;
        controller = null;
        menuManager = null;
        targetBeforeNext = string.Empty;
        targetAfterNext = string.Empty;
        phase = TestPhase.BootAndOpen;

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(FailedKey, false);

        Debug.Log("[RelocationPopupPlaytestBatch] Opening TestSandbox for relocation popup verification.");
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
        Application.logMessageReceived -= HandleLogMessage;
        Application.logMessageReceived += HandleLogMessage;
    }

    private static void BootstrapTick()
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            EditorApplication.update -= BootstrapTick;
            return;
        }

        if (EditorApplication.isPlaying)
        {
            StartTickingInPlayMode();
        }
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
            Debug.Log("[RelocationPopupPlaytestBatch] Exited Play Mode.");
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.update -= BootstrapTick;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= HandleLogMessage;
            failed = SessionState.GetBool(FailedKey, failed);
            SessionState.EraseBool(RunningKey);
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
        waitFrames = 0;
        phase = TestPhase.BootAndOpen;
        Debug.Log("[RelocationPopupPlaytestBatch] Entered Play Mode.");
        EditorApplication.update -= BootstrapTick;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        waitFrames++;
        if (waitFrames < 60)
        {
            return;
        }

        if (phase == TestPhase.BootAndOpen)
        {
            OpenPopupForVerification();
            return;
        }

        VerifyInteractions();
    }

    private static void OpenPopupForVerification()
    {
        controller = Object.FindFirstObjectByType<GameRuntimeUIController>();
        menuManager = Object.FindFirstObjectByType<InGameMenuManager>();
        if (controller == null || menuManager == null)
        {
            Fail("GameRuntimeUIController or InGameMenuManager not found.");
            StopPlayMode();
            return;
        }

        DisableQuitSavesForPlaytest();

        if (!InvokePrivate(controller, "OpenRelocationPopup"))
        {
            Fail("OpenRelocationPopup invocation failed.");
            StopPlayMode();
            return;
        }

        Transform root = FindByName("RuntimeRelocationPopupRoot");
        if (!VerifyOpenedPopup(root))
        {
            StopPlayMode();
            return;
        }

        phase = TestPhase.Interactions;
        waitFrames = 0;
    }

    private static void VerifyInteractions()
    {
        Transform root = FindByName("RuntimeRelocationPopupRoot");
        if (!VerifyOpenedPopup(root))
        {
            StopPlayMode();
            return;
        }

        targetBeforeNext = GetText(root, "TargetTitleRow");
        if (!InvokeButton(root, "RelocationNext"))
        {
            StopPlayMode();
            return;
        }

        targetAfterNext = GetText(root, "TargetTitleRow");
        if (string.IsNullOrWhiteSpace(targetAfterNext) || targetAfterNext == targetBeforeNext)
        {
            Fail($"Next arrow did not change target. before='{targetBeforeNext}', after='{targetAfterNext}'.");
            StopPlayMode();
            return;
        }

        if (!VerifyOpenedPopup(root))
        {
            StopPlayMode();
            return;
        }

        if (!InvokeButton(root, "RelocationPrev"))
        {
            StopPlayMode();
            return;
        }

        string targetAfterPrev = GetText(root, "TargetTitleRow");
        if (string.IsNullOrWhiteSpace(targetAfterPrev) || targetAfterPrev != targetBeforeNext)
        {
            Fail($"Prev arrow did not return target. before='{targetBeforeNext}', afterPrev='{targetAfterPrev}'.");
            StopPlayMode();
            return;
        }

        if (!VerifyExecuteButton(root))
        {
            StopPlayMode();
            return;
        }

        if (!InvokeButton(root, "RelocationCancelButton") || root.gameObject.activeSelf)
        {
            Fail("Cancel button did not close relocation popup.");
            StopPlayMode();
            return;
        }

        Debug.Log("[RelocationPopupPlaytestBatch] Verified open/cancel close, arrows, no X close, icons, size badges, and execute button state.");
        StopPlayMode();
    }

    private static bool VerifyOpenedPopup(Transform root)
    {
        if (root == null || !root.gameObject.activeSelf)
        {
            Fail("Relocation popup root is missing or inactive.");
            return false;
        }

        if (!HasSprite(root, "CurrentLocationIcon") || !HasSprite(root, "TargetLocationIcon"))
        {
            Fail("Location icon sprite missing.");
            return false;
        }

        if (!VerifyIconSize(root, "CurrentLocationIcon") ||
            !VerifyIconSize(root, "TargetLocationIcon"))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(GetText(root, "CurrentInfo")) ||
            string.IsNullOrWhiteSpace(GetText(root, "TargetTitleRow")) ||
            string.IsNullOrWhiteSpace(GetText(root, "RiskText")) ||
            string.IsNullOrWhiteSpace(GetText(root, "FlowText")) ||
            string.IsNullOrWhiteSpace(GetText(root, "RentText")) ||
            string.IsNullOrWhiteSpace(GetText(root, "QuoteText")))
        {
            Fail("Required relocation text is empty.");
            return false;
        }

        if (FindDeepChild(root, "RelocationClose") != null)
        {
            Fail("Relocation close X button should be removed.");
            return false;
        }

        Transform targetAccent = FindDeepChild(root, "TargetAccent");
        if (targetAccent != null && targetAccent.gameObject.activeSelf)
        {
            Fail("TargetAccent should be removed or inactive for the measured relocation layout.");
            return false;
        }

        if (FindDeepChild(root, "RelocationTargetCard") != null ||
            FindDeepChild(root, "RelocationCompareBox") != null ||
            FindDeepChild(root, "CompareCurrentSizeVisual") != null ||
            FindDeepChild(root, "CompareTargetSizeVisual") != null ||
            FindDeepChild(root, "VerticalLines") != null ||
            FindDeepChild(root, "HorizontalLines") != null)
        {
            Fail("Dense second-pass relocation UI elements are still present.");
            return false;
        }

        RelocationManager.RelocationQuote quote;
        if (!menuManager.TryGetCurrentRelocationQuote(out quote))
        {
            Fail("Relocation quote unavailable during popup verification.");
            return false;
        }

        if (!VerifySizeBadge(root, "CurrentSizeBadge", quote.currentGridWidth, quote.currentGridHeight) ||
            !VerifySizeBadge(root, "TargetSizeBadge", quote.targetGridWidth, quote.targetGridHeight))
        {
            return false;
        }

        if (!VerifySizeBadgeVariants(root))
        {
            return false;
        }

        if (!VerifyRelocationLayoutDensity(root))
        {
            return false;
        }

        return true;
    }

    private static bool VerifyIconSize(Transform root, string iconName)
    {
        RectTransform rect = FindComponent<RectTransform>(root, iconName);
        if (rect == null)
        {
            Fail($"Location icon rect missing: {iconName}.");
            return false;
        }

        if (Mathf.Abs(rect.sizeDelta.x - 120f) > 1f ||
            Mathf.Abs(rect.sizeDelta.y - 120f) > 1f)
        {
            Fail($"{iconName} size should be 120x120. actual={rect.sizeDelta}.");
            return false;
        }

        return true;
    }

    private static bool VerifySizeBadge(Transform root, string badgeName, int width, int height)
    {
        Transform badge = FindDeepChild(root, badgeName);
        Transform shadow = FindDeepChild(badge, "SizeShadow");
        Transform square = FindDeepChild(badge, "SizeSquare");
        Text topLabel = FindComponent<Text>(badge, "DimensionTopLabel");
        Text leftLabel = FindComponent<Text>(badge, "DimensionLeftLabel");
        RectTransform badgeRect = badge != null ? badge.GetComponent<RectTransform>() : null;
        RectTransform shadowRect = shadow != null ? shadow.GetComponent<RectTransform>() : null;
        RectTransform rect = square != null ? square.GetComponent<RectTransform>() : null;
        if (badgeRect == null || shadowRect == null || rect == null || topLabel == null || leftLabel == null)
        {
            Fail($"Size badge missing: {badgeName}.");
            return false;
        }

        const float expectedContainerSize = 150f;
        float expectedBoardSize = GetExpectedBadgeBoardSize(width, height);
        if (Mathf.Abs(badgeRect.sizeDelta.x - expectedContainerSize) > 1f ||
            Mathf.Abs(badgeRect.sizeDelta.y - expectedContainerSize) > 1f ||
            Mathf.Abs(shadowRect.sizeDelta.x - expectedBoardSize) > 1f ||
            Mathf.Abs(shadowRect.sizeDelta.y - expectedBoardSize) > 1f ||
            Mathf.Abs(rect.sizeDelta.x - expectedBoardSize) > 1f ||
            Mathf.Abs(rect.sizeDelta.y - expectedBoardSize) > 1f)
        {
            Fail($"{badgeName} size mismatch. expectedContainer={expectedContainerSize}, expectedBoard={expectedBoardSize}, badge={badgeRect.sizeDelta}, shadow={shadowRect.sizeDelta}, square={rect.sizeDelta}.");
            return false;
        }

        if (FindDeepChild(square, "BadgeGridV_01") != null ||
            FindDeepChild(square, "BadgeGridH_01") != null)
        {
            Fail($"{badgeName} should not contain internal grid lines.");
            return false;
        }

        string expectedDimension = GetExpectedBadgeDimension(width, height).ToString();
        if (topLabel.text != expectedDimension || leftLabel.text != expectedDimension)
        {
            Fail($"{badgeName} dimension labels mismatch. expected={expectedDimension}, top='{topLabel.text}', left='{leftLabel.text}'.");
            return false;
        }

        if (!HasChild(badge, "DimensionTopLineLeft") ||
            !HasChild(badge, "DimensionTopLineRight") ||
            !HasChild(badge, "DimensionLeftLineTop") ||
            !HasChild(badge, "DimensionLeftLineBottom"))
        {
            Fail($"{badgeName} dimension guide lines are missing.");
            return false;
        }

        Image squareImage = square.GetComponent<Image>();
        if (squareImage == null || !IsNeutralSteelColor(squareImage.color))
        {
            Fail($"{badgeName} square color should be neutral steel. actual={(squareImage != null ? squareImage.color.ToString() : "missing")}.");
            return false;
        }

        return true;
    }

    private static bool VerifySizeBadgeVariants(Transform root)
    {
        RectTransform badge = FindComponent<RectTransform>(root, "CurrentSizeBadge");
        if (badge == null)
        {
            Fail("CurrentSizeBadge missing for variant verification.");
            return false;
        }

        RelocationManager.RelocationQuote quote;
        if (!menuManager.TryGetCurrentRelocationQuote(out quote))
        {
            Fail("Relocation quote unavailable for size badge variant verification.");
            return false;
        }

        if (!ApplySizeBadgeForVerification(badge, 8, 8) ||
            !VerifySizeBadge(root, "CurrentSizeBadge", 8, 8) ||
            !ApplySizeBadgeForVerification(badge, 16, 16) ||
            !VerifySizeBadge(root, "CurrentSizeBadge", 16, 16) ||
            !ApplySizeBadgeForVerification(badge, 32, 32) ||
            !VerifySizeBadge(root, "CurrentSizeBadge", 32, 32))
        {
            return false;
        }

        return ApplySizeBadgeForVerification(badge, quote.currentGridWidth, quote.currentGridHeight) &&
               VerifySizeBadge(root, "CurrentSizeBadge", quote.currentGridWidth, quote.currentGridHeight);
    }

    private static bool VerifyRelocationLayoutDensity(Transform root)
    {
        if (!VerifyRectSize(root, "RelocationPopupFrame", 780f, 1040f, 820f, 1100f) ||
            !VerifyRect(root, "RelocationCurrentBox", 0f, 240f, 700f, 220f, 1f) ||
            !VerifyRect(root, "CurrentLocationIcon", -278f, 3f, 120f, 120f, 1f) ||
            !VerifyRect(root, "CurrentInfo", 0f, 3f, 430f, 150f, 1f) ||
            !VerifyRect(root, "CurrentSizeBadge", 238f, 3f, 150f, 150f, 1f) ||
            !VerifyRect(root, "RelocationTargetBox", 0f, 0f, 700f, 220f, 1f) ||
            !VerifyRect(root, "TargetLocationIcon", -278f, -19f, 120f, 120f, 1f) ||
            !VerifyRect(root, "TargetTitleRow", -18f, 62f, 390f, 48f, 1f) ||
            !VerifyRect(root, "RiskText", -18f, 26f, 350f, 36f, 1f) ||
            !VerifyRect(root, "FlowText", -18f, -14f, 350f, 36f, 1f) ||
            !VerifyRect(root, "RentText", -18f, -54f, 350f, 36f, 1f) ||
            !VerifyRect(root, "TargetSizeBadge", 240f, -19f, 150f, 150f, 1f) ||
            !VerifyRect(root, "RelocationQuoteBox", 0f, -240f, 700f, 220f, 1f) ||
            !VerifyRect(root, "QuoteText", -18f, 8f, 620f, 146f, 1f) ||
            !VerifyRect(root, "RelocationPrev", -178f, 62f, 90f, 66f, 1f) ||
            !VerifyRect(root, "RelocationNext", 142f, 62f, 90f, 66f, 1f) ||
            !VerifyRectSize(root, "RelocationExecuteButton", 290f, 88f, 320f, 104f) ||
            !VerifyRectSize(root, "RelocationCancelButton", 290f, 88f, 320f, 104f))
        {
            return false;
        }

        if (!VerifyParent(root, "RelocationPrev", "RelocationTargetBox") ||
            !VerifyParent(root, "RelocationNext", "RelocationTargetBox"))
        {
            return false;
        }

        if (!VerifyTransparentButton(root, "RelocationPrev") ||
            !VerifyTransparentButton(root, "RelocationNext"))
        {
            return false;
        }

        if (!VerifyVerticalGap(root, "RelocationCurrentBox", "RelocationTargetBox", 18f) ||
            !VerifyVerticalGap(root, "RelocationTargetBox", "RelocationQuoteBox", 18f) ||
            !VerifyVerticalGap(root, "RelocationQuoteBox", "RelocationExecuteButton", 24f))
        {
            return false;
        }

        return true;
    }

    private static bool VerifyRect(Transform root, string objectName, float x, float y, float width, float height, float tolerance)
    {
        RectTransform rect = FindComponent<RectTransform>(root, objectName);
        if (rect == null)
        {
            Fail($"Rect missing: {objectName}.");
            return false;
        }

        if (Mathf.Abs(rect.anchoredPosition.x - x) > tolerance ||
            Mathf.Abs(rect.anchoredPosition.y - y) > tolerance ||
            Mathf.Abs(rect.sizeDelta.x - width) > tolerance ||
            Mathf.Abs(rect.sizeDelta.y - height) > tolerance)
        {
            Fail($"{objectName} rect mismatch. expected=({x},{y},{width},{height}), actual=({rect.anchoredPosition.x},{rect.anchoredPosition.y},{rect.sizeDelta.x},{rect.sizeDelta.y}).");
            return false;
        }

        return true;
    }

    private static bool VerifyParent(Transform root, string objectName, string parentName)
    {
        Transform target = FindDeepChild(root, objectName);
        if (target == null)
        {
            Fail($"Object missing: {objectName}.");
            return false;
        }

        if (target.parent == null || target.parent.name != parentName)
        {
            Fail($"{objectName} should be a child of {parentName}.");
            return false;
        }

        return true;
    }

    private static bool VerifyRectSize(Transform root, string objectName, float minWidth, float minHeight, float maxWidth, float maxHeight)
    {
        RectTransform rect = FindComponent<RectTransform>(root, objectName);
        if (rect == null)
        {
            Fail($"Rect missing: {objectName}.");
            return false;
        }

        Vector2 size = rect.sizeDelta;
        if (size.x < minWidth || size.x > maxWidth || size.y < minHeight || size.y > maxHeight)
        {
            Fail($"{objectName} size is outside expected range. actual={size}, min=({minWidth},{minHeight}), max=({maxWidth},{maxHeight}).");
            return false;
        }

        return true;
    }

    private static bool ApplySizeBadgeForVerification(RectTransform badge, int width, int height)
    {
        MethodInfo method = typeof(RuntimeGameUIController).GetMethod("ApplyRelocationSizeBadge", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null || controller == null)
        {
            Fail("ApplyRelocationSizeBadge reflection lookup failed.");
            return false;
        }

        method.Invoke(controller, new object[] { badge, width, height, false });
        return true;
    }

    private static bool HasChild(Transform root, string childName)
    {
        return FindDeepChild(root, childName) != null;
    }

    private static bool IsNeutralSteelColor(Color color)
    {
        float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        return max - min <= 0.08f && color.a >= 0.85f;
    }

    private static bool VerifyTransparentButton(Transform root, string buttonName)
    {
        Button button = FindComponent<Button>(root, buttonName);
        Image image = FindComponent<Image>(root, buttonName);
        if (button == null || image == null)
        {
            Fail($"Transparent hit button missing: {buttonName}.");
            return false;
        }

        if (!image.raycastTarget)
        {
            Fail($"{buttonName} must keep a clickable raycast target.");
            return false;
        }

        if (image.color.a > 0.02f)
        {
            Fail($"{buttonName} should be visually transparent. alpha={image.color.a}.");
            return false;
        }

        if (FindDeepChild(button.transform, "Label") != null)
        {
            Fail($"{buttonName} should not have a visible button label child.");
            return false;
        }

        return true;
    }

    private static bool VerifyVerticalGap(Transform root, string upperName, string lowerName, float minGap)
    {
        RectTransform upper = FindComponent<RectTransform>(root, upperName);
        RectTransform lower = FindComponent<RectTransform>(root, lowerName);
        if (upper == null || lower == null)
        {
            Fail($"Cannot verify vertical gap between {upperName} and {lowerName}.");
            return false;
        }

        float upperBottom = upper.anchoredPosition.y - (upper.sizeDelta.y * 0.5f);
        float lowerTop = lower.anchoredPosition.y + (lower.sizeDelta.y * 0.5f);
        float gap = upperBottom - lowerTop;
        if (gap < minGap)
        {
            Fail($"Vertical gap between {upperName} and {lowerName} is too small. expected>={minGap}, actual={gap}.");
            return false;
        }

        return true;
    }

    private static bool VerifyExecuteButton(Transform root)
    {
        Button button = FindComponent<Button>(root, "RelocationExecuteButton");
        if (button == null)
        {
            Fail("Execute button missing.");
            return false;
        }

        RelocationManager.RelocationQuote quote;
        if (!menuManager.TryGetCurrentRelocationQuote(out quote))
        {
            Fail("Relocation quote unavailable for execute button state.");
            return false;
        }

        bool expectedInteractable = quote.isValid && quote.shortageAmount <= 0;
        if (button.interactable != expectedInteractable)
        {
            Fail($"Execute button interactable mismatch. expected={expectedInteractable}, actual={button.interactable}.");
            return false;
        }

        return true;
    }

    private static bool InvokePrivate(Object target, string methodName)
    {
        MethodInfo method = typeof(RuntimeGameUIController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            return false;
        }

        method.Invoke(target, null);
        return true;
    }

    private static bool InvokeButton(Transform root, string buttonName)
    {
        Button button = FindComponent<Button>(root, buttonName);
        if (button == null)
        {
            Fail($"Button missing: {buttonName}.");
            return false;
        }

        button.onClick.Invoke();
        return true;
    }

    private static bool HasSprite(Transform root, string imageName)
    {
        Image image = FindComponent<Image>(root, imageName);
        return image != null && image.sprite != null;
    }

    private static string GetText(Transform root, string textName)
    {
        Text text = FindComponent<Text>(root, textName);
        return text != null ? text.text : string.Empty;
    }

    private static T FindComponent<T>(Transform root, string objectName) where T : Component
    {
        Transform target = FindDeepChild(root, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindByName(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static float GetExpectedBadgeBoardSize(int width, int height)
    {
        int dimension = GetExpectedBadgeDimension(width, height);
        if (dimension <= 8)
        {
            return 64f;
        }

        if (dimension <= 16)
        {
            return 92f;
        }

        return 112f;
    }

    private static int GetExpectedBadgeDimension(int width, int height)
    {
        int max = Mathf.Max(width, height);
        if (max <= 8)
        {
            return 8;
        }

        if (max <= 16)
        {
            return 16;
        }

        return 32;
    }

    private static void DisableQuitSavesForPlaytest()
    {
        SetPrivateBool(Object.FindFirstObjectByType<SaveManager>(), "autoSaveOnPauseOrFocusLost", false);
        SetPrivateBool(Object.FindFirstObjectByType<AutoSaveHeartbeatManager>(), "saveOnApplicationQuit", false);
        SetPrivateBool(Object.FindFirstObjectByType<GymEconomyManager>(), "saveOnApplicationQuit", false);
    }

    private static void SetPrivateBool(Object target, string fieldName, bool value)
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(target, value);
        }
    }

    private static void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        if (condition.Contains("NullReferenceException") ||
            condition.Contains("Missing Sprite") ||
            condition.Contains("Resources.Load"))
        {
            Fail($"Console issue detected: {condition}");
        }
    }

    private static void Fail(string message)
    {
        failed = true;
        SessionState.SetBool(FailedKey, true);
        Debug.LogError("[RelocationPopupPlaytestBatch] " + message);
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
