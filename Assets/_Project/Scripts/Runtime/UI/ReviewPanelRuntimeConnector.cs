using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ReviewPanelRuntimeConnector : MonoBehaviour
{
    private const string ReviewRootName = "ReviewPanelRoot";
    private const string ActiveTabSprite = "GeneratedRuntimeUI/ui_v2/tab_active_green_base";
    private const string InactiveTabSprite = "GeneratedRuntimeUI/ui_v2/tab_inactive_beige_base";

    private void Awake()
    {
        RebindButtons();
    }

    private void OnEnable()
    {
        RebindButtons();
    }

    public void RebindButtons()
    {
        Transform root = ResolveRuntimeRoot();
        if (root == null)
        {
            return;
        }

        Transform reviewTab = FindDeepChild(root, "ReviewTabButton");
        if (reviewTab == null)
        {
            return;
        }

        Button button = reviewTab.GetComponent<Button>();
        if (button == null)
        {
            button = reviewTab.gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ShowReviewPanel);
    }

    public void ShowReviewPanel()
    {
        Transform root = ResolveRuntimeRoot();
        if (root == null)
        {
            Debug.LogWarning("[ReviewPanelRuntimeConnector] RuntimeGameUIRoot를 찾지 못했습니다.", this);
            return;
        }

        Transform reviewRoot = FindDeepChild(root, ReviewRootName);
        if (reviewRoot == null)
        {
            Debug.LogWarning("[ReviewPanelRuntimeConnector] ReviewPanelRoot를 찾지 못했습니다. 에디터에서 Rebuild Review Panel을 먼저 실행하세요.", this);
            return;
        }

        SetPanelActive(root, "OperatePanelRoot", false);
        SetPanelActive(root, "InstallPanelRoot", false);
        SetPanelActive(root, "EconomyPanelRoot", false);
        SetPanelActive(root, "ComingSoonPanelRoot", false);
        reviewRoot.gameObject.SetActive(true);

        SetSharedTitle(root, "회원 후기");
        SetTabVisuals(root, "ReviewTabButton");
    }

    private Transform ResolveRuntimeRoot()
    {
        Transform root = transform.Find("RuntimeGameUIRoot");
        if (root == null)
        {
            root = FindDeepChild(transform, "RuntimeGameUIRoot");
        }

        return root;
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
            GeneratedRuntimeSprites.Assign(image, active ? ActiveTabSprite : InactiveTabSprite, false);
        }

        Transform labelTransform = FindDeepChild(tab, "Label");
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
        if (label != null)
        {
            label.color = active ? Color.white : new Color(0.12f, 0.08f, 0.03f, 1f);
        }
    }

    private static Transform FindDeepChild(Transform root, string childName)
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
}
