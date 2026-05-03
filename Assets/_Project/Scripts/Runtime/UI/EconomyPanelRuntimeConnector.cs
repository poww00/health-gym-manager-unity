using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class EconomyPanelRuntimeConnector : MonoBehaviour
{
    private const string EconomyTitle = "경제 현황";

    private Transform runtimeRoot;
    private Transform economyRoot;
    private Text sharedTitleText;
    private Button economyTabButton;
    private bool boundRuntimeButton;

    private void OnEnable()
    {
        Resolve();
        BindEconomyButtonIfNeeded();
    }

    private void Update()
    {
        Resolve();

        if (Application.isPlaying)
        {
            BindEconomyButtonIfNeeded();
        }
    }

    public void ShowEconomyPanel()
    {
        if (!Resolve())
        {
            return;
        }

        SetPanelActive("OperatePanelRoot", false);
        SetPanelActive("InstallPanelRoot", false);
        SetPanelActive("ComingSoonPanelRoot", false);
        SetPanelActive("ReviewPanelRoot", false);

        if (economyRoot != null)
        {
            economyRoot.gameObject.SetActive(true);
            EconomyPanelDataBinder binder = economyRoot.GetComponent<EconomyPanelDataBinder>();
            if (binder != null)
            {
                binder.RefreshNow();
            }
        }

        if (sharedTitleText != null)
        {
            sharedTitleText.text = EconomyTitle;
            sharedTitleText.color = Color.white;
        }

        SetTabVisual("OperateTabButton", false);
        SetTabVisual("InstallTabButton", false);
        SetTabVisual("EconomyTabButton", true);
        SetTabVisual("ReviewTabButton", false);
    }

    private void BindEconomyButtonIfNeeded()
    {
        if (!Application.isPlaying || boundRuntimeButton)
        {
            return;
        }

        if (!Resolve() || economyTabButton == null)
        {
            return;
        }

        economyTabButton.onClick.RemoveAllListeners();
        economyTabButton.onClick.AddListener(ShowEconomyPanel);
        boundRuntimeButton = true;
    }

    private bool Resolve()
    {
        if (runtimeRoot == null)
        {
            runtimeRoot = transform.Find("RuntimeGameUIRoot");
        }

        if (runtimeRoot == null)
        {
            runtimeRoot = FindDeepChild(transform, "RuntimeGameUIRoot");
        }

        if (runtimeRoot == null)
        {
            return false;
        }

        if (economyRoot == null)
        {
            economyRoot = FindDeepChild(runtimeRoot, "EconomyPanelRoot");
        }

        if (sharedTitleText == null)
        {
            Transform title = FindDeepChild(runtimeRoot, "SharedPanelTitle");
            sharedTitleText = title != null ? title.GetComponent<Text>() : null;
        }

        if (economyTabButton == null)
        {
            Transform tab = FindDeepChild(runtimeRoot, "EconomyTabButton");
            economyTabButton = tab != null ? tab.GetComponent<Button>() : null;
        }

        return true;
    }

    private void SetPanelActive(string panelName, bool active)
    {
        Transform panel = FindDeepChild(runtimeRoot, panelName);
        if (panel != null && panel.gameObject.activeSelf != active)
        {
            panel.gameObject.SetActive(active);
        }
    }

    private void SetTabVisual(string tabName, bool active)
    {
        Transform tab = FindDeepChild(runtimeRoot, tabName);
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
