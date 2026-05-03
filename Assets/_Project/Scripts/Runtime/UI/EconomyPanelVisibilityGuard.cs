using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class EconomyPanelVisibilityGuard : MonoBehaviour
{
    private const string EconomyTitle = "경제 현황";

    private Transform cachedRoot;
    private Transform cachedEconomyRoot;
    private Text cachedTitleText;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            RefreshVisibility();
        }
    }

    private void Update()
    {
        // Edit mode에서는 패널 미리보기 버튼을 누른 뒤 상태가 남기 때문에 계속 보정한다.
        // Play mode에서도 EconomyPanelRoot가 씬에 저장된 채 시작했을 때 운영/설치 위로 덮이지 않도록 막는다.
        if (!Application.isPlaying)
        {
            RefreshVisibility();
        }
    }

    public void RefreshVisibility()
    {
        if (!Resolve())
        {
            return;
        }

        bool shouldShowEconomy = cachedTitleText != null && cachedTitleText.text == EconomyTitle;
        if (cachedEconomyRoot != null && cachedEconomyRoot.gameObject.activeSelf != shouldShowEconomy)
        {
            cachedEconomyRoot.gameObject.SetActive(shouldShowEconomy);
        }
    }

    public void ShowEconomyOnly()
    {
        if (!Resolve())
        {
            return;
        }

        SetPanelActive("OperatePanelRoot", false);
        SetPanelActive("InstallPanelRoot", false);
        SetPanelActive("ComingSoonPanelRoot", false);
        SetPanelActive("ReviewPanelRoot", false);

        if (cachedEconomyRoot != null)
        {
            cachedEconomyRoot.gameObject.SetActive(true);
        }

        if (cachedTitleText != null)
        {
            cachedTitleText.text = EconomyTitle;
        }
    }

    public void HideEconomy()
    {
        if (!Resolve())
        {
            return;
        }

        if (cachedEconomyRoot != null)
        {
            cachedEconomyRoot.gameObject.SetActive(false);
        }
    }

    private bool Resolve()
    {
        cachedRoot ??= transform.Find("RuntimeGameUIRoot");
        if (cachedRoot == null)
        {
            cachedRoot = FindDeepChild(transform, "RuntimeGameUIRoot");
        }

        if (cachedRoot == null)
        {
            return false;
        }

        cachedEconomyRoot ??= FindDeepChild(cachedRoot, "EconomyPanelRoot");

        if (cachedTitleText == null)
        {
            Transform title = FindDeepChild(cachedRoot, "SharedPanelTitle");
            cachedTitleText = title != null ? title.GetComponent<Text>() : null;
        }

        return true;
    }

    private void SetPanelActive(string panelName, bool active)
    {
        Transform panel = FindDeepChild(cachedRoot, panelName);
        if (panel != null && panel.gameObject.activeSelf != active)
        {
            panel.gameObject.SetActive(active);
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
