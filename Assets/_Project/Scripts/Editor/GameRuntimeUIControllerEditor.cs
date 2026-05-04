#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(GameRuntimeUIController))]
public sealed class GameRuntimeUIControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool inspectorChanged = EditorGUI.EndChangeCheck();

        GameRuntimeUIController controller = (GameRuntimeUIController)target;

        if (inspectorChanged && !Application.isPlaying)
        {
            controller.RefreshMenuPopupPreviewForEditMode();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(controller.gameObject);
            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Panel Preview", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("운영 프리뷰"))
            {
                PreviewPanel(controller, "OperatePanelRoot", "운영 현황", "OperateTabButton", false);
            }

            if (GUILayout.Button("설치 프리뷰"))
            {
                PreviewPanel(controller, "InstallPanelRoot", "설치", "InstallTabButton", false);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("경제 프리뷰"))
            {
                EconomyPanelEditModeBuilder.PreviewExistingEconomyPanel(controller, bindData: true);
            }

            if (GUILayout.Button("리뷰 프리뷰"))
            {
                ReviewPanelEditModeBuilder.PreviewExistingReviewPanel(controller);
            }
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Menu Popup Preview", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Menu Popup"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Preview Menu Popup");
                controller.PreviewMenuPopupForEditMode();
                EditorUtility.SetDirty(controller.gameObject);
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            if (GUILayout.Button("Refresh Menu Layout"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Refresh Menu Popup Layout");
                controller.RefreshMenuPopupPreviewForEditMode();
                EditorUtility.SetDirty(controller.gameObject);
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }
        }

        EditorGUILayout.HelpBox(
            "프리뷰 버튼 4개만 남겼습니다. 레이아웃을 덮어쓰는 Rebuild/Repair/Bind 버튼은 숨겼습니다.",
            MessageType.Info);
    }

    private static void PreviewPanel(GameRuntimeUIController controller, string targetPanelName, string title, string activeTabName, bool useComingSoonIfMissing)
    {
        if (controller == null)
        {
            return;
        }

        Transform root = controller.transform.Find("RuntimeGameUIRoot");
        if (root == null)
        {
            root = FindDeepChild(controller.transform, "RuntimeGameUIRoot");
        }

        if (root == null)
        {
            if (!InvokeInstanceMethod(controller, "ContextMaterializeGameUi"))
            {
                InvokeInstanceMethod(controller, "MaterializeForEditMode");
            }

            root = FindDeepChild(controller.transform, "RuntimeGameUIRoot");
        }

        if (root == null)
        {
            Debug.LogWarning("[GameRuntimeUIControllerEditor] RuntimeGameUIRoot를 찾지 못했습니다. Materialize Game UI를 먼저 실행하세요.", controller);
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Preview Game UI Panel");

        EconomyPanelVisibilityGuard guard = EnsureGuard(controller);
        guard.HideEconomy();

        SetPanelActive(root, "OperatePanelRoot", targetPanelName == "OperatePanelRoot");
        SetPanelActive(root, "InstallPanelRoot", targetPanelName == "InstallPanelRoot");
        SetPanelActive(root, "EconomyPanelRoot", targetPanelName == "EconomyPanelRoot");
        SetPanelActive(root, "ReviewPanelRoot", targetPanelName == "ReviewPanelRoot");

        Transform targetPanel = FindDeepChild(root, targetPanelName);
        bool targetExists = targetPanel != null;

        if (targetExists)
        {
            targetPanel.gameObject.SetActive(true);
            SetPanelActive(root, "ComingSoonPanelRoot", false);
        }
        else if (useComingSoonIfMissing)
        {
            SetPanelActive(root, "ComingSoonPanelRoot", true);
            SetComingSoonMessage(root, title + " 화면은 다음 구현 범위입니다.");
        }
        else
        {
            SetPanelActive(root, "ComingSoonPanelRoot", false);
        }

        SetSharedTitle(root, title);
        SetTabVisuals(root, activeTabName);

        if (targetPanelName == "InstallPanelRoot")
        {
            NormalizeInstallCardTexts(root);
        }

        EditorUtility.SetDirty(controller.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }

    private static EconomyPanelVisibilityGuard EnsureGuard(GameRuntimeUIController controller)
    {
        EconomyPanelVisibilityGuard guard = controller.GetComponent<EconomyPanelVisibilityGuard>();
        if (guard == null)
        {
            guard = controller.gameObject.AddComponent<EconomyPanelVisibilityGuard>();
        }

        return guard;
    }

    internal static bool InvokeInstanceMethod(object targetObject, string methodName)
    {
        if (targetObject == null || string.IsNullOrEmpty(methodName))
        {
            return false;
        }

        System.Type type = targetObject.GetType();
        while (type != null)
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if (method != null)
            {
                method.Invoke(targetObject, null);
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    internal static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetPanelActive(Transform root, string panelName, bool active)
    {
        Transform panel = FindDeepChild(root, panelName);
        if (panel != null && panel.gameObject.activeSelf != active)
        {
            panel.gameObject.SetActive(active);
        }
    }

    private static void SetSharedTitle(Transform root, string title)
    {
        Transform titleTransform = FindDeepChild(root, "SharedPanelTitle");
        if (titleTransform == null)
        {
            return;
        }

        Text titleText = titleTransform.GetComponent<Text>();
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = Color.white;
        }
    }

    private static void SetComingSoonMessage(Transform root, string message)
    {
        Transform messageTransform = FindDeepChild(root, "ComingSoonMessage");
        if (messageTransform == null)
        {
            return;
        }

        Text text = messageTransform.GetComponent<Text>();
        if (text != null)
        {
            text.text = message;
        }
    }

    private static void SetTabVisuals(Transform root, string activeTabName)
    {
        SetTabVisual(root, "OperateTabButton", activeTabName == "OperateTabButton");
        SetTabVisual(root, "InstallTabButton", activeTabName == "InstallTabButton");
        SetTabVisual(root, "EconomyTabButton", activeTabName == "EconomyTabButton");
        SetTabVisual(root, "ReviewTabButton", activeTabName == "ReviewTabButton");
    }

    private static void SetTabVisual(Transform root, string tabName, bool active)
    {
        Transform tab = FindDeepChild(root, tabName);
        if (tab == null)
        {
            return;
        }

        Image image = tab.GetComponent<Image>();
        if (image != null)
        {
            GeneratedRuntimeSprites.Assign(
                image,
                active ? "GeneratedRuntimeUI/ui_v2/tab_active_green_base" : "GeneratedRuntimeUI/ui_v2/tab_inactive_beige_base",
                false);
        }

        Transform labelTransform = FindDeepChild(tab, "Label");
        if (labelTransform == null)
        {
            return;
        }

        Text label = labelTransform.GetComponent<Text>();
        if (label != null)
        {
            label.color = active ? Color.white : new Color(0.12f, 0.08f, 0.03f, 1f);
        }
    }

    private static void NormalizeInstallCardTexts(Transform root)
    {
        Transform listRoot = FindDeepChild(root, "EquipmentCardList");
        if (listRoot == null)
        {
            return;
        }

        Text[] texts = listRoot.GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text == null || !IsInstallCardInnerText(text.transform))
            {
                continue;
            }

            Undo.RecordObject(text, "Normalize Install Card Text");
            text.fontStyle = FontStyle.Normal;

            Outline outline = text.GetComponent<Outline>();
            if (outline != null)
            {
                Undo.DestroyObjectImmediate(outline);
            }

            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow != null)
            {
                Undo.DestroyObjectImmediate(shadow);
            }

            EditorUtility.SetDirty(text);
        }
    }

    private static bool IsInstallCardInnerText(Transform textTransform)
    {
        if (textTransform == null)
        {
            return false;
        }

        string textName = textTransform.name;
        if (textName != "Name" && textName != "Price" && textName != "Owned" && textName != "Footprint")
        {
            return false;
        }

        Transform current = textTransform.parent;
        while (current != null)
        {
            if (current.name.StartsWith("InstallCard_", System.StringComparison.Ordinal))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}

internal static class EconomyPanelEditModeBuilder
{
    private const string EconomyRootName = "EconomyPanelRoot";
    private static readonly Color Ink = new Color(0.12f, 0.08f, 0.03f, 1f);
    private static readonly Color IncomeGreen = new Color(0.05f, 0.39f, 0.10f, 1f);
    private static readonly Color ExpenseRed = new Color(0.70f, 0.08f, 0.05f, 1f);

    public static void PreviewExistingEconomyPanel(GameRuntimeUIController controller, bool bindData)
    {
        if (controller == null)
        {
            return;
        }

        Transform root = ResolveRuntimeRoot(controller);
        if (root == null)
        {
            Debug.LogWarning("[EconomyPanelEditModeBuilder] RuntimeGameUIRoot를 찾지 못했습니다. 먼저 Materialize Game UI를 실행하세요.", controller);
            return;
        }

        Transform economyRoot = GameRuntimeUIControllerEditor.FindDeepChild(root, EconomyRootName);
        if (economyRoot == null)
        {
            Debug.LogWarning("[EconomyPanelEditModeBuilder] EconomyPanelRoot를 찾지 못했습니다. Rebuild Economy Panel을 한 번 실행해서 경제 패널을 만든 뒤 수동 조정하세요.", controller);
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Preview Economy Panel Without Rebuild");

        if (bindData)
        {
            BindExistingEconomyPanelDataOnly(controller, previewAfterBind: false);
        }

        SetPanelActive(root, "OperatePanelRoot", false);
        SetPanelActive(root, "InstallPanelRoot", false);
        SetPanelActive(root, "ComingSoonPanelRoot", false);
        SetPanelActive(root, "ReviewPanelRoot", false);
        economyRoot.gameObject.SetActive(true);
        SetSharedTitle(root, "경제 현황");
        SetTabVisuals(root, "EconomyTabButton");

        EconomyPanelVisibilityGuard guard = controller.GetComponent<EconomyPanelVisibilityGuard>();
        if (guard == null)
        {
            guard = controller.gameObject.AddComponent<EconomyPanelVisibilityGuard>();
        }

        guard.ShowEconomyOnly();

        EditorUtility.SetDirty(controller.gameObject);
        EditorUtility.SetDirty(economyRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }

    public static void BindExistingEconomyPanelDataOnly(GameRuntimeUIController controller, bool previewAfterBind)
    {
        if (controller == null)
        {
            return;
        }

        Transform root = ResolveRuntimeRoot(controller);
        if (root == null)
        {
            Debug.LogWarning("[EconomyPanelEditModeBuilder] RuntimeGameUIRoot를 찾지 못했습니다. 먼저 Materialize Game UI를 실행하세요.", controller);
            return;
        }

        Transform economyRoot = GameRuntimeUIControllerEditor.FindDeepChild(root, EconomyRootName);
        if (economyRoot == null)
        {
            Debug.LogWarning("[EconomyPanelEditModeBuilder] EconomyPanelRoot를 찾지 못했습니다. Rebuild Economy Panel을 한 번 실행해서 경제 패널을 만든 뒤, 수동 조정 후 Bind Economy Data Only를 사용하세요.", controller);
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Bind Economy Data");

        EconomyPanelDataBinder binder = economyRoot.GetComponent<EconomyPanelDataBinder>();
        if (binder == null)
        {
            binder = economyRoot.gameObject.AddComponent<EconomyPanelDataBinder>();
        }

        EconomyPanelRuntimeConnector connector = controller.GetComponent<EconomyPanelRuntimeConnector>();
        if (connector == null)
        {
            connector = controller.gameObject.AddComponent<EconomyPanelRuntimeConnector>();
        }

        EconomyPanelVisibilityGuard guard = controller.GetComponent<EconomyPanelVisibilityGuard>();
        if (guard == null)
        {
            guard = controller.gameObject.AddComponent<EconomyPanelVisibilityGuard>();
        }

        binder.RefreshNow();

        if (previewAfterBind)
        {
            SetPanelActive(root, "OperatePanelRoot", false);
            SetPanelActive(root, "InstallPanelRoot", false);
            SetPanelActive(root, "ComingSoonPanelRoot", false);
            SetPanelActive(root, "ReviewPanelRoot", false);
            economyRoot.gameObject.SetActive(true);
            SetSharedTitle(root, "경제 현황");
            SetTabVisuals(root, "EconomyTabButton");
            guard.ShowEconomyOnly();
        }

        EditorUtility.SetDirty(controller.gameObject);
        EditorUtility.SetDirty(economyRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }

    public static void BuildAndPreview(GameRuntimeUIController controller)
    {
        if (controller == null)
        {
            return;
        }

        Transform root = ResolveRuntimeRoot(controller);
        if (root == null)
        {
            Debug.LogWarning("[EconomyPanelEditModeBuilder] RuntimeGameUIRoot를 찾지 못했습니다. 먼저 Materialize Game UI를 실행하세요.", controller);
            return;
        }

        Transform sharedContent = GameRuntimeUIControllerEditor.FindDeepChild(root, "SharedPanelContentRoot");
        if (sharedContent == null)
        {
            Debug.LogWarning("[EconomyPanelEditModeBuilder] SharedPanelContentRoot를 찾지 못했습니다.", controller);
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Build Economy Panel");

        Transform economyRoot = FindDirectChild(sharedContent, EconomyRootName);
        if (economyRoot == null)
        {
            economyRoot = CreateNode(sharedContent, EconomyRootName).transform;
        }
        else
        {
            ClearChildren(economyRoot);
        }

        SetRect(economyRoot.GetComponent<RectTransform>(), 0f, 0f, 1010f, 682f);
        BuildEconomyContent(economyRoot);

        EconomyPanelDataBinder binder = economyRoot.GetComponent<EconomyPanelDataBinder>();
        if (binder == null)
        {
            binder = economyRoot.gameObject.AddComponent<EconomyPanelDataBinder>();
        }

        EconomyPanelRuntimeConnector connector = controller.GetComponent<EconomyPanelRuntimeConnector>();
        if (connector == null)
        {
            connector = controller.gameObject.AddComponent<EconomyPanelRuntimeConnector>();
        }

        binder.RefreshNow();

        SetPanelActive(root, "OperatePanelRoot", false);
        SetPanelActive(root, "InstallPanelRoot", false);
        SetPanelActive(root, "ComingSoonPanelRoot", false);
        SetPanelActive(root, "ReviewPanelRoot", false);
        economyRoot.gameObject.SetActive(true);

        SetSharedTitle(root, "경제 현황");
        SetTabVisuals(root, "EconomyTabButton");

        EconomyPanelVisibilityGuard guard = controller.GetComponent<EconomyPanelVisibilityGuard>();
        if (guard == null)
        {
            guard = controller.gameObject.AddComponent<EconomyPanelVisibilityGuard>();
        }

        guard.ShowEconomyOnly();

        EditorUtility.SetDirty(controller.gameObject);
        EditorUtility.SetDirty(economyRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }

    private static Transform ResolveRuntimeRoot(GameRuntimeUIController controller)
    {
        Transform root = controller.transform.Find("RuntimeGameUIRoot");
        if (root == null)
        {
            root = GameRuntimeUIControllerEditor.FindDeepChild(controller.transform, "RuntimeGameUIRoot");
        }

        return root;
    }

    private static void BuildEconomyContent(Transform parent)
    {
        CreateSummaryCard(parent, "EconomyIncomeSummaryCard", -378f, 246f, "수익", "8,400G", "GeneratedRuntimeUI/ui_v2/economy/economy_icon_income_up", IncomeGreen);
        CreateSummaryCard(parent, "EconomyExpenseSummaryCard", -126f, 246f, "지출", "2,100G", "GeneratedRuntimeUI/ui_v2/economy/economy_icon_expense_down", ExpenseRed);
        CreateSummaryCard(parent, "EconomyProfitSummaryCard", 126f, 246f, "순이익", "+6,300G", "GeneratedRuntimeUI/ui_v2/economy/economy_icon_profit_up", IncomeGreen);
        CreateSummaryCard(parent, "EconomyCashSummaryCard", 378f, 246f, "자금", "32,500G", "GeneratedRuntimeUI/ui_v2/economy/economy_icon_money_bag", Ink);

        GameObject incomeBox = CreateImage(parent, "EconomyIncomeDetailBox", "GeneratedRuntimeUI/ui_v2/economy/economy_detail_box", -253f, 50f, 488f, 218f, false);
        CreateText(incomeBox.transform, "Title", "수입 상세", 30, IncomeGreen, TextAnchor.MiddleLeft, -178f, 76f, 300f, 40f);
        CreateDivider(incomeBox.transform, "UpperDivider", 0f, 52f, 380f);
        CreateText(incomeBox.transform, "MembershipLabel", "회원권 수익", 26, Ink, TextAnchor.MiddleLeft, -154f, 18f, 210f, 36f);
        CreateText(incomeBox.transform, "MembershipValue", "6,000G", 26, Ink, TextAnchor.MiddleRight, 125f, 18f, 170f, 36f);
        CreateText(incomeBox.transform, "DailyLabel", "일일 입장 수익", 26, Ink, TextAnchor.MiddleLeft, -154f, -36f, 230f, 36f);
        CreateText(incomeBox.transform, "DailyValue", "2,400G", 26, Ink, TextAnchor.MiddleRight, 125f, -36f, 170f, 36f);
        CreateDivider(incomeBox.transform, "LowerDivider", 0f, -68f, 380f);
        CreateText(incomeBox.transform, "TotalLabel", "수입 합계", 27, IncomeGreen, TextAnchor.MiddleLeft, -154f, -96f, 210f, 40f);
        CreateText(incomeBox.transform, "TotalValue", "8,400G", 27, IncomeGreen, TextAnchor.MiddleRight, 125f, -96f, 170f, 40f);

        GameObject expenseBox = CreateImage(parent, "EconomyExpenseDetailBox", "GeneratedRuntimeUI/ui_v2/economy/economy_detail_box", 253f, 50f, 488f, 218f, false);
        CreateText(expenseBox.transform, "Title", "지출 상세", 30, ExpenseRed, TextAnchor.MiddleLeft, -178f, 76f, 300f, 40f);
        CreateDivider(expenseBox.transform, "UpperDivider", 0f, 52f, 380f);
        CreateText(expenseBox.transform, "SalaryLabel", "직원 급여", 26, Ink, TextAnchor.MiddleLeft, -154f, 18f, 210f, 36f);
        CreateText(expenseBox.transform, "SalaryValue", "1,200G", 26, Ink, TextAnchor.MiddleRight, 125f, 18f, 170f, 36f);
        CreateText(expenseBox.transform, "MaintenanceLabel", "유지비", 26, Ink, TextAnchor.MiddleLeft, -154f, -36f, 210f, 36f);
        CreateText(expenseBox.transform, "MaintenanceValue", "900G", 26, Ink, TextAnchor.MiddleRight, 125f, -36f, 170f, 36f);
        CreateDivider(expenseBox.transform, "LowerDivider", 0f, -68f, 380f);
        CreateText(expenseBox.transform, "TotalLabel", "지출 합계", 27, ExpenseRed, TextAnchor.MiddleLeft, -154f, -96f, 210f, 40f);
        CreateText(expenseBox.transform, "TotalValue", "2,100G", 27, ExpenseRed, TextAnchor.MiddleRight, 125f, -96f, 170f, 40f);

        GameObject chartBox = CreateImage(parent, "EconomyChartBox", "GeneratedRuntimeUI/ui_v2/economy/economy_bottom_box", -253f, -200f, 488f, 218f, false);
        CreateText(chartBox.transform, "Title", "최근 7일 수익 추이", 28, Ink, TextAnchor.MiddleLeft, -155f, 76f, 310f, 40f);
        CreateSolid(chartBox.transform, "Axis", new Color(0.25f, 0.16f, 0.07f, 0.70f), -8f, -58f, 360f, 4f);
        string[] labels = { "3/1", "3/2", "3/3", "3/4", "3/5", "3/6", "3/7" };
        float[] heights = { 70f, 82f, 56f, 86f, 66f, 82f, 112f };
        for (int i = 0; i < labels.Length; i++)
        {
            float x = -158f + i * 52f;
            string path = i == labels.Length - 1
                ? "GeneratedRuntimeUI/ui_v2/economy/economy_chart_bar_orange"
                : "GeneratedRuntimeUI/ui_v2/economy/economy_chart_bar_green";
            CreateImage(chartBox.transform, "Bar_" + i, path, x, -58f + heights[i] * 0.5f, 28f, heights[i], false);
            CreateText(chartBox.transform, "Date_" + i, labels[i], 20, Ink, TextAnchor.MiddleCenter, x, -84f, 48f, 26f);
        }

        GameObject memoBox = CreateImage(parent, "EconomyMemoBox", "GeneratedRuntimeUI/ui_v2/economy/economy_bottom_box", 253f, -200f, 488f, 218f, false);
        CreateText(memoBox.transform, "Title", "메모", 28, Ink, TextAnchor.MiddleLeft, -175f, 76f, 230f, 40f);
        CreateDivider(memoBox.transform, "MemoDivider", 20f, 46f, 330f);
        CreateText(memoBox.transform, "MemoText", "회원 증가로\n수익이 안정적으로\n오르고 있어요!", 28, Ink, TextAnchor.MiddleLeft, -160f, -20f, 250f, 132f);
        CreateImage(memoBox.transform, "ManagerCharacter", "GeneratedRuntimeUI/ui_v2/economy/economy_manager_character", 152f, -16f, 142f, 170f, true);
    }

    private static void CreateSummaryCard(Transform parent, string name, float x, float y, string title, string value, string iconPath, Color valueColor)
    {
        GameObject card = CreateImage(parent, name, "GeneratedRuntimeUI/ui_v2/economy/economy_summary_card_box", x, y, 235f, 132f, false);
        CreateImage(card.transform, "Icon", iconPath, -80f, 26f, 44f, 44f, true);
        CreateText(card.transform, "Label", title, 25, Ink, TextAnchor.MiddleLeft, -38f, 26f, 140f, 34f);
        CreateText(card.transform, "Value", value, 34, valueColor, TextAnchor.MiddleCenter, 0f, -28f, 190f, 50f);
    }

    private static GameObject CreateNode(Transform parent, string name, params System.Type[] components)
    {
        GameObject node = new GameObject(name, typeof(RectTransform));
        node.transform.SetParent(parent, false);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && node.GetComponent(components[i]) == null)
            {
                node.AddComponent(components[i]);
            }
        }

        return node;
    }

    private static GameObject CreateImage(Transform parent, string name, string path, float x, float y, float width, float height, bool preserveAspect)
    {
        GameObject node = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        Image image = node.GetComponent<Image>();
        image.raycastTarget = false;
        if (!GeneratedRuntimeSprites.Assign(image, path, preserveAspect))
        {
            image.color = new Color(1f, 0.88f, 0.56f, 0.18f);
        }

        SetRect(node.GetComponent<RectTransform>(), x, y, width, height);
        return node;
    }

    private static GameObject CreateSolid(Transform parent, string name, Color color, float x, float y, float width, float height)
    {
        GameObject node = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        Image image = node.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
        SetRect(node.GetComponent<RectTransform>(), x, y, width, height);
        return node;
    }

    private static void CreateDivider(Transform parent, string name, float x, float y, float width)
    {
        CreateSolid(parent, name, new Color(0.43f, 0.28f, 0.10f, 0.65f), x, y, width, 3f);
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor alignment, float x, float y, float width, float height)
    {
        GameObject node = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Text));
        Text text = node.GetComponent<Text>();
        text.text = value;
        text.font = LoadUiFont();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Normal;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.92f;
        SetRect(text.rectTransform, x, y, width, height);
        return text;
    }

    private static Font LoadUiFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/neodgm.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void SetPanelActive(Transform root, string panelName, bool active)
    {
        Transform panel = GameRuntimeUIControllerEditor.FindDeepChild(root, panelName);
        if (panel != null && panel.gameObject.activeSelf != active)
        {
            panel.gameObject.SetActive(active);
        }
    }

    private static void SetSharedTitle(Transform root, string title)
    {
        Transform titleTransform = GameRuntimeUIControllerEditor.FindDeepChild(root, "SharedPanelTitle");
        if (titleTransform == null)
        {
            return;
        }

        Text titleText = titleTransform.GetComponent<Text>();
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = Color.white;
        }
    }

    private static void SetTabVisuals(Transform root, string activeTabName)
    {
        SetTabVisual(root, "OperateTabButton", activeTabName == "OperateTabButton");
        SetTabVisual(root, "InstallTabButton", activeTabName == "InstallTabButton");
        SetTabVisual(root, "EconomyTabButton", activeTabName == "EconomyTabButton");
        SetTabVisual(root, "ReviewTabButton", activeTabName == "ReviewTabButton");
    }

    private static void SetTabVisual(Transform root, string tabName, bool active)
    {
        Transform tab = GameRuntimeUIControllerEditor.FindDeepChild(root, tabName);
        if (tab == null)
        {
            return;
        }

        Image image = tab.GetComponent<Image>();
        if (image != null)
        {
            GeneratedRuntimeSprites.Assign(
                image,
                active ? "GeneratedRuntimeUI/ui_v2/tab_active_green_base" : "GeneratedRuntimeUI/ui_v2/tab_inactive_beige_base",
                false);
        }

        Transform labelTransform = GameRuntimeUIControllerEditor.FindDeepChild(tab, "Label");
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
        if (label != null)
        {
            label.color = active ? Color.white : Ink;
        }
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ClearChildren(Transform target)
    {
        if (target == null)
        {
            return;
        }

        for (int i = target.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(target.GetChild(i).gameObject);
        }
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }
}

internal static class ReviewPanelEditModeBuilder
{
    private const string ReviewRootName = "ReviewPanelRoot";
    private const string ReviewAssetPath = "GeneratedRuntimeUI/ui_v2/review/";
    private static readonly Color Ink = new Color(0.12f, 0.08f, 0.03f, 1f);
    private static readonly Color Green = new Color(0.05f, 0.39f, 0.10f, 1f);
    private static readonly Color MutedInk = new Color(0.30f, 0.20f, 0.10f, 1f);

    public static void PreviewExistingReviewPanel(GameRuntimeUIController controller)
    {
        if (controller == null)
        {
            return;
        }

        Transform root = ResolveRuntimeRoot(controller);
        if (root == null)
        {
            Debug.LogWarning("[ReviewPanelEditModeBuilder] RuntimeGameUIRoot를 찾지 못했습니다. 먼저 Materialize Game UI를 실행하세요.", controller);
            return;
        }

        Transform reviewRoot = GameRuntimeUIControllerEditor.FindDeepChild(root, ReviewRootName);
        if (reviewRoot == null)
        {
            Debug.LogWarning("[ReviewPanelEditModeBuilder] ReviewPanelRoot를 찾지 못했습니다. Rebuild Review Panel을 한 번 실행해서 리뷰 패널을 만든 뒤 수동 조정하세요.", controller);
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Preview Review Panel Without Rebuild");

        BindReviewRuntimeOnly(controller, previewAfterBind: false);
        ShowReviewOnly(controller, root, reviewRoot);

        EditorUtility.SetDirty(controller.gameObject);
        EditorUtility.SetDirty(reviewRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }

    public static void BindReviewRuntimeOnly(GameRuntimeUIController controller, bool previewAfterBind)
    {
        if (controller == null)
        {
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Bind Review Runtime");

        ReviewPanelRuntimeConnector connector = controller.GetComponent<ReviewPanelRuntimeConnector>();
        if (connector == null)
        {
            connector = controller.gameObject.AddComponent<ReviewPanelRuntimeConnector>();
        }

        connector.RebindButtons();

        Transform root = ResolveRuntimeRoot(controller);
        Transform reviewRoot = root != null ? GameRuntimeUIControllerEditor.FindDeepChild(root, ReviewRootName) : null;
        if (reviewRoot != null)
        {
            ReviewPanelDataBinder binder = reviewRoot.GetComponent<ReviewPanelDataBinder>();
            if (binder == null)
            {
                binder = reviewRoot.gameObject.AddComponent<ReviewPanelDataBinder>();
            }

            binder.RefreshNow();
            EditorUtility.SetDirty(reviewRoot.gameObject);
        }

        if (previewAfterBind)
        {
            if (root != null && reviewRoot != null)
            {
                ShowReviewOnly(controller, root, reviewRoot);
            }
        }

        EditorUtility.SetDirty(controller.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }

    public static bool RepairReviewSpritesAndRows(GameRuntimeUIController controller, bool previewAfterRepair)
    {
        if (controller == null)
        {
            return false;
        }

        Transform root = ResolveRuntimeRoot(controller);
        if (root == null)
        {
            Debug.LogWarning("[ReviewPanelEditModeBuilder] RuntimeGameUIRoot를 찾지 못했습니다. Repair는 기존 리뷰 패널만 수정합니다.", controller);
            return false;
        }

        Transform reviewRoot = GameRuntimeUIControllerEditor.FindDeepChild(root, ReviewRootName);
        if (reviewRoot == null)
        {
            Debug.LogWarning("[ReviewPanelEditModeBuilder] ReviewPanelRoot를 찾지 못했습니다. Repair는 Rebuild 없이 기존 패널만 수정합니다.", controller);
            return false;
        }

        Undo.RegisterFullObjectHierarchyUndo(reviewRoot.gameObject, "Repair Review Sprites Without Rebuild");
        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Bind Review Runtime");

        EnsureReviewSpriteImporters();
        GeneratedRuntimeSprites.ClearCache();
        RepairReviewSprites(reviewRoot);
        ApplyReviewMockupLayout(root, reviewRoot);
        BindReviewRuntimeOnly(controller, previewAfterBind: false);

        if (previewAfterRepair)
        {
            ShowReviewOnly(controller, root, reviewRoot);
        }

        EditorUtility.SetDirty(controller.gameObject);
        EditorUtility.SetDirty(reviewRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log("[ReviewPanelEditModeBuilder] Repaired review sprites and rows without rebuilding sibling panels.", controller);
        return true;
    }

    public static void BuildAndPreview(GameRuntimeUIController controller)
    {
        if (controller == null)
        {
            return;
        }

        Transform root = ResolveRuntimeRoot(controller);
        if (root == null)
        {
            Debug.LogWarning("[ReviewPanelEditModeBuilder] RuntimeGameUIRoot를 찾지 못했습니다. 먼저 Materialize Game UI를 실행하세요.", controller);
            return;
        }

        Transform sharedContent = GameRuntimeUIControllerEditor.FindDeepChild(root, "SharedPanelContentRoot");
        if (sharedContent == null)
        {
            Debug.LogWarning("[ReviewPanelEditModeBuilder] SharedPanelContentRoot를 찾지 못했습니다.", controller);
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Build Review Panel");

        Transform reviewRoot = FindDirectChild(sharedContent, ReviewRootName);
        if (reviewRoot == null)
        {
            reviewRoot = CreateNode(sharedContent, ReviewRootName).transform;
        }
        else
        {
            ClearChildren(reviewRoot);
        }

        SetRect(reviewRoot.GetComponent<RectTransform>(), 0f, 0f, 1010f, 682f);
        BuildReviewContent(reviewRoot);
        ApplyReviewMockupLayout(root, reviewRoot);
        BindReviewRuntimeOnly(controller, previewAfterBind: false);
        ShowReviewOnly(controller, root, reviewRoot);

        EditorUtility.SetDirty(controller.gameObject);
        EditorUtility.SetDirty(reviewRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }

    private static void BuildReviewContent(Transform parent)
    {
        CreateReviewSummaryCard(parent, "ReviewSatisfactionSummaryCard", -326f, 252f, "전체 만족도:", "좋음", Asset("icon_mood_good"), Green);
        CreateReviewSummaryCard(parent, "ReviewNewCountSummaryCard", 0f, 252f, "오늘 신규 후기:", "3건", Asset("icon_review_chat"), Green);
        CreateReviewSummaryCard(parent, "ReviewRecommendSummaryCard", 326f, 252f, "추천 비율:", "87%", Asset("icon_review_like"), Green);

        CreateReviewListItem(
            parent,
            "ReviewListItem_0",
            120f,
            "김민수",
            "기구 배치가 좋아서 운동하기 편해요",
            Asset("icon_mood_good"),
            5);

        CreateReviewListItem(
            parent,
            "ReviewListItem_1",
            25f,
            "이서연",
            "러닝머신이 조금 부족한 것 같아요",
            Asset("icon_mood_normal"),
            4);

        CreateReviewListItem(
            parent,
            "ReviewListItem_2",
            -70f,
            "박준호",
            "직원분이 친절해서 좋았어요",
            Asset("icon_mood_good"),
            5);

        CreateReviewListItem(
            parent,
            "ReviewListItem_3",
            -165f,
            "최유진",
            "헬스장이 깔끔하고 쾌적해요",
            Asset("icon_mood_good"),
            5);

        CreateReviewListItem(
            parent,
            "ReviewListItem_4",
            -260f,
            "한지호",
            "초보자도 이용하기 편해요",
            Asset("icon_mood_normal"),
            4);

        CreateFilterButton(parent, "ReviewFilterAllButton", -366f, -342f, "전체", true);
        CreateFilterButton(parent, "ReviewFilterPositiveButton", -122f, -342f, "긍정", false);
        CreateFilterButton(parent, "ReviewFilterNegativeButton", 122f, -342f, "불만", false);
        CreateFilterButton(parent, "ReviewFilterLatestButton", 366f, -342f, "최신순 ▼", false);
    }

    private static void CreateReviewSummaryCard(Transform parent, string name, float x, float y, string title, string value, string iconPath, Color valueColor)
    {
        GameObject card = CreateImage(parent, name, Asset("review_summary_card_base"), x, y, 306f, 108f, false);
        CreateImage(card.transform, "Icon", iconPath, -116f, 0f, 50f, 50f, true);
        CreateText(card.transform, "Label", title, 24, Ink, TextAnchor.MiddleLeft, -42f, 0f, 188f, 54f);
        CreateText(card.transform, "Value", value, 27, valueColor, TextAnchor.MiddleLeft, 108f, 0f, 78f, 54f);
    }

    private static void CreateReviewListItem(Transform parent, string name, float y, string userName, string comment, string moodIconPath, int stars)
    {
        GameObject row = CreateImage(parent, name, Asset("review_list_item_base"), 0f, y, 960f, 94f, false);

        CreateImage(row.transform, "MoodIcon", moodIconPath, -426f, 0f, 56f, 56f, true);
        CreateText(row.transform, "UserName", userName, 28, Ink, TextAnchor.MiddleLeft, -316f, 0f, 142f, 42f);
        CreateText(row.transform, "Comment", comment, 26, Ink, TextAnchor.MiddleLeft, 20f, 0f, 500f, 42f);
        CreateStarRow(row.transform, "Stars", 5, stars, 380f, 0f, 30f, 30f, 32f);
    }

    private static void CreateStarRow(Transform parent, string name, int max, int filled, float x, float y, float width, float height, float spacing)
    {
        GameObject root = CreateNode(parent, name);
        SetRect(root.GetComponent<RectTransform>(), x, y, max * spacing, height);

        for (int i = 0; i < max; i++)
        {
            string spritePath = i < filled ? Asset("icon_star_full") : Asset("icon_star_empty");
            CreateImage(root.transform, "Star_" + i, spritePath, -((max - 1) * spacing) * 0.5f + i * spacing, 0f, width, height, true);
        }
    }

    private static void CreateFilterButton(Transform parent, string name, float x, float y, string label, bool active)
    {
        GameObject button = CreateImage(
            parent,
            name,
            active ? Asset("review_filter_button_active") : Asset("review_filter_button_inactive"),
            x,
            y,
            220f,
            68f,
            false);

        CreateText(
            button.transform,
            "Label",
            label,
            30,
            active ? Color.white : Ink,
            TextAnchor.MiddleCenter,
            0f,
            0f,
            200f,
            54f);
    }

    private static void EnsureReviewSpriteImporters()
    {
        string[] names =
        {
            "review_filter_button_active",
            "review_filter_button_inactive",
            "review_list_item_base",
            "review_summary_card_base",
            "icon_review_chat",
            "icon_review_like",
            "icon_mood_good",
            "icon_mood_normal",
            "icon_mood_bad",
            "icon_star_full",
            "icon_star_empty"
        };

        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            string assetPath = "Assets/_Project/Resources/" + Asset(name) + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[ReviewPanelEditModeBuilder] Missing review sprite importer: " + assetPath);
                continue;
            }

            Vector4 border = GetImporterBorder(name);
            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           importer.spritePixelsPerUnit != 100f ||
                           importer.mipmapEnabled ||
                           importer.alphaIsTransparency != true ||
                           importer.filterMode != FilterMode.Point ||
                           importer.wrapMode != TextureWrapMode.Clamp ||
                           importer.maxTextureSize < 4096 ||
                           importer.textureCompression != TextureImporterCompression.Uncompressed ||
                           importer.spriteBorder != border;

            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            if (textureSettings.spriteMeshType != SpriteMeshType.FullRect)
            {
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                changed = true;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (changed)
            {
                importer.SaveAndReimport();
            }

            NormalizeReviewSpriteRect(assetPath, importer, name, border);
        }

        AssetDatabase.Refresh();
    }

    private static void NormalizeReviewSpriteRect(string assetPath, TextureImporter importer, string name, Vector4 border)
    {
        if (importer == null)
        {
            return;
        }

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            return;
        }

        dataProvider.InitSpriteEditorDataProvider();
        ITextureDataProvider textureDataProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
        if (textureDataProvider == null)
        {
            return;
        }

        textureDataProvider.GetTextureActualWidthAndHeight(out int width, out int height);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        SpriteRect[] rects = dataProvider.GetSpriteRects();
        SpriteRect rect = rects != null && rects.Length > 0 ? rects[0] : new SpriteRect();
        Rect fullRect = new Rect(0f, 0f, width, height);
        Vector2 centerPivot = new Vector2(0.5f, 0.5f);
        bool changed = rects == null ||
                       rects.Length != 1 ||
                       rect.name != name ||
                       rect.rect != fullRect ||
                       rect.alignment != SpriteAlignment.Center ||
                       rect.pivot != centerPivot ||
                       rect.border != border;

        if (changed)
        {
            rect.name = name;
            rect.rect = fullRect;
            rect.alignment = SpriteAlignment.Center;
            rect.pivot = centerPivot;
            rect.border = border;

            dataProvider.SetSpriteRects(new[] { rect });
            dataProvider.Apply();
        }

        bool legacyChanged = NormalizeLegacySpriteSheetMetadata(importer, name, fullRect, border, centerPivot);
        if (!changed && !legacyChanged)
        {
            return;
        }

        AssetDatabase.ForceReserializeAssets(new[] { assetPath }, ForceReserializeAssetsOptions.ReserializeMetadata);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static bool NormalizeLegacySpriteSheetMetadata(TextureImporter importer, string name, Rect fullRect, Vector4 border, Vector2 pivot)
    {
        var serializedImporter = new SerializedObject(importer);
        serializedImporter.Update();

        SerializedProperty sprites = serializedImporter.FindProperty("m_SpriteSheet.m_Sprites");
        if (sprites == null)
        {
            return false;
        }

        bool changed = false;
        if (sprites.arraySize != 1)
        {
            sprites.arraySize = 1;
            changed = true;
        }

        SerializedProperty sprite = sprites.GetArrayElementAtIndex(0);
        changed |= SetString(sprite.FindPropertyRelative("m_Name"), name);
        changed |= SetRect(sprite.FindPropertyRelative("m_Rect"), fullRect);
        changed |= SetInt(sprite.FindPropertyRelative("m_Alignment"), (int)SpriteAlignment.Center);
        changed |= SetVector2(sprite.FindPropertyRelative("m_Pivot"), pivot);
        changed |= SetVector4(sprite.FindPropertyRelative("m_Border"), border);

        SerializedProperty spriteId = sprite.FindPropertyRelative("m_SpriteID");
        SerializedProperty rootSpriteId = serializedImporter.FindProperty("m_SpriteSheet.m_SpriteID");
        if (spriteId != null && string.IsNullOrEmpty(spriteId.stringValue))
        {
            string value = rootSpriteId != null && !string.IsNullOrEmpty(rootSpriteId.stringValue)
                ? rootSpriteId.stringValue
                : GUID.Generate().ToString();
            spriteId.stringValue = value;
            changed = true;
        }

        if (changed)
        {
            serializedImporter.ApplyModifiedPropertiesWithoutUndo();
        }

        return changed;
    }

    private static bool SetString(SerializedProperty property, string value)
    {
        if (property == null || property.stringValue == value)
        {
            return false;
        }

        property.stringValue = value;
        return true;
    }

    private static bool SetInt(SerializedProperty property, int value)
    {
        if (property == null || property.intValue == value)
        {
            return false;
        }

        property.intValue = value;
        return true;
    }

    private static bool SetRect(SerializedProperty property, Rect value)
    {
        if (property == null || property.rectValue == value)
        {
            return false;
        }

        property.rectValue = value;
        return true;
    }

    private static bool SetVector2(SerializedProperty property, Vector2 value)
    {
        if (property == null || property.vector2Value == value)
        {
            return false;
        }

        property.vector2Value = value;
        return true;
    }

    private static bool SetVector4(SerializedProperty property, Vector4 value)
    {
        if (property == null || property.vector4Value == value)
        {
            return false;
        }

        property.vector4Value = value;
        return true;
    }

    private static Vector4 GetImporterBorder(string name)
    {
        if (name == "review_list_item_base" || name == "review_summary_card_base")
        {
            return new Vector4(18f, 18f, 18f, 18f);
        }

        if (name == "review_filter_button_active" || name == "review_filter_button_inactive")
        {
            return new Vector4(12f, 12f, 12f, 12f);
        }

        return Vector4.zero;
    }

    private static void RepairReviewSprites(Transform reviewRoot)
    {
        AssignImage(reviewRoot, "ReviewSatisfactionSummaryCard", Asset("review_summary_card_base"), false);
        AssignImage(reviewRoot, "ReviewNewCountSummaryCard", Asset("review_summary_card_base"), false);
        AssignImage(reviewRoot, "ReviewRecommendSummaryCard", Asset("review_summary_card_base"), false);

        AssignChildImage(reviewRoot, "ReviewSatisfactionSummaryCard", "Icon", Asset("icon_mood_good"), true);
        AssignChildImage(reviewRoot, "ReviewNewCountSummaryCard", "Icon", Asset("icon_review_chat"), true);
        AssignChildImage(reviewRoot, "ReviewRecommendSummaryCard", "Icon", Asset("icon_review_like"), true);

        AssignImage(reviewRoot, "ReviewFilterAllButton", Asset("review_filter_button_active"), false);
        AssignImage(reviewRoot, "ReviewFilterPositiveButton", Asset("review_filter_button_inactive"), false);
        AssignImage(reviewRoot, "ReviewFilterNegativeButton", Asset("review_filter_button_inactive"), false);
        AssignImage(reviewRoot, "ReviewFilterLatestButton", Asset("review_filter_button_inactive"), false);

        for (int i = 0; i < 5; i++)
        {
            Transform row = GameRuntimeUIControllerEditor.FindDeepChild(reviewRoot, "ReviewListItem_" + i);
            if (row == null)
            {
                continue;
            }

            Image rowImage = row.GetComponent<Image>();
            if (rowImage != null)
            {
                GeneratedRuntimeSprites.Assign(rowImage, Asset("review_list_item_base"), false);
            }

            AssignChildImage(row, "MoodIcon", i == 1 || i == 4 ? Asset("icon_mood_normal") : Asset("icon_mood_good"), true);

            Transform stars = GameRuntimeUIControllerEditor.FindDeepChild(row, "Stars");
            if (stars == null)
            {
                continue;
            }

            for (int starIndex = 0; starIndex < 5; starIndex++)
            {
                string spritePath = starIndex < 4 || i != 1 && i != 4 ? Asset("icon_star_full") : Asset("icon_star_empty");
                AssignChildImage(stars, "Star_" + starIndex, spritePath, true);
            }
        }
    }

    private static void ApplyReviewMockupLayout(Transform root, Transform reviewRoot)
    {
        if (reviewRoot == null)
        {
            return;
        }

        SetRect(reviewRoot.GetComponent<RectTransform>(), 0f, 0f, 1010f, 682f);
        ApplyReviewHeaderLayout(root);

        LayoutReviewSummaryCard(reviewRoot, "ReviewSatisfactionSummaryCard", -326f, "전체 만족도:", "좋음", Asset("icon_mood_good"));
        LayoutReviewSummaryCard(reviewRoot, "ReviewNewCountSummaryCard", 0f, "오늘 신규 후기:", "3건", Asset("icon_review_chat"));
        LayoutReviewSummaryCard(reviewRoot, "ReviewRecommendSummaryCard", 326f, "추천 비율:", "87%", Asset("icon_review_like"));

        RepairReviewRowRects(reviewRoot);

        LayoutReviewFilterButton(reviewRoot, "ReviewFilterAllButton", -366f, "전체");
        LayoutReviewFilterButton(reviewRoot, "ReviewFilterPositiveButton", -122f, "긍정");
        LayoutReviewFilterButton(reviewRoot, "ReviewFilterNegativeButton", 122f, "불만");
        LayoutReviewFilterButton(reviewRoot, "ReviewFilterLatestButton", 366f, "최신순 ▼");
    }

    private static void ApplyReviewHeaderLayout(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Transform header = GameRuntimeUIControllerEditor.FindDeepChild(root, "SharedPanelHeaderBar");
        if (header != null)
        {
            Image image = header.GetComponent<Image>();
            if (image != null)
            {
                GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/ui_v2/header_bar_blue", false);
            }

            SetRect(header, 0f, 390f, 1000f, 86f);
        }

        SetTextLayout(root, "SharedPanelTitle", "회원 후기", 45, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 0f, 390f, 930f, 76f);
    }

    private static void LayoutReviewSummaryCard(Transform reviewRoot, string cardName, float x, string label, string value, string iconPath)
    {
        Transform card = GameRuntimeUIControllerEditor.FindDeepChild(reviewRoot, cardName);
        if (card == null)
        {
            return;
        }

        SetRect(card, x, 252f, 306f, 108f);
        AssignChildImage(card, "Icon", iconPath, true);
        SetRect(GetRect(card, "Icon"), -116f, 0f, 50f, 50f);
        SetTextLayout(card, "Label", label, 24, Ink, TextAnchor.MiddleLeft, FontStyle.Normal, -42f, 0f, 188f, 54f);
        SetTextLayout(card, "Value", value, 27, Green, TextAnchor.MiddleLeft, FontStyle.Bold, 108f, 0f, 78f, 54f);
    }

    private static void LayoutReviewFilterButton(Transform reviewRoot, string buttonName, float x, string label)
    {
        Transform button = GameRuntimeUIControllerEditor.FindDeepChild(reviewRoot, buttonName);
        if (button == null)
        {
            return;
        }

        SetRect(button, x, -342f, 220f, 68f);
        SetTextLayout(button, "Label", label, 30, buttonName == "ReviewFilterAllButton" ? Color.white : Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 0f, 0f, 200f, 54f);
    }

    private static void RepairReviewRowRects(Transform reviewRoot)
    {
        float[] rowY = { 120f, 25f, -70f, -165f, -260f };
        for (int i = 0; i < 5; i++)
        {
            Transform row = GameRuntimeUIControllerEditor.FindDeepChild(reviewRoot, "ReviewListItem_" + i);
            if (row == null)
            {
                continue;
            }

            SetRect(row, 0f, rowY[i], 960f, 94f);
            SetRect(GetRect(row, "MoodIcon"), -426f, 0f, 56f, 56f);
            SetTextLayout(row, "UserName", null, 28, Ink, TextAnchor.MiddleLeft, FontStyle.Bold, -316f, 0f, 142f, 42f);
            SetTextLayout(row, "Comment", null, 26, Ink, TextAnchor.MiddleLeft, FontStyle.Normal, 20f, 0f, 500f, 42f);

            Transform stars = GameRuntimeUIControllerEditor.FindDeepChild(row, "Stars");
            if (stars == null)
            {
                continue;
            }

            SetRect(stars, 380f, 0f, 160f, 30f);

            for (int starIndex = 0; starIndex < 5; starIndex++)
            {
                float x = -64f + (starIndex * 32f);
                SetRect(GetRect(stars, "Star_" + starIndex), x, 0f, 30f, 30f);
            }
        }
    }

    public static bool VerifyReviewPanelLayout(GameRuntimeUIController controller, bool logDetails)
    {
        if (controller == null)
        {
            return false;
        }

        Transform root = ResolveRuntimeRoot(controller);
        Transform reviewRoot = root != null ? GameRuntimeUIControllerEditor.FindDeepChild(root, ReviewRootName) : null;
        if (reviewRoot == null)
        {
            Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: ReviewPanelRoot missing.", controller);
            return false;
        }

        bool ok = VerifyReviewAssets() && reviewRoot.GetComponent<ReviewPanelDataBinder>() != null;
        ok &= VerifyText(root, "SharedPanelTitle", "회원 후기", controller);
        ok &= VerifyRectTransform(reviewRoot.GetComponent<RectTransform>(), ReviewRootName, 0f, 0f, 1010f, 682f, controller);
        ok &= VerifyRect(reviewRoot, "ReviewSatisfactionSummaryCard", -326f, 252f, 306f, 108f, controller);
        ok &= VerifyRect(reviewRoot, "ReviewNewCountSummaryCard", 0f, 252f, 306f, 108f, controller);
        ok &= VerifyRect(reviewRoot, "ReviewRecommendSummaryCard", 326f, 252f, 306f, 108f, controller);

        float[] rowY = { 120f, 25f, -70f, -165f, -260f };
        for (int i = 0; i < 5; i++)
        {
            Transform row = GameRuntimeUIControllerEditor.FindDeepChild(reviewRoot, "ReviewListItem_" + i);
            if (row == null)
            {
                Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: missing ReviewListItem_" + i, controller);
                ok = false;
                continue;
            }

            ok &= VerifyRectTransform(row.GetComponent<RectTransform>(), row.name, 0f, rowY[i], 960f, 94f, controller);

            Image rowImage = row.GetComponent<Image>();
            if (rowImage == null || rowImage.sprite == null)
            {
                Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: row sprite missing on " + row.name, controller);
                ok = false;
            }
            else if (rowImage.sprite.border.y > 24f || rowImage.sprite.border.w > 24f)
            {
                Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: row border too large on " + row.name + " border=" + rowImage.sprite.border, controller);
                ok = false;
            }

            ok &= VerifyNoOverlap(row, "MoodIcon", "UserName", controller);
            ok &= VerifyNoOverlap(row, "UserName", "Comment", controller);
            ok &= VerifyNoOverlap(row, "Comment", "Stars", controller);
            ok &= VerifyStarSize(row, controller);
        }

        if (logDetails)
        {
            Debug.Log(ok
                ? "[ReviewPanelEditModeBuilder] Review panel verify passed."
                : "[ReviewPanelEditModeBuilder] Review panel verify failed.", controller);
        }

        return ok;
    }

    private static bool VerifyText(Transform root, string childName, string expected, Object context)
    {
        Transform child = GameRuntimeUIControllerEditor.FindDeepChild(root, childName);
        Text text = child != null ? child.GetComponent<Text>() : null;
        if (text == null || text.text != expected)
        {
            Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: " + childName + " text mismatch.", context);
            return false;
        }

        return true;
    }

    private static bool VerifyRect(Transform root, string childName, float x, float y, float width, float height, Object context)
    {
        return VerifyRectTransform(GetRect(root, childName), childName, x, y, width, height, context);
    }

    private static bool VerifyRectTransform(RectTransform rect, string label, float x, float y, float width, float height, Object context)
    {
        if (rect == null)
        {
            Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: missing RectTransform for " + label, context);
            return false;
        }

        const float epsilon = 1.5f;
        bool ok = Mathf.Abs(rect.anchoredPosition.x - x) <= epsilon &&
                  Mathf.Abs(rect.anchoredPosition.y - y) <= epsilon &&
                  Mathf.Abs(rect.sizeDelta.x - width) <= epsilon &&
                  Mathf.Abs(rect.sizeDelta.y - height) <= epsilon;
        if (!ok)
        {
            Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: rect mismatch for " + label + " pos=" + rect.anchoredPosition + " size=" + rect.sizeDelta, context);
        }

        return ok;
    }

    private static bool VerifyReviewAssets()
    {
        string[] names =
        {
            "review_filter_button_active",
            "review_filter_button_inactive",
            "review_list_item_base",
            "review_summary_card_base",
            "icon_review_chat",
            "icon_review_like",
            "icon_mood_good",
            "icon_mood_normal",
            "icon_mood_bad",
            "icon_star_full",
            "icon_star_empty"
        };

        bool ok = true;
        for (int i = 0; i < names.Length; i++)
        {
            string assetPath = "Assets/_Project/Resources/" + Asset(names[i]) + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null)
            {
                Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: missing review asset " + assetPath);
                ok = false;
            }
        }

        return ok;
    }

    private static bool VerifyStarSize(Transform row, Object context)
    {
        Transform stars = GameRuntimeUIControllerEditor.FindDeepChild(row, "Stars");
        if (stars == null)
        {
            Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: missing Stars under " + row.name, context);
            return false;
        }

        bool ok = true;
        for (int i = 0; i < 5; i++)
        {
            Transform star = GameRuntimeUIControllerEditor.FindDeepChild(stars, "Star_" + i);
            RectTransform rect = star != null ? star.GetComponent<RectTransform>() : null;
            if (rect == null ||
                rect.sizeDelta.x < 24f ||
                rect.sizeDelta.x > 34f ||
                rect.sizeDelta.y < 24f ||
                rect.sizeDelta.y > 34f)
            {
                Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: star size outside mockup range in " + row.name, context);
                ok = false;
            }
        }

        return ok;
    }

    private static bool VerifyNoOverlap(Transform row, string leftName, string rightName, Object context)
    {
        RectTransform left = GetRect(row, leftName);
        RectTransform right = GetRect(row, rightName);
        if (left == null || right == null)
        {
            return false;
        }

        float leftRight = left.anchoredPosition.x + (left.sizeDelta.x * 0.5f);
        float rightLeft = right.anchoredPosition.x - (right.sizeDelta.x * 0.5f);
        if (leftRight > rightLeft)
        {
            Debug.LogError("[ReviewPanelEditModeBuilder] Verify failed: " + leftName + " overlaps " + rightName + " in " + row.name, context);
            return false;
        }

        return true;
    }

    private static void AssignImage(Transform root, string objectName, string spritePath, bool preserveAspect)
    {
        Transform target = GameRuntimeUIControllerEditor.FindDeepChild(root, objectName);
        Image image = target != null ? target.GetComponent<Image>() : null;
        if (image != null)
        {
            GeneratedRuntimeSprites.Assign(image, spritePath, preserveAspect);
        }
    }

    private static void AssignChildImage(Transform root, string childName, string spritePath, bool preserveAspect)
    {
        AssignImage(root, childName, spritePath, preserveAspect);
    }

    private static void AssignChildImage(Transform root, string parentName, string childName, string spritePath, bool preserveAspect)
    {
        Transform parent = GameRuntimeUIControllerEditor.FindDeepChild(root, parentName);
        if (parent != null)
        {
            AssignChildImage(parent, childName, spritePath, preserveAspect);
        }
    }

    private static void SetRect(Transform target, float x, float y, float width, float height)
    {
        RectTransform rect = target != null ? target.GetComponent<RectTransform>() : null;
        SetRect(rect, x, y, width, height);
    }

    private static void SetTextLayout(Transform root, string childName, string value, int fontSize, Color color, TextAnchor alignment, FontStyle style, float x, float y, float width, float height)
    {
        Transform target = GameRuntimeUIControllerEditor.FindDeepChild(root, childName);
        Text text = target != null ? target.GetComponent<Text>() : null;
        if (text == null)
        {
            return;
        }

        if (value != null)
        {
            text.text = value;
        }

        text.font = LoadUiFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.92f;
        SetRect(text.rectTransform, x, y, width, height);
        EditorUtility.SetDirty(text);
    }

    private static void SetRectIfOldOrBroken(Transform root, string childName, float x, float y, float width, float height, float oldX, float oldY, float oldWidth, float oldHeight)
    {
        RectTransform rect = GetRect(root, childName);
        if (rect == null)
        {
            return;
        }

        if (IsBrokenRect(rect) || RectApproximately(rect, oldX, oldY, oldWidth, oldHeight))
        {
            SetRect(rect, x, y, width, height);
        }
    }

    private static void SetRectIfOldOrBroken(Transform target, float x, float y, float width, float height, float oldX, float oldY, float oldWidth, float oldHeight)
    {
        RectTransform rect = target != null ? target.GetComponent<RectTransform>() : null;
        if (rect == null)
        {
            return;
        }

        if (IsBrokenRect(rect) || RectApproximately(rect, oldX, oldY, oldWidth, oldHeight))
        {
            SetRect(rect, x, y, width, height);
        }
    }

    private static RectTransform GetRect(Transform root, string childName)
    {
        Transform child = GameRuntimeUIControllerEditor.FindDeepChild(root, childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private static bool IsBrokenRect(RectTransform rect)
    {
        return rect.sizeDelta.x < 4f || rect.sizeDelta.y < 4f;
    }

    private static bool RectApproximately(RectTransform rect, float x, float y, float width, float height)
    {
        const float epsilon = 0.75f;
        return Mathf.Abs(rect.anchoredPosition.x - x) <= epsilon &&
               Mathf.Abs(rect.anchoredPosition.y - y) <= epsilon &&
               Mathf.Abs(rect.sizeDelta.x - width) <= epsilon &&
               Mathf.Abs(rect.sizeDelta.y - height) <= epsilon;
    }

    private static void ShowReviewOnly(GameRuntimeUIController controller, Transform root, Transform reviewRoot)
    {
        SetPanelActive(root, "OperatePanelRoot", false);
        SetPanelActive(root, "InstallPanelRoot", false);
        SetPanelActive(root, "EconomyPanelRoot", false);
        SetPanelActive(root, "ComingSoonPanelRoot", false);
        reviewRoot.gameObject.SetActive(true);
        SetSharedTitle(root, "회원 후기");
        SetTabVisuals(root, "ReviewTabButton");

        EconomyPanelVisibilityGuard guard = controller.GetComponent<EconomyPanelVisibilityGuard>();
        if (guard == null)
        {
            guard = controller.gameObject.AddComponent<EconomyPanelVisibilityGuard>();
        }

        guard.HideEconomy();
    }

    private static string Asset(string name)
    {
        return ReviewAssetPath + name;
    }

    private static Transform ResolveRuntimeRoot(GameRuntimeUIController controller)
    {
        Transform root = controller.transform.Find("RuntimeGameUIRoot");
        if (root == null)
        {
            root = GameRuntimeUIControllerEditor.FindDeepChild(controller.transform, "RuntimeGameUIRoot");
        }

        return root;
    }

    private static GameObject CreateNode(Transform parent, string name, params System.Type[] components)
    {
        GameObject node = new GameObject(name, typeof(RectTransform));
        node.transform.SetParent(parent, false);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && node.GetComponent(components[i]) == null)
            {
                node.AddComponent(components[i]);
            }
        }

        return node;
    }

    private static GameObject CreateImage(Transform parent, string name, string path, float x, float y, float width, float height, bool preserveAspect)
    {
        GameObject node = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        Image image = node.GetComponent<Image>();
        image.raycastTarget = false;

        if (!GeneratedRuntimeSprites.Assign(image, path, preserveAspect))
        {
            image.color = new Color(1f, 0.88f, 0.56f, 0.18f);
        }

        SetRect(node.GetComponent<RectTransform>(), x, y, width, height);
        return node;
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor alignment, float x, float y, float width, float height)
    {
        GameObject node = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Text));
        Text text = node.GetComponent<Text>();
        text.text = value;
        text.font = LoadUiFont();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Normal;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.92f;
        SetRect(text.rectTransform, x, y, width, height);
        return text;
    }

    private static Font LoadUiFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/neodgm.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void SetPanelActive(Transform root, string panelName, bool active)
    {
        Transform panel = GameRuntimeUIControllerEditor.FindDeepChild(root, panelName);
        if (panel != null && panel.gameObject.activeSelf != active)
        {
            panel.gameObject.SetActive(active);
        }
    }

    private static void SetSharedTitle(Transform root, string title)
    {
        Transform titleTransform = GameRuntimeUIControllerEditor.FindDeepChild(root, "SharedPanelTitle");
        if (titleTransform == null)
        {
            return;
        }

        Text titleText = titleTransform.GetComponent<Text>();
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = Color.white;
        }
    }

    private static void SetTabVisuals(Transform root, string activeTabName)
    {
        SetTabVisual(root, "OperateTabButton", activeTabName == "OperateTabButton");
        SetTabVisual(root, "InstallTabButton", activeTabName == "InstallTabButton");
        SetTabVisual(root, "EconomyTabButton", activeTabName == "EconomyTabButton");
        SetTabVisual(root, "ReviewTabButton", activeTabName == "ReviewTabButton");
    }

    private static void SetTabVisual(Transform root, string tabName, bool active)
    {
        Transform tab = GameRuntimeUIControllerEditor.FindDeepChild(root, tabName);
        if (tab == null)
        {
            return;
        }

        Image image = tab.GetComponent<Image>();
        if (image != null)
        {
            GeneratedRuntimeSprites.Assign(
                image,
                active ? "GeneratedRuntimeUI/ui_v2/tab_active_green_base" : "GeneratedRuntimeUI/ui_v2/tab_inactive_beige_base",
                false);
        }

        Transform labelTransform = GameRuntimeUIControllerEditor.FindDeepChild(tab, "Label");
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
        if (label != null)
        {
            label.color = active ? Color.white : Ink;
        }
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ClearChildren(Transform target)
    {
        if (target == null)
        {
            return;
        }

        for (int i = target.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(target.GetChild(i).gameObject);
        }
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }
}

public static class ReviewPanelRepairBatch
{
    private const string TestSandboxScenePath = "Assets/_Project/Scenes/TestSandbox.unity";

    public static void RepairAndVerifyTestSandbox()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(TestSandboxScenePath, OpenSceneMode.Single);
        GameRuntimeUIController controller = Object.FindFirstObjectByType<GameRuntimeUIController>();
        if (controller == null)
        {
            Debug.LogError("[ReviewPanelRepairBatch] GameRuntimeUIController not found in TestSandbox.");
            EditorApplication.Exit(1);
            return;
        }

        bool repaired = ReviewPanelEditModeBuilder.RepairReviewSpritesAndRows(controller, previewAfterRepair: true);
        bool verified = repaired && ReviewPanelEditModeBuilder.VerifyReviewPanelLayout(controller, logDetails: true);
        if (!verified)
        {
            Debug.LogError("[ReviewPanelRepairBatch] Review panel repair verification failed.");
            EditorApplication.Exit(1);
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ReviewPanelRepairBatch] Review panel repair verification passed and TestSandbox was saved.");
    }
}
#endif
