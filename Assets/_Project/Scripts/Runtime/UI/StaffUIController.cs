using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaffUIController : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject popupPanel;
    public Button openButton;
    public Button closeButton;

    [Header("Tabs")]
    public Button staffTabBtn;
    public Button applicantTabBtn;
    public Text titleText;

    [Header("Summary")]
    public Text staffCountText;
    public Text applicantCountText;
    public Text payrollText;

    [Header("Scroll Views")]
    public ScrollRect staffScroll;
    public ScrollRect applicantScroll;

    [Header("Contents")]
    public Transform staffContent;
    public Transform applicantContent;

    private const string TitleStaff = "\uc9c1\uc6d0 \uad00\ub9ac";
    private const string TitleQueue = "\ucc44\uc6a9 \ub300\uae30\uc5f4";
    private const string TabStaff = "\uadfc\ubb34 \uc911";
    private const string TabApplicant = "\uc9c0\uc6d0\uc790";
    private const string LabelStaff = "\uc9c1\uc6d0";
    private const string LabelApplicant = "\uc9c0\uc6d0\uc790";
    private const string LabelPayroll = "\uae09\uc5ec";
    private const string EmptyStaffTitle = "\uadfc\ubb34 \uc911\uc778 \uc9c1\uc6d0\uc774 \uc5c6\uc2b5\ub2c8\ub2e4";
    private const string EmptyStaffBody = "\uce74\uc6b4\ud130, \ud2b8\ub808\uc774\ub108, \uccad\uc18c \uc9c1\uc6d0\uc744 \ucc44\uc6a9\ud574 \uc6b4\uc601\uc744 \uc548\uc815\uc2dc\ucf1c \ubcf4\uc138\uc694.";
    private const string EmptyApplicantTitle = "\uc624\ub298\uc740 \uc9c0\uc6d0\uc790\uac00 \uc5c6\uc2b5\ub2c8\ub2e4";
    private const string EmptyApplicantBody = "\ud558\ub8e8\uac00 \uc9c0\ub098\uba74 \uc0c8 \uc9c0\uc6d0\uc790\uac00 \ub4e4\uc5b4\uc624\ub2c8 \ub2e4\uc74c \ub0a0\uc9dc\ub97c \uae30\ub2e4\ub824 \uc8fc\uc138\uc694.";
    private const string HireReadyLabel = "\uc624\ub298 \ubc14\ub85c \ucc44\uc6a9\ud560 \uc218 \uc788\uc2b5\ub2c8\ub2e4";
    private const string FireLabel = "\ud574\uace0";
    private const string HireLabel = "\ucc44\uc6a9";
    private const string RoleReception = "\uce74\uc6b4\ud130";
    private const string RoleTrainer = "\ud2b8\ub808\uc774\ub108";
    private const string RoleCleaner = "\uccad\uc18c";

    private readonly Color ink = new Color(0.13f, 0.2f, 0.28f, 1f);
    private readonly Color subInk = new Color(0.35f, 0.42f, 0.48f, 1f);

    private GameUiTheme theme;
    private StaffManager staffManager;
    private Button dimBackgroundButton;
    private int activeTabIndex;
    private bool isBuilt;

    public void Configure(GameUiTheme uiTheme)
    {
        theme = uiTheme ?? GameUiTheme.CreateDefault();
    }

    public void BuildUi(Transform canvasRoot)
    {
        if (isBuilt)
        {
            return;
        }

        theme ??= GameUiTheme.CreateDefault();

        popupPanel = GameUiFactory.CreateNode(canvasRoot, "StaffModalRoot", typeof(CanvasRenderer), typeof(Image));
        Image overlay = popupPanel.GetComponent<Image>();
        overlay.color = theme.Overlay;
        overlay.raycastTarget = true;
        GameUiFactory.Stretch(popupPanel.GetComponent<RectTransform>());

        dimBackgroundButton = GameUiFactory.GetOrAdd<Button>(popupPanel);
        dimBackgroundButton.transition = Selectable.Transition.None;

        RectTransform windowContent;
        GameObject window = GameUiFactory.CreatePanel(popupPanel.transform, "StaffWindow", theme, theme.PanelFill, out windowContent, 24f);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(windowRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1180f));

        VerticalLayoutGroup windowLayout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(windowContent.gameObject);
        windowLayout.padding = new RectOffset(0, 0, 0, 0);
        windowLayout.spacing = 12f;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = false;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;
        windowLayout.childAlignment = TextAnchor.UpperLeft;

        GameObject headerRow = GameUiFactory.CreateNode(windowContent, "HeaderRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        GameUiFactory.GetOrAdd<LayoutElement>(headerRow).preferredHeight = 56f;

        titleText = GameUiFactory.CreateText(headerRow.transform, "Title", theme, 32, theme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement titleLayout = GameUiFactory.GetOrAdd<LayoutElement>(titleText.gameObject);
        titleLayout.flexibleWidth = 1f;
        titleLayout.preferredHeight = 56f;
        titleText.text = TitleStaff;

        closeButton = GameUiFactory.CreateButton(headerRow.transform, "CloseButton", theme, "X", GameUiTone.Surface, out _, 6f);
        LayoutElement closeLayout = GameUiFactory.GetOrAdd<LayoutElement>(closeButton.gameObject);
        closeLayout.preferredWidth = 56f;
        closeLayout.preferredHeight = 56f;

        GameObject tabRow = GameUiFactory.CreateNode(windowContent, "TabRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 12f;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = false;
        GameUiFactory.GetOrAdd<LayoutElement>(tabRow).preferredHeight = 62f;

        staffTabBtn = GameUiFactory.CreateButton(tabRow.transform, "StaffTab", theme, TabStaff, GameUiTone.Accent, out _, 12f);
        applicantTabBtn = GameUiFactory.CreateButton(tabRow.transform, "ApplicantTab", theme, TabApplicant, GameUiTone.Surface, out _, 12f);
        GameUiFactory.GetOrAdd<LayoutElement>(staffTabBtn.gameObject).preferredHeight = 62f;
        GameUiFactory.GetOrAdd<LayoutElement>(applicantTabBtn.gameObject).preferredHeight = 62f;

        GameObject summaryRow = GameUiFactory.CreateNode(windowContent, "SummaryRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup summaryRowLayout = summaryRow.GetComponent<HorizontalLayoutGroup>();
        summaryRowLayout.spacing = 12f;
        summaryRowLayout.childControlWidth = true;
        summaryRowLayout.childControlHeight = true;
        summaryRowLayout.childForceExpandWidth = true;
        summaryRowLayout.childForceExpandHeight = false;
        GameUiFactory.GetOrAdd<LayoutElement>(summaryRow).preferredHeight = 118f;

        CreateSummaryMetric(summaryRow.transform, "StaffCount", LabelStaff, out staffCountText);
        CreateSummaryMetric(summaryRow.transform, "ApplicantCount", LabelApplicant, out applicantCountText);
        CreateSummaryMetric(summaryRow.transform, "Payroll", LabelPayroll, out payrollText);

        RectTransform bodyContent;
        GameObject bodyCard = GameUiFactory.CreateCard(windowContent, "BodyCard", theme, out bodyContent, 830f);
        GameUiFactory.GetOrAdd<LayoutElement>(bodyCard).preferredHeight = 830f;

        staffScroll = GameUiFactory.CreateScrollView(bodyContent, "StaffScroll", theme, out RectTransform staffContentRoot);
        GameUiFactory.Stretch(staffScroll.GetComponent<RectTransform>());
        staffContent = staffContentRoot;

        applicantScroll = GameUiFactory.CreateScrollView(bodyContent, "ApplicantScroll", theme, out RectTransform applicantContentRoot);
        GameUiFactory.Stretch(applicantScroll.GetComponent<RectTransform>());
        applicantContent = applicantContentRoot;
        applicantScroll.gameObject.SetActive(false);

        popupPanel.SetActive(false);
        isBuilt = true;

        staffManager = FindFirstObjectByType<StaffManager>();
        if (staffManager != null)
        {
            staffManager.ApplicantsChanged += HandleApplicantsChanged;
            staffManager.HiredStaffChanged += HandleHiredStaffChanged;
        }

        BindButtons();
        ApplyTabVisuals(0);
        RefreshSummary();
        RefreshStaffList();
        RefreshApplicantList();
    }

    private void OnDestroy()
    {
        if (staffManager != null)
        {
            staffManager.ApplicantsChanged -= HandleApplicantsChanged;
            staffManager.HiredStaffChanged -= HandleHiredStaffChanged;
        }
    }

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (dimBackgroundButton != null)
        {
            dimBackgroundButton.onClick.RemoveListener(ClosePopup);
            dimBackgroundButton.onClick.AddListener(ClosePopup);
        }

        if (staffTabBtn != null)
        {
            staffTabBtn.onClick.RemoveAllListeners();
            staffTabBtn.onClick.AddListener(() => SwitchTab(0));
        }

        if (applicantTabBtn != null)
        {
            applicantTabBtn.onClick.RemoveAllListeners();
            applicantTabBtn.onClick.AddListener(() => SwitchTab(1));
        }
    }

    public void OpenPopup()
    {
        EnsureBuiltFromSceneCanvas();

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }

        RefreshSummary();
        RefreshStaffList();
        RefreshApplicantList();
        SwitchTab(activeTabIndex);
    }

    private void EnsureBuiltFromSceneCanvas()
    {
        if (isBuilt)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas != null)
        {
            BuildUi(canvas.transform);
        }
    }

    public void ClosePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    private void SwitchTab(int tabIndex)
    {
        activeTabIndex = tabIndex;
        if (staffScroll != null) staffScroll.gameObject.SetActive(tabIndex == 0);
        if (applicantScroll != null) applicantScroll.gameObject.SetActive(tabIndex == 1);
        ApplyTabVisuals(tabIndex);
        RefreshSummary();
    }

    private void ApplyTabVisuals(int activeIndex)
    {
        UpdateTab(staffTabBtn, activeIndex == 0, TabStaff);
        UpdateTab(applicantTabBtn, activeIndex == 1, TabApplicant);

        if (titleText != null)
        {
            titleText.text = activeIndex == 0 ? TitleStaff : TitleQueue;
        }
    }

    private void UpdateTab(Button button, bool active, string label)
    {
        if (button == null)
        {
            return;
        }

        Transform fill = button.transform.Find("Fill");
        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = active ? theme.Accent : theme.TabIdle;
            }
        }

        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = label;
            text.color = active ? theme.BrightInk : theme.Ink;
        }
    }

    private void HandleApplicantsChanged()
    {
        RefreshSummary();
        RefreshApplicantList();
    }

    private void HandleHiredStaffChanged()
    {
        RefreshSummary();
        RefreshStaffList();
    }

    private void RefreshSummary()
    {
        int hiredCount = staffManager != null ? staffManager.HiredStaff.Count : 0;
        int applicantCount = staffManager != null ? staffManager.AvailableApplicants.Count : 0;
        int payroll = staffManager != null ? staffManager.GetTotalMonthlySalary() : 0;

        SetTextIfChanged(staffCountText, $"{hiredCount}\uba85");
        SetTextIfChanged(applicantCountText, $"{applicantCount}\uba85");
        SetTextIfChanged(payrollText, $"{payroll:N0} G");
    }

    private void RefreshStaffList()
    {
        if (staffContent == null)
        {
            return;
        }

        GameUiFactory.ClearChildren(staffContent);

        if (staffManager == null || staffManager.HiredStaff.Count == 0)
        {
            CreateEmptyState(staffContent, EmptyStaffTitle, EmptyStaffBody);
            return;
        }

        foreach (StaffData staff in staffManager.HiredStaff)
        {
            CreateStaffRow(staffContent, staff, true);
        }
    }

    private void RefreshApplicantList()
    {
        if (applicantContent == null)
        {
            return;
        }

        GameUiFactory.ClearChildren(applicantContent);

        if (staffManager == null || staffManager.AvailableApplicants.Count == 0)
        {
            CreateEmptyState(applicantContent, EmptyApplicantTitle, EmptyApplicantBody);
            return;
        }

        foreach (StaffData applicant in staffManager.AvailableApplicants)
        {
            CreateStaffRow(applicantContent, applicant, false);
        }
    }

    private void CreateEmptyState(Transform parent, string title, string body)
    {
        RectTransform contentRoot;
        GameObject panel = GameUiFactory.CreateCard(parent, "EmptyState", theme, out contentRoot, 168f);
        VerticalLayoutGroup layout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(contentRoot.gameObject);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text titleTextObj = GameUiFactory.CreateText(contentRoot, "Title", theme, 28, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        GameUiFactory.GetOrAdd<LayoutElement>(titleTextObj.gameObject).preferredHeight = 34f;
        titleTextObj.text = title;

        Text bodyTextObj = GameUiFactory.CreateText(contentRoot, "Body", theme, 22, subInk, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.GetOrAdd<LayoutElement>(bodyTextObj.gameObject).preferredHeight = 78f;
        bodyTextObj.text = body;
    }

    private void CreateSummaryMetric(Transform parent, string name, string label, out Text valueText)
    {
        RectTransform cardContent;
        GameObject card = GameUiFactory.CreateCard(parent, name, theme, out cardContent, 112f);
        LayoutElement cardLayout = GameUiFactory.GetOrAdd<LayoutElement>(card);
        cardLayout.preferredHeight = 112f;
        cardLayout.flexibleWidth = 1f;

        Text labelText = GameUiFactory.CreateText(cardContent, "Label", theme, 18, theme.MutedInk, TextAnchor.UpperCenter, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 24f));
        labelText.text = label;

        valueText = GameUiFactory.CreateText(cardContent, "Value", theme, 28, theme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(0f, -28f));
        valueText.text = "--";
    }

    private void CreateStaffRow(Transform parent, StaffData staffData, bool isHired)
    {
        RectTransform contentRoot;
        GameObject row = GameUiFactory.CreateListRow(parent, isHired ? "StaffRow" : "ApplicantRow", theme, out contentRoot, 154f);

        Text nameText = GameUiFactory.CreateText(contentRoot, "Name", theme, 22, ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(-170f, 28f));
        nameText.text = $"[{GetRoleName(staffData)}] {staffData.staffName}";

        Text salaryText = GameUiFactory.CreateText(contentRoot, "Salary", theme, 18, subInk, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(salaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(-170f, 22f));
        salaryText.text = $"\uc6d4\uae09 {staffData.monthlySalary:N0} G";

        Text detailText = GameUiFactory.CreateText(contentRoot, "Detail", theme, 18, ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(detailText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(-170f, 34f));
        detailText.text = BuildStatsText(staffData);

        Text stateText = GameUiFactory.CreateText(contentRoot, "State", theme, 16, subInk, TextAnchor.LowerLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(stateText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -2f), new Vector2(-170f, 38f));
        stateText.text = isHired ? BuildHiredStateText(staffData) : HireReadyLabel;

        GameUiTone tone = isHired ? GameUiTone.Danger : GameUiTone.Accent;
        Button actionButton = GameUiFactory.CreateButton(contentRoot, "Action", theme, isHired ? FireLabel : HireLabel, tone, out _, 10f);
        GameUiFactory.SetAnchoredRect(actionButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(132f, 52f));

        if (staffManager != null)
        {
            if (isHired)
            {
                actionButton.onClick.AddListener(() => staffManager.FireStaff(staffData.staffId));
            }
            else
            {
                actionButton.onClick.AddListener(() => staffManager.HireApplicant(staffData));
            }
        }
    }

    private void SetTextIfChanged(Text text, string value)
    {
        if (text != null && text.text != value)
        {
            text.text = value;
        }
    }

    private string GetRoleName(StaffData data)
    {
        if (staffManager != null)
        {
            return staffManager.GetRoleNameKOR(data.role);
        }

        switch (data.role)
        {
            case StaffRole.Receptionist:
                return RoleReception;
            case StaffRole.Trainer:
                return RoleTrainer;
            default:
                return RoleCleaner;
        }
    }

    private static string BuildStatsText(StaffData data)
    {
        switch (data.role)
        {
            case StaffRole.Receptionist:
                return $"\uc678\ubaa8 {data.looks}  |  \ud68c\uc6d0 \uc751\ub300 \ubcf4\uc870";
            case StaffRole.Trainer:
                return $"\uc678\ubaa8 {data.looks}  |  \ub9ac\ub354\uc2ed {data.leadership}  |  PT {data.ptMemberCount}";
            default:
                return $"\uccad\uc18c {data.cleaningSkill}  |  \uc2dc\uc124 \uad00\ub9ac";
        }
    }

    private static string BuildHiredStateText(StaffData data)
    {
        switch (data.role)
        {
            case StaffRole.Receptionist:
                return "\ud68c\uc6d0 \uc751\ub300\uc640 \uc785\uad6c \uc6b4\uc601\uc744 \ub9e1\uace0 \uc788\uc2b5\ub2c8\ub2e4";
            case StaffRole.Trainer:
                return data.ptMemberCount > 0
                    ? $"\ud604\uc7ac PT \ud68c\uc6d0 {data.ptMemberCount}\uba85\uc744 \ub9e1\uace0 \uc788\uc2b5\ub2c8\ub2e4"
                    : "\uc6b4\ub3d9 \uad6c\uc5ed\uc744 \uc21c\ud68c\ud558\uba70 \ud68c\uc6d0\uc744 \ub3d5\uace0 \uc788\uc2b5\ub2c8\ub2e4";
            default:
                return "\uccb4\uc721\uad00 \uccad\uacb0\uacfc \ud68c\ubcf5 \uad6c\uc5ed\uc744 \uad00\ub9ac\ud558\uace0 \uc788\uc2b5\ub2c8\ub2e4";
        }
    }

}
