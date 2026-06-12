using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialDimmerController : MonoBehaviour
{
    private const string ViewName = "TutorialDimmerView";
    private TutorialDimmerView view;
    private Rect screenRect;
    private TutorialFocusTarget currentTarget;
    private bool initialized;

    public TutorialFocusTarget CurrentTarget => currentTarget;
    public Rect CurrentFocusRect => view != null ? view.CurrentFocusRect : Rect.zero;
    public TutorialFocusMode CurrentFocusMode => view != null ? view.CurrentFocusMode : TutorialFocusMode.None;

    public void Initialize(Rect screen, Color dimColor)
    {
        screenRect = screen;
        EnsureRootRect();
        EnsureView(dimColor);
        initialized = true;
    }

    public void Show(TutorialFocusTarget focusTarget)
    {
        if (!initialized)
        {
            Initialize(screenRect.width > 0f ? screenRect : new Rect(-540f, -960f, 1080f, 1920f), new Color(0f, 0f, 0f, 0.58f));
        }

        currentTarget = focusTarget;
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerController.Show called go=\"{gameObject.name}\" scene=\"{gameObject.scene.name}\" activeBefore={gameObject.activeSelf} mode={focusTarget.Mode} target=\"{focusTarget.TargetName}\" rect={focusTarget.Rect} interact={focusTarget.AllowFocusInteraction}");
        gameObject.SetActive(true);
        view.Show(focusTarget);
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerController.Show finished activeAfter={gameObject.activeSelf} panels={GetDebugPanelStateSummary()}");
        Debug.Log($"[TutorialDimmer] mode={focusTarget.Mode}, target={focusTarget.TargetName}, rect={focusTarget.Rect}, interact={focusTarget.AllowFocusInteraction}");
    }

    public void Hide()
    {
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerController.Hide called go=\"{gameObject.name}\" activeBefore={gameObject.activeSelf} panels={GetDebugPanelStateSummary()}");
        if (view != null)
        {
            view.Hide();
        }

        currentTarget = TutorialFocusTarget.None();
        gameObject.SetActive(false);
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerController.Hide finished activeAfter={gameObject.activeSelf}");
    }

    public void SetDebugOutlineVisible(bool visible)
    {
        if (view != null)
        {
            view.SetDebugOutlineVisible(visible);
        }
    }

    public void LogCurrentFocusRect()
    {
        Debug.Log($"[TutorialDimmer] current mode={CurrentFocusMode}, rect={CurrentFocusRect}, target={currentTarget.TargetName}");
    }

    public bool TryGetCurrentDimPanelRects(out Rect top, out Rect bottom, out Rect left, out Rect right)
    {
        if (view != null)
        {
            return view.TryGetCurrentDimPanelRects(out top, out bottom, out left, out right);
        }

        top = Rect.zero;
        bottom = Rect.zero;
        left = Rect.zero;
        right = Rect.zero;
        return false;
    }

    public string GetDebugPanelStateSummary()
    {
        return view != null ? view.GetDebugPanelStateSummary() : "view=null";
    }

    private void EnsureRootRect()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }

        SetRect(rect, 0f, 0f, screenRect.width, screenRect.height);

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Destroy(raycaster);
        }
    }

    private void EnsureView(Color dimColor)
    {
        if (view != null)
        {
            view.Initialize(screenRect, dimColor);
            return;
        }

        Transform existing = transform.Find(ViewName);
        bool willCreateView = existing == null;
        GameObject viewObject = existing != null
            ? existing.gameObject
            : new GameObject(ViewName, typeof(RectTransform), typeof(TutorialDimmerView));
        viewObject.transform.SetParent(transform, false);
        viewObject.transform.SetAsFirstSibling();

        RectTransform viewRect = viewObject.GetComponent<RectTransform>();
        SetRect(viewRect, 0f, 0f, screenRect.width, screenRect.height);

        view = viewObject.GetComponent<TutorialDimmerView>();
        view.Initialize(screenRect, dimColor);
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerController.EnsureView viewCreated={willCreateView} view=\"{viewObject.name}\" scene=\"{viewObject.scene.name}\" activeSelf={viewObject.activeSelf} activeInHierarchy={viewObject.activeInHierarchy}");
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
