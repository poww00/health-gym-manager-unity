using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class ReviewPanelDataBinder : MonoBehaviour
{
    private const float RefreshInterval = 0.35f;
    private const int MinimumRowCount = 5;
    private const string ReviewAssetPath = "GeneratedRuntimeUI/ui_v2/review/";
    private const float RuntimeFirstRowY = 100f;
    private const int RuntimeCommentFontSize = 24;
    private const float RuntimeCommentMinHeight = 40f;
    private const float RuntimeSummaryCardVisibleBottomInset = 36f;
    private const float RuntimeViewportTopGap = 0f;
    private const float RuntimeViewportBottomGap = 4f;
    private const float RuntimeUserNameX = -300f;
    private const float RuntimeUserNameWidth = 170f;
    private const float RuntimeCommentX = 55f;
    private const float RuntimeCommentWidth = 500f;
    private const string EmptyReviewMessage = "아직 작성된 리뷰가 없습니다.";

    private enum ReviewFilterMode
    {
        All,
        Positive,
        Negative
    }

    [Header("Row Layout Sync")]
    [SerializeField] private bool syncAllRowsFromFirstRow = true;
    [FormerlySerializedAs("primaryRowName")]
    [SerializeField] private string templateRowName = "ReviewListItem_0";
    [SerializeField] private float firstRowY = -35f;
    [SerializeField] private float rowSpacingY = 100f;

    [Header("Stars")]
    [SerializeField] private Vector2 starSize = new Vector2(62f, 62f);
    [SerializeField] private float starStep = 44f;

    private GymEconomyManager economyManager;
    private float nextRefreshAt;

    private Text satisfactionValue;
    private Text newCountValue;
    private Text recommendValue;

    private Button allFilterButton;
    private Button positiveFilterButton;
    private Button negativeFilterButton;
    private Button sortFilterButton;
    private Image allFilterImage;
    private Image positiveFilterImage;
    private Image negativeFilterImage;
    private Image sortFilterImage;
    private Text allFilterLabel;
    private Text positiveFilterLabel;
    private Text negativeFilterLabel;
    private Text sortFilterLabel;
    private ScrollRect listScrollRect;
    private RectTransform listViewportRect;
    private RectTransform listContentRect;
    private Text emptyStateText;
    private RectTransform emptyStateRect;

    private ReviewFilterMode currentFilter = ReviewFilterMode.All;
    private bool latestFirst = true;

    private readonly List<ReviewRow> rows = new List<ReviewRow>(MinimumRowCount);

    private sealed class ReviewEntry
    {
        public string authorName;
        public string text;
        public float stars;
        public int order;

        public ReviewEntry(string authorName, string text, float stars, int order)
        {
            this.authorName = authorName;
            this.text = text;
            this.stars = stars;
            this.order = order;
        }
    }

    private sealed class ReviewRow
    {
        public GameObject root;
        public RectTransform rect;
        public Image moodIcon;
        public RectTransform moodIconRect;
        public Text userName;
        public RectTransform userNameRect;
        public Text comment;
        public RectTransform commentRect;
        public RectTransform starsRootRect;
        public readonly Image[] stars = new Image[5];
        public readonly RectTransform[] starRects = new RectTransform[5];
    }

    private void OnEnable()
    {
        Resolve();
        RefreshNow();
        ResetScrollToTop();
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup < nextRefreshAt)
        {
            return;
        }

        nextRefreshAt = Time.realtimeSinceStartup + RefreshInterval;
        RefreshNow();
    }

    public void RefreshNow()
    {
        Resolve();
        ApplyFilterBindings();
        RefreshFilterButtonVisuals();

        List<ReviewEntry> source = BuildSourceReviews();
        RefreshSummary(source);

        List<ReviewEntry> visible = ApplyFilterAndSort(source);
        bool hasAnyReviews = source != null && source.Count > 0;
        EnsureRuntimeRowCapacity(visible != null ? visible.Count : 1);
        AdjustRuntimeListViewport();

        if (syncAllRowsFromFirstRow)
        {
            ApplyTemplateRow();
        }

        ApplyStarLayout();
        DisplayRows(visible, hasAnyReviews);
        RefreshScrollContentBounds(hasAnyReviews && visible != null ? visible.Count : 1);
    }

    private void Resolve()
    {
        if (economyManager == null)
        {
            economyManager = FindFirstObjectByType<GymEconomyManager>();
        }

        if (satisfactionValue == null)
        {
            satisfactionValue = FindText("Value", "ReviewSatisfactionSummaryCard");
        }

        if (newCountValue == null)
        {
            newCountValue = FindText("Value", "ReviewNewCountSummaryCard");
        }

        if (recommendValue == null)
        {
            recommendValue = FindText("Value", "ReviewRecommendSummaryCard");
        }

        ResolveButtons();
        ResolveScroll();
        ResolveEmptyStateText();

        EnsureRowSlotCount(MinimumRowCount);
        for (int i = 0; i < rows.Count; i++)
        {
            ResolveRow(i, false);
        }
    }

    private void ResolveScroll()
    {
        if (listScrollRect == null)
        {
            Transform viewport = FindDeepChild(transform, "ListViewport");
            listScrollRect = viewport != null ? viewport.GetComponent<ScrollRect>() : GetComponentInChildren<ScrollRect>(true);
        }

        if (listViewportRect == null && listScrollRect != null)
        {
            listViewportRect = listScrollRect.viewport != null
                ? listScrollRect.viewport
                : listScrollRect.GetComponent<RectTransform>();
        }

        if (listContentRect == null && listScrollRect != null)
        {
            listContentRect = listScrollRect.content;
        }

        if (listContentRect == null)
        {
            Transform template = FindTemplateRow();
            listContentRect = template != null ? template.parent as RectTransform : null;
        }
    }

    private void ResolveEmptyStateText()
    {
        if (emptyStateText == null)
        {
            Transform existing = FindDeepChild(transform, "ReviewEmptyStateText");
            emptyStateText = existing != null ? existing.GetComponent<Text>() : null;
            emptyStateRect = existing as RectTransform;
        }

        if (emptyStateText == null && Application.isPlaying)
        {
            Transform parent = listViewportRect != null
                ? listViewportRect
                : listContentRect != null
                    ? listContentRect
                    : transform;

            GameObject node = new GameObject("ReviewEmptyStateText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            node.transform.SetParent(parent, false);
            emptyStateText = node.GetComponent<Text>();
            emptyStateRect = node.GetComponent<RectTransform>();
            node.SetActive(false);
        }

        if (emptyStateText == null)
        {
            return;
        }

        if (emptyStateRect == null)
        {
            emptyStateRect = emptyStateText.GetComponent<RectTransform>();
        }

        if (emptyStateRect != null)
        {
            emptyStateRect.anchorMin = Vector2.zero;
            emptyStateRect.anchorMax = Vector2.one;
            emptyStateRect.pivot = new Vector2(0.5f, 0.5f);
            emptyStateRect.offsetMin = Vector2.zero;
            emptyStateRect.offsetMax = Vector2.zero;
            emptyStateRect.SetAsLastSibling();
        }

        Text templateComment = null;
        Transform template = FindTemplateRow();
        if (template != null)
        {
            templateComment = GetText(template, "Comment");
        }

        emptyStateText.font = templateComment != null && templateComment.font != null
            ? templateComment.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        emptyStateText.fontSize = RuntimeCommentFontSize;
        emptyStateText.alignment = TextAnchor.MiddleCenter;
        emptyStateText.color = templateComment != null
            ? templateComment.color
            : new Color(0.20f, 0.12f, 0.04f, 1f);
        emptyStateText.raycastTarget = false;
    }

    private void ResolveButtons()
    {
        ResolveButton(new[] { "AllFilterButton", "ReviewFilterAllButton" }, ref allFilterButton, ref allFilterImage, ref allFilterLabel);
        ResolveButton(new[] { "PositiveFilterButton", "ReviewFilterPositiveButton" }, ref positiveFilterButton, ref positiveFilterImage, ref positiveFilterLabel);
        ResolveButton(new[] { "NegativeFilterButton", "ReviewFilterNegativeButton" }, ref negativeFilterButton, ref negativeFilterImage, ref negativeFilterLabel);
        ResolveButton(new[] { "SortFilterButton", "ReviewFilterLatestButton" }, ref sortFilterButton, ref sortFilterImage, ref sortFilterLabel);
    }

    private void ResolveButton(string[] candidates, ref Button button, ref Image image, ref Text label)
    {
        Transform target = null;
        for (int i = 0; i < candidates.Length && target == null; i++)
        {
            target = FindDeepChild(transform, candidates[i]);
        }

        if (target == null)
        {
            return;
        }

        image = target.GetComponent<Image>();
        label = GetText(target, "Label");

        button = target.GetComponent<Button>();
        if (button == null)
        {
            button = target.gameObject.AddComponent<Button>();
        }

        if (image != null)
        {
            image.raycastTarget = true;
            button.targetGraphic = image;
        }
    }

    private void ApplyFilterBindings()
    {
        BindButton(allFilterButton, HandleAllFilter);
        BindButton(positiveFilterButton, HandlePositiveFilter);
        BindButton(negativeFilterButton, HandleNegativeFilter);
        BindButton(sortFilterButton, HandleSortToggle);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void HandleAllFilter()
    {
        currentFilter = ReviewFilterMode.All;
        RefreshNow();
        ResetScrollToTop();
    }

    private void HandlePositiveFilter()
    {
        currentFilter = ReviewFilterMode.Positive;
        RefreshNow();
        ResetScrollToTop();
    }

    private void HandleNegativeFilter()
    {
        currentFilter = ReviewFilterMode.Negative;
        RefreshNow();
        ResetScrollToTop();
    }

    private void HandleSortToggle()
    {
        latestFirst = !latestFirst;
        RefreshNow();
        ResetScrollToTop();
    }

    private void RefreshFilterButtonVisuals()
    {
        ApplyFilterButtonVisual(allFilterImage, allFilterLabel, currentFilter == ReviewFilterMode.All, "전체");
        ApplyFilterButtonVisual(positiveFilterImage, positiveFilterLabel, currentFilter == ReviewFilterMode.Positive, "긍정");
        ApplyFilterButtonVisual(negativeFilterImage, negativeFilterLabel, currentFilter == ReviewFilterMode.Negative, "부정");
        ApplyFilterButtonVisual(sortFilterImage, sortFilterLabel, false, latestFirst ? "최신순 ▼" : "오래된순 ▲");
    }

    private void ApplyFilterButtonVisual(Image image, Text label, bool active, string text)
    {
        if (image != null)
        {
            GeneratedRuntimeSprites.Assign(image, ReviewAssetPath + (active ? "review_filter_button_active" : "review_filter_button_inactive"), false);
            image.raycastTarget = true;
        }

        if (label != null)
        {
            label.text = text;
            label.color = active ? Color.white : new Color(0.12f, 0.08f, 0.03f, 1f);
        }
    }

    private void ResolveRow(int index, bool allowRuntimeCreate)
    {
        ReviewRow row = rows[index];
        if (row == null)
        {
            row = new ReviewRow();
            rows[index] = row;
        }

        Transform rowTransform = FindRowTransform(index);
        if (rowTransform == null && allowRuntimeCreate)
        {
            rowTransform = CreateRuntimeRow(index);
        }

        if (rowTransform == null)
        {
            return;
        }

        row.root = rowTransform.gameObject;
        row.rect = rowTransform as RectTransform;
        row.moodIcon = GetImage(rowTransform, "MoodIcon");
        row.moodIconRect = row.moodIcon != null ? row.moodIcon.rectTransform : null;
        row.userName = GetText(rowTransform, "UserName");
        row.userNameRect = row.userName != null ? row.userName.GetComponent<RectTransform>() : null;
        row.comment = GetText(rowTransform, "Comment");
        row.commentRect = row.comment != null ? row.comment.GetComponent<RectTransform>() : null;

        Transform starsRoot = FindDeepChild(rowTransform, "Stars");
        row.starsRootRect = starsRoot as RectTransform;
        for (int i = 0; i < row.stars.Length; i++)
        {
            row.stars[i] = starsRoot != null ? GetImage(starsRoot, "Star_" + i) : null;
            row.starRects[i] = row.stars[i] != null ? row.stars[i].rectTransform : null;
        }
    }

    private void ApplyTemplateRow()
    {
        Transform template = FindTemplateRow();
        if (template == null)
        {
            return;
        }

        RectTransform templateRect = template as RectTransform;
        RectTransform templateMoodRect = GetImage(template, "MoodIcon")?.rectTransform;
        Text templateUser = GetText(template, "UserName");
        Text templateComment = GetText(template, "Comment");
        RectTransform templateUserRect = templateUser != null ? templateUser.GetComponent<RectTransform>() : null;
        RectTransform templateCommentRect = templateComment != null ? templateComment.GetComponent<RectTransform>() : null;
        RectTransform templateStarsRect = FindDeepChild(template, "Stars") as RectTransform;
        RectTransform[] templateStarRects = new RectTransform[5];
        for (int i = 0; i < templateStarRects.Length; i++)
        {
            templateStarRects[i] = GetImage(templateStarsRect, "Star_" + i)?.rectTransform;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            ReviewRow row = rows[i];
            if (row == null || row.rect == null)
            {
                continue;
            }

            CopyRootRectWithSpacing(templateRect, row.rect, i);
            CopyRect(templateMoodRect, row.moodIconRect);
            CopyRect(templateUserRect, row.userNameRect);
            CopyRect(templateCommentRect, row.commentRect);
            CopyTextStyle(templateUser, row.userName);
            CopyTextStyle(templateComment, row.comment);
            ApplyRuntimeUserNameStyle(row.userName, row.userNameRect);
            ApplyRuntimeCommentStyle(row.comment, row.commentRect);
            CopyRect(templateStarsRect, row.starsRootRect);
            for (int star = 0; star < templateStarRects.Length; star++)
            {
                CopyRect(templateStarRects[star], row.starRects[star]);
            }
        }
    }

    private void ApplyStarLayout()
    {
        float effectiveStarStep = Mathf.Min(starStep, 44f);
        for (int i = 0; i < rows.Count; i++)
        {
            ReviewRow row = rows[i];
            if (row == null || row.starsRootRect == null)
            {
                continue;
            }

            int count = row.starRects.Length;
            float totalWidth = starSize.x + (effectiveStarStep * (count - 1));
            row.starsRootRect.sizeDelta = new Vector2(totalWidth, starSize.y);

            float startX = -0.5f * totalWidth + 0.5f * starSize.x;
            for (int star = 0; star < count; star++)
            {
                RectTransform rect = row.starRects[star];
                if (rect == null)
                {
                    continue;
                }

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = starSize;
                rect.anchoredPosition = new Vector2(startX + (effectiveStarStep * star), 0f);
            }
        }
    }

    private List<ReviewEntry> BuildSourceReviews()
    {
        List<ReviewEntry> result = new List<ReviewEntry>();
        IReadOnlyList<GymEconomyManager.CustomerReview> runtime = economyManager != null ? economyManager.GetRecentReviews() : System.Array.Empty<GymEconomyManager.CustomerReview>();

        if (runtime != null && runtime.Count > 0)
        {
            for (int i = 0; i < runtime.Count; i++)
            {
                var review = runtime[i];
                result.Add(new ReviewEntry(
                    string.IsNullOrWhiteSpace(review.authorName) ? "회원" : review.authorName,
                    string.IsNullOrWhiteSpace(review.text) ? "후기 내용이 비어 있습니다" : review.text,
                    review.stars,
                    i
                ));
            }
            return result;
        }

        return result;
    }

    private void RefreshSummary(List<ReviewEntry> source)
    {
        if (source == null || source.Count <= 0)
        {
            SetText(satisfactionValue, "-");
            SetText(newCountValue, "0건");
            SetText(recommendValue, "0%");
            return;
        }

        float sum = 0f;
        int positiveCount = 0;
        for (int i = 0; i < source.Count; i++)
        {
            sum += source[i].stars;
            if (source[i].stars >= 4f)
            {
                positiveCount++;
            }
        }

        float avg = sum / source.Count;
        SetText(satisfactionValue, avg >= 4f ? "좋음" : avg >= 3f ? "보통" : "주의");
        SetText(newCountValue, $"{Mathf.Min(2, source.Count)}건");
        SetText(recommendValue, $"{Mathf.RoundToInt(positiveCount / (float)source.Count * 100f)}%");
    }

    private List<ReviewEntry> ApplyFilterAndSort(List<ReviewEntry> source)
    {
        List<ReviewEntry> filtered = new List<ReviewEntry>();
        for (int i = 0; i < source.Count; i++)
        {
            bool include = currentFilter switch
            {
                ReviewFilterMode.Positive => source[i].stars >= 3f,
                ReviewFilterMode.Negative => source[i].stars <= 2f,
                _ => true
            };

            if (include)
            {
                filtered.Add(source[i]);
            }
        }

        filtered.Sort((a, b) => latestFirst ? b.order.CompareTo(a.order) : a.order.CompareTo(b.order));
        return filtered;
    }

    private void DisplayRows(List<ReviewEntry> visible, bool hasAnyReviews)
    {
        if (!hasAnyReviews)
        {
            HideAllRows();
            SetEmptyStateVisible(true, EmptyReviewMessage);
            return;
        }

        SetEmptyStateVisible(false, string.Empty);
        if (visible == null || visible.Count <= 0)
        {
            HideAllRows();
            SetEmptyStateVisible(true, "조건에 맞는 리뷰가 없습니다.");
            return;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            ReviewRow row = rows[i];
            if (row == null || row.root == null)
            {
                continue;
            }

            if (i >= visible.Count)
            {
                row.root.SetActive(false);
                continue;
            }

            row.root.SetActive(true);
            ApplyRuntimeUserNameStyle(row.userName, row.userNameRect);
            ApplyRuntimeCommentStyle(row.comment, row.commentRect);
            SetMood(row, visible[i].stars);
            SetText(row.userName, visible[i].authorName);
            SetText(row.comment, visible[i].text);
            SetStars(row, visible[i].stars);
        }
    }

    private void HideAllRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null && rows[i].root != null)
            {
                rows[i].root.SetActive(false);
            }
        }
    }

    private void SetEmptyStateVisible(bool visible, string message)
    {
        ResolveEmptyStateText();
        if (emptyStateText == null)
        {
            return;
        }

        emptyStateText.text = message;
        emptyStateText.gameObject.SetActive(visible);
    }

    private void EnsureRowSlotCount(int count)
    {
        while (rows.Count < count)
        {
            rows.Add(new ReviewRow());
        }
    }

    private void EnsureRuntimeRowCapacity(int visibleCount)
    {
        int targetCount = Mathf.Max(MinimumRowCount, visibleCount);
        EnsureRowSlotCount(targetCount);

        for (int i = 0; i < targetCount; i++)
        {
            ResolveRow(i, Application.isPlaying);
        }
    }

    private Transform CreateRuntimeRow(int index)
    {
        if (!Application.isPlaying || index < MinimumRowCount)
        {
            return null;
        }

        Transform template = FindTemplateRow();
        if (template == null || template.parent == null)
        {
            return null;
        }

        GameObject clone = Instantiate(template.gameObject, template.parent);
        clone.name = BuildIndexedName(template.name, index) ?? "ReviewRow_" + index;
        clone.SetActive(false);

        return clone.transform;
    }

    private void RefreshScrollContentBounds(int visibleCount)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveScroll();
        if (listContentRect == null)
        {
            return;
        }

        int activeRows = Mathf.Max(1, visibleCount);
        float rowHeight = GetTemplateRowHeight();
        float contentHeight = Mathf.Abs(GetFirstRowY()) + (rowSpacingY * Mathf.Max(0, activeRows - 1)) + rowHeight;
        float viewportHeight = listScrollRect != null && listScrollRect.viewport != null
            ? listScrollRect.viewport.rect.height
            : 0f;

        contentHeight = Mathf.Max(contentHeight, viewportHeight);

        listContentRect.anchorMin = new Vector2(0f, 1f);
        listContentRect.anchorMax = new Vector2(1f, 1f);
        listContentRect.pivot = new Vector2(0.5f, 1f);
        listContentRect.sizeDelta = new Vector2(listContentRect.sizeDelta.x, contentHeight);

        if (listScrollRect != null)
        {
            listScrollRect.content = listContentRect;
            listScrollRect.horizontal = false;
            listScrollRect.vertical = true;
            listScrollRect.viewport = listViewportRect != null ? listViewportRect : listScrollRect.viewport;
        }
    }

    private void AdjustRuntimeListViewport()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveScroll();
        if (listViewportRect == null)
        {
            return;
        }

        RectTransform parent = listViewportRect.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        bool foundSummary = TryGetLocalBoundsY(parent, new[]
        {
            "ReviewSatisfactionSummaryCard",
            "ReviewNewCountSummaryCard",
            "ReviewRecommendSummaryCard"
        }, out float summaryBottomY, out _);

        bool foundFilters = TryGetLocalBoundsY(parent, new[]
        {
            "AllFilterButton",
            "PositiveFilterButton",
            "NegativeFilterButton",
            "SortFilterButton"
        }, out _, out float filterTopY);

        if (!foundSummary && !foundFilters)
        {
            return;
        }

        listViewportRect.anchorMin = new Vector2(listViewportRect.anchorMin.x, 0f);
        listViewportRect.anchorMax = new Vector2(listViewportRect.anchorMax.x, 1f);
        listViewportRect.pivot = new Vector2(listViewportRect.pivot.x, 0.5f);

        Vector2 offsetMin = listViewportRect.offsetMin;
        Vector2 offsetMax = listViewportRect.offsetMax;

        if (foundSummary)
        {
            float parentTopY = parent.rect.height * (1f - parent.pivot.y);
            float desiredTopY = summaryBottomY + RuntimeSummaryCardVisibleBottomInset - RuntimeViewportTopGap;
            offsetMax.y = -(parentTopY - desiredTopY);
        }

        if (foundFilters)
        {
            float parentBottomY = -parent.rect.height * parent.pivot.y;
            float desiredBottomY = filterTopY + RuntimeViewportBottomGap;
            offsetMin.y = desiredBottomY - parentBottomY;
        }

        if (parent.rect.height + offsetMax.y - offsetMin.y < rowSpacingY)
        {
            return;
        }

        listViewportRect.offsetMin = offsetMin;
        listViewportRect.offsetMax = offsetMax;
    }

    private float GetTemplateRowHeight()
    {
        Transform template = FindTemplateRow();
        RectTransform rect = template as RectTransform;
        if (rect != null && rect.sizeDelta.y > 0f)
        {
            return rect.sizeDelta.y;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null && rows[i].rect != null && rows[i].rect.sizeDelta.y > 0f)
            {
                return rows[i].rect.sizeDelta.y;
            }
        }

        return rowSpacingY;
    }

    private void ResetScrollToTop()
    {
        if (listScrollRect == null)
        {
            ResolveScroll();
        }

        if (listScrollRect != null)
        {
            listScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void CopyRootRectWithSpacing(RectTransform source, RectTransform target, int index)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = new Vector2(source.anchoredPosition.x, GetFirstRowY() - (rowSpacingY * index));
    }

    private float GetFirstRowY()
    {
        return Application.isPlaying ? RuntimeFirstRowY : firstRowY;
    }

    private Transform FindTemplateRow()
    {
        Transform template = FindDeepChild(transform, templateRowName);
        if (template != null)
        {
            return template;
        }

        template = FindDeepChild(transform, "ReviewRow_0");
        if (template != null)
        {
            return template;
        }

        return FindDeepChild(transform, "ReviewListItem_0");
    }

    private Transform FindRowTransform(int index)
    {
        Transform row = FindDeepChild(transform, BuildIndexedName(templateRowName, index));
        if (row != null)
        {
            return row;
        }

        row = FindDeepChild(transform, "ReviewRow_" + index);
        if (row != null)
        {
            return row;
        }

        return FindDeepChild(transform, "ReviewListItem_" + index);
    }

    private static string BuildIndexedName(string templateName, int index)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            return null;
        }

        int digitStart = templateName.Length;
        while (digitStart > 0 && char.IsDigit(templateName[digitStart - 1]))
        {
            digitStart--;
        }

        if (digitStart == templateName.Length)
        {
            return index == 0 ? templateName : null;
        }

        return templateName.Substring(0, digitStart) + index;
    }

    private static void CopyRect(RectTransform source, RectTransform target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
    }

    private static void CopyTextStyle(Text source, Text target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.font = source.font;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.lineSpacing = source.lineSpacing;
        target.supportRichText = source.supportRichText;
        target.alignment = source.alignment;
        target.alignByGeometry = source.alignByGeometry;
        target.horizontalOverflow = source.horizontalOverflow;
        target.verticalOverflow = source.verticalOverflow;
        target.resizeTextForBestFit = source.resizeTextForBestFit;
        target.resizeTextMinSize = source.resizeTextMinSize;
        target.resizeTextMaxSize = source.resizeTextMaxSize;
        target.color = source.color;
    }

    private static void ApplyRuntimeCommentStyle(Text comment, RectTransform commentRect)
    {
        if (!Application.isPlaying || comment == null)
        {
            return;
        }

        comment.fontSize = RuntimeCommentFontSize;
        comment.resizeTextForBestFit = false;
        comment.alignment = TextAnchor.MiddleLeft;
        comment.horizontalOverflow = HorizontalWrapMode.Wrap;
        comment.verticalOverflow = VerticalWrapMode.Truncate;

        if (commentRect != null && commentRect.sizeDelta.y < RuntimeCommentMinHeight)
        {
            commentRect.sizeDelta = new Vector2(commentRect.sizeDelta.x, RuntimeCommentMinHeight);
        }

        if (commentRect != null)
        {
            commentRect.anchoredPosition = new Vector2(RuntimeCommentX, commentRect.anchoredPosition.y);
            commentRect.sizeDelta = new Vector2(RuntimeCommentWidth, Mathf.Max(commentRect.sizeDelta.y, RuntimeCommentMinHeight));
        }
    }

    private static void ApplyRuntimeUserNameStyle(Text userName, RectTransform userNameRect)
    {
        if (!Application.isPlaying || userName == null)
        {
            return;
        }

        userName.alignment = TextAnchor.MiddleLeft;
        userName.resizeTextForBestFit = true;
        userName.resizeTextMinSize = 18;
        userName.resizeTextMaxSize = Mathf.Max(22, userName.fontSize);
        userName.horizontalOverflow = HorizontalWrapMode.Wrap;
        userName.verticalOverflow = VerticalWrapMode.Truncate;

        if (userNameRect != null)
        {
            userNameRect.anchoredPosition = new Vector2(RuntimeUserNameX, userNameRect.anchoredPosition.y);
            userNameRect.sizeDelta = new Vector2(RuntimeUserNameWidth, Mathf.Max(userNameRect.sizeDelta.y, 36f));
        }
    }

    private static void SetMood(ReviewRow row, float stars)
    {
        if (row.moodIcon == null)
        {
            return;
        }

        string spriteName = stars >= 4f ? "icon_mood_good" : stars >= 3f ? "icon_mood_normal" : "icon_mood_bad";
        GeneratedRuntimeSprites.Assign(row.moodIcon, ReviewAssetPath + spriteName, true);
    }

    private static void SetStars(ReviewRow row, float stars)
    {
        int filled = Mathf.Clamp(Mathf.RoundToInt(stars), 0, row.stars.Length);
        for (int i = 0; i < row.stars.Length; i++)
        {
            if (row.stars[i] == null)
            {
                continue;
            }

            GeneratedRuntimeSprites.Assign(row.stars[i], ReviewAssetPath + (i < filled ? "icon_star_full" : "icon_star_empty"), true);
        }
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private Text FindText(string childName, string ancestorName)
    {
        Transform ancestor = FindDeepChild(transform, ancestorName);
        return ancestor != null ? GetText(ancestor, childName) : null;
    }

    private static Image GetImage(Transform root, string childName)
    {
        Transform child = FindDeepChild(root, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static Text GetText(Transform root, string childName)
    {
        Transform child = FindDeepChild(root, childName);
        return child != null ? child.GetComponent<Text>() : null;
    }

    private static bool TryGetLocalBoundsY(RectTransform parent, string[] childNames, out float minY, out float maxY)
    {
        minY = float.MaxValue;
        maxY = float.MinValue;
        bool found = false;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < childNames.Length; i++)
        {
            Transform child = FindDeepChild(parent, childNames[i]);
            RectTransform rect = child as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy)
            {
                continue;
            }

            rect.GetWorldCorners(corners);
            for (int corner = 0; corner < corners.Length; corner++)
            {
                float localY = parent.InverseTransformPoint(corners[corner]).y;
                minY = Mathf.Min(minY, localY);
                maxY = Mathf.Max(maxY, localY);
            }

            found = true;
        }

        if (!found)
        {
            minY = 0f;
            maxY = 0f;
        }

        return found;
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
