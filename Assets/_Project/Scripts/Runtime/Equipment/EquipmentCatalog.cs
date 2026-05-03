using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// [프로토타입/MVP]
/// 하단 운영 패널 안에서 기구를 분류형으로 선택하는 임시 UI.
///
/// 중요한 점:
/// - 정식 Canvas 상점 UI 아님
/// - 현재는 "배치할 기구 선택"만 담당
/// - push / pull / leg는 아직 SO 정식 필드가 아니라
///   equipmentId / displayName 기반의 임시 분류
/// </summary>
public sealed class EquipmentCatalog : MonoBehaviour
{
    private enum PrototypeUiGroup
    {
        Cardio,
        Push,
        Pull,
        Legs,
        Recovery,
        Other
    }

    [Header("Definitions")]
    [SerializeField] private List<EquipmentDefinition> definitions = new List<EquipmentDefinition>();
    [SerializeField] private EquipmentDefinition defaultDefinition;

    [Header("Prototype Input")]
    [SerializeField] private bool selectDefaultOnAwake = true;
    [SerializeField] private bool enableNumberHotkeys = true;
    [SerializeField] private bool enableCycleKey = true;
    [SerializeField] private KeyCode legacyCycleNextKey = KeyCode.Tab;

    [Header("Bottom HUD Integration")]
    [SerializeField] private bool useBottomHudHost = true;
    [SerializeField] private int maxVisibleGroupTabs = 4;
    [SerializeField] private int maxVisibleEquipmentButtonsPortrait = 3;
    [SerializeField] private int maxVisibleEquipmentButtonsLandscape = 4;

    [Header("Debug")]
    [SerializeField] private bool showDebugOnGUI = false;
    [SerializeField] private bool logSelection = true;

    private GUIStyle toolbarBoxStyle;
    private GUIStyle groupTabStyle;
    private GUIStyle equipmentButtonStyle;
    private GUIStyle smallArrowStyle;
    private GUIStyle debugLabelStyle;

    private PrototypeUiGroup selectedGroup = PrototypeUiGroup.Cardio;
    private int groupPageStartIndex = 0;
    private int equipmentPageStartIndex = 0;

    private readonly List<PrototypeUiGroup> cachedPopulatedGroups = new List<PrototypeUiGroup>();
    private readonly List<EquipmentDefinition> cachedVisibleDefinitions = new List<EquipmentDefinition>();

    public IReadOnlyList<EquipmentDefinition> Definitions => definitions;

    private void Awake()
    {
        if (!selectDefaultOnAwake)
        {
            EnsureValidSelectedGroup();
            return;
        }

        EquipmentDefinition initial = defaultDefinition != null ? defaultDefinition : GetFirstValidDefinition();
        if (initial != null)
        {
            EquipmentSelectionState.Select(initial);
            SyncSelectedGroupWithDefinition(initial);

            if (logSelection)
            {
                Debug.Log($"[EquipmentCatalog] 기본 선택: [{initial.BrandTierLabel}] {initial.DisplayName}");
            }
        }
        else
        {
            EnsureValidSelectedGroup();
        }
    }

    private void Update()
    {
        EnsureValidSelectedGroup();

        if (enableNumberHotkeys)
        {
            if (WasSelectKeyPressed(0)) SelectByIndex(0);
            if (WasSelectKeyPressed(1)) SelectByIndex(1);
            if (WasSelectKeyPressed(2)) SelectByIndex(2);
            if (WasSelectKeyPressed(3)) SelectByIndex(3);
            if (WasSelectKeyPressed(4)) SelectByIndex(4);
        }

        if (enableCycleKey && WasCycleKeyPressed())
        {
            SelectNext();
        }
    }

    private void OnGUI()
    {
        // [UGUI 도입] 기존 OnGUI 카탈로그 창은 더 이상 그리지 않습니다.
        return;

        if (!showDebugOnGUI)
        {
            return;
        }

        if (useBottomHudHost)
        {
            return;
        }

        DrawDebugPanel();
    }

    public void DrawBottomHudContent(Rect contentRect)
    {
        EnsureStyles();
        EnsureValidSelectedGroup();

        List<PrototypeUiGroup> groups = GetPopulatedGroups();
        if (groups.Count <= 0)
        {
            GUI.Box(contentRect, GUIContent.none, toolbarBoxStyle);
            GUI.Label(contentRect, "표시할 기구가 없어.", debugLabelStyle);
            return;
        }

        List<EquipmentDefinition> visibleDefinitions = GetDefinitionsForGroup(selectedGroup);

        GUI.Box(contentRect, GUIContent.none, toolbarBoxStyle);
        GUI.BeginGroup(contentRect);

        float localWidth = contentRect.width;
        float localHeight = contentRect.height;

        float margin = 8f;
        float tabRowHeight = 26f;

        DrawGroupTabsLocal(new Rect(margin, margin, localWidth - (margin * 2f), tabRowHeight), groups);
        DrawEquipmentButtonsLocal(
            new Rect(margin, margin + tabRowHeight + 8f, localWidth - (margin * 2f), localHeight - tabRowHeight - 16f),
            visibleDefinitions
        );

        GUI.EndGroup();
    }

    private void DrawGroupTabsLocal(Rect rect, List<PrototypeUiGroup> groups)
    {
        int safeMaxVisibleGroupTabs = Mathf.Max(1, maxVisibleGroupTabs);
        groupPageStartIndex = Mathf.Clamp(groupPageStartIndex, 0, Mathf.Max(0, groups.Count - 1));

        float gap = 4f;
        bool showPager = groups.Count > safeMaxVisibleGroupTabs;
        float arrowWidth = showPager ? 22f : 0f;

        if (showPager)
        {
            Rect prevRect = new Rect(rect.x, rect.y, arrowWidth, rect.height);
            GUI.enabled = groupPageStartIndex > 0;
            if (GUI.Button(prevRect, "<", smallArrowStyle))
            {
                groupPageStartIndex = Mathf.Max(0, groupPageStartIndex - safeMaxVisibleGroupTabs);
            }
            GUI.enabled = true;
        }

        float tabsX = rect.x + (showPager ? arrowWidth + gap : 0f);
        float tabsWidth = rect.width - (showPager ? ((arrowWidth * 2f) + (gap * 2f)) : 0f);

        int remainingGroupCount = groups.Count - groupPageStartIndex;
        int visibleTabCount = Mathf.Min(safeMaxVisibleGroupTabs, remainingGroupCount);
        visibleTabCount = Mathf.Max(1, visibleTabCount);

        float tabWidth = (tabsWidth - (gap * (visibleTabCount - 1))) / visibleTabCount;
        Color originalBackgroundColor = GUI.backgroundColor;

        for (int i = 0; i < visibleTabCount; i++)
        {
            PrototypeUiGroup group = groups[groupPageStartIndex + i];
            Rect tabRect = new Rect(
                tabsX + (tabWidth + gap) * i,
                rect.y,
                tabWidth,
                rect.height
            );

            bool isSelected = selectedGroup == group;
            GUI.backgroundColor = isSelected
                ? new Color(0.28f, 0.75f, 1f, 1f)
                : Color.white;

            if (GUI.Button(tabRect, GetGroupDisplayName(group), groupTabStyle))
            {
                selectedGroup = group;
                equipmentPageStartIndex = 0;
            }
        }

        GUI.backgroundColor = originalBackgroundColor;

        if (showPager)
        {
            Rect nextRect = new Rect(rect.xMax - arrowWidth, rect.y, arrowWidth, rect.height);
            GUI.enabled = groupPageStartIndex + safeMaxVisibleGroupTabs < groups.Count;
            if (GUI.Button(nextRect, ">", smallArrowStyle))
            {
                groupPageStartIndex = Mathf.Min(groups.Count - 1, groupPageStartIndex + safeMaxVisibleGroupTabs);
            }
            GUI.enabled = true;
        }
    }

    private void DrawEquipmentButtonsLocal(Rect rect, List<EquipmentDefinition> visibleDefinitions)
    {
        if (visibleDefinitions.Count <= 0)
        {
            GUI.Label(rect, "이 분류에는 기구가 아직 없어.", debugLabelStyle);
            return;
        }

        bool isPortrait = Screen.height > Screen.width;
        int maxVisibleButtons = isPortrait
            ? Mathf.Max(1, maxVisibleEquipmentButtonsPortrait)
            : Mathf.Max(1, maxVisibleEquipmentButtonsLandscape);

        equipmentPageStartIndex = Mathf.Clamp(equipmentPageStartIndex, 0, Mathf.Max(0, visibleDefinitions.Count - 1));

        float gap = 6f;
        bool showPager = visibleDefinitions.Count > maxVisibleButtons;
        float arrowWidth = showPager ? 22f : 0f;

        if (showPager)
        {
            Rect prevRect = new Rect(rect.x, rect.y + 8f, arrowWidth, rect.height - 16f);
            GUI.enabled = equipmentPageStartIndex > 0;
            if (GUI.Button(prevRect, "<", smallArrowStyle))
            {
                equipmentPageStartIndex = Mathf.Max(0, equipmentPageStartIndex - maxVisibleButtons);
            }
            GUI.enabled = true;
        }

        float buttonsX = rect.x + (showPager ? arrowWidth + gap : 0f);
        float buttonsWidth = rect.width - (showPager ? ((arrowWidth * 2f) + (gap * 2f)) : 0f);

        int remainingCount = visibleDefinitions.Count - equipmentPageStartIndex;
        int visibleButtonCount = Mathf.Min(maxVisibleButtons, remainingCount);
        visibleButtonCount = Mathf.Max(1, visibleButtonCount);

        float buttonWidth = (buttonsWidth - (gap * (visibleButtonCount - 1))) / visibleButtonCount;
        Color originalBackgroundColor = GUI.backgroundColor;

        for (int i = 0; i < visibleButtonCount; i++)
        {
            EquipmentDefinition definition = visibleDefinitions[equipmentPageStartIndex + i];
            Rect buttonRect = new Rect(
                buttonsX + (buttonWidth + gap) * i,
                rect.y,
                buttonWidth,
                rect.height
            );

            bool isSelected = EquipmentSelectionState.CurrentDefinition == definition;
            GUI.backgroundColor = isSelected
                ? definition.DebugColor
                : Color.white;

            string buttonText =
                $"[{definition.BrandTierLabel}] {definition.DisplayName}\n" +
                $"{definition.Width}x{definition.Height}\n" +
                $"{definition.InstallCost:N0}";

            if (GUI.Button(buttonRect, buttonText, equipmentButtonStyle))
            {
                SelectDefinition(definition);
            }
        }

        GUI.backgroundColor = originalBackgroundColor;

        if (showPager)
        {
            Rect nextRect = new Rect(rect.xMax - arrowWidth, rect.y + 8f, arrowWidth, rect.height - 16f);
            GUI.enabled = equipmentPageStartIndex + maxVisibleButtons < visibleDefinitions.Count;
            if (GUI.Button(nextRect, ">", smallArrowStyle))
            {
                equipmentPageStartIndex = Mathf.Min(visibleDefinitions.Count - 1, equipmentPageStartIndex + maxVisibleButtons);
            }
            GUI.enabled = true;
        }
    }

    private void DrawDebugPanel()
    {
        EnsureStyles();

        float x = 10f;
        float y = 248f;
        float w = 230f;
        float h = 126f;

        GUI.Box(new Rect(x, y, w, h), "기구 선택 [디버그]");
        GUILayout.BeginArea(new Rect(x + 10f, y + 24f, w - 20f, h - 30f));

        GUILayout.Label($"현재 그룹: {GetGroupDisplayName(selectedGroup)}", debugLabelStyle);
        GUILayout.Label($"현재 선택: {EquipmentSelectionState.GetCurrentName()}", debugLabelStyle);

        List<EquipmentDefinition> defs = GetDefinitionsForGroup(selectedGroup);
        for (int i = 0; i < defs.Count && i < 4; i++)
        {
            EquipmentDefinition def = defs[i];
            if (def == null)
            {
                continue;
            }

            GUILayout.Label(
                $"- [{def.BrandTierLabel}] {def.DisplayName} ({def.Width}x{def.Height}, {def.InstallCost:N0})",
                debugLabelStyle
            );
        }

        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (toolbarBoxStyle == null)
        {
            toolbarBoxStyle = new GUIStyle(GUI.skin.box);
            toolbarBoxStyle.padding = new RectOffset(8, 8, 8, 8);
        }

        if (groupTabStyle == null)
        {
            groupTabStyle = new GUIStyle(GUI.skin.button);
            groupTabStyle.alignment = TextAnchor.MiddleCenter;
            groupTabStyle.wordWrap = false;
            groupTabStyle.clipping = TextClipping.Clip;
            groupTabStyle.padding = new RectOffset(4, 4, 4, 4);
        }

        if (equipmentButtonStyle == null)
        {
            equipmentButtonStyle = new GUIStyle(GUI.skin.button);
            equipmentButtonStyle.alignment = TextAnchor.MiddleCenter;
            equipmentButtonStyle.wordWrap = true;
            equipmentButtonStyle.padding = new RectOffset(4, 4, 4, 4);
        }

        if (smallArrowStyle == null)
        {
            smallArrowStyle = new GUIStyle(GUI.skin.button);
            smallArrowStyle.alignment = TextAnchor.MiddleCenter;
            smallArrowStyle.padding = new RectOffset(2, 2, 2, 2);
        }

        if (debugLabelStyle == null)
        {
            debugLabelStyle = new GUIStyle(GUI.skin.label);
            debugLabelStyle.normal.textColor = Color.white;
            debugLabelStyle.wordWrap = true;
        }

        bool isPortrait = Screen.height > Screen.width;

        groupTabStyle.fontSize = isPortrait ? 11 : 10;
        equipmentButtonStyle.fontSize = isPortrait ? 11 : 10;
        smallArrowStyle.fontSize = isPortrait ? 12 : 11;
        debugLabelStyle.fontSize = 13;
    }

    public void SelectByIndex(int index)
    {
        if (index < 0 || index >= definitions.Count)
        {
            return;
        }

        EquipmentDefinition definition = definitions[index];
        if (definition == null)
        {
            return;
        }

        SelectDefinition(definition);
    }

    public void SelectById(string equipmentId)
    {
        EquipmentDefinition definition = GetDefinitionById(equipmentId);
        if (definition == null)
        {
            return;
        }

        SelectDefinition(definition);
    }

    public EquipmentDefinition GetDefinitionById(string equipmentId)
    {
        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            return null;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            EquipmentDefinition def = definitions[i];
            if (def == null)
            {
                continue;
            }

            if (def.EquipmentId == equipmentId)
            {
                return def;
            }
        }

        if (defaultDefinition != null && defaultDefinition.EquipmentId == equipmentId)
        {
            return defaultDefinition;
        }

        return null;
    }

    public EquipmentDefinition GetFirstValidDefinition()
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] != null)
            {
                return definitions[i];
            }
        }

        return null;
    }

    public void SelectNext()
    {
        if (definitions.Count == 0)
        {
            return;
        }

        EquipmentDefinition current = EquipmentSelectionState.CurrentDefinition;
        int currentIndex = -1;

        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] == current)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = currentIndex + 1;
        if (nextIndex >= definitions.Count)
        {
            nextIndex = 0;
        }

        SelectByIndex(nextIndex);
    }

    private void SelectDefinition(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        EquipmentSelectionState.Select(definition);
        SyncSelectedGroupWithDefinition(definition);

        if (logSelection)
        {
            Debug.Log(
                $"[EquipmentCatalog] 선택 변경: [{definition.BrandTierLabel}] {definition.DisplayName} / " +
                $"{GetGroupDisplayName(selectedGroup)} / {definition.Width}x{definition.Height} / 비용 {definition.InstallCost:N0}"
            );
        }
    }

    private void SyncSelectedGroupWithDefinition(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        selectedGroup = GetGroupForDefinition(definition);
        equipmentPageStartIndex = 0;

        List<PrototypeUiGroup> groups = GetPopulatedGroups();
        int index = groups.IndexOf(selectedGroup);
        if (index >= 0)
        {
            int safeTabCount = Mathf.Max(1, maxVisibleGroupTabs);
            groupPageStartIndex = (index / safeTabCount) * safeTabCount;
        }
    }

    private void EnsureValidSelectedGroup()
    {
        List<PrototypeUiGroup> groups = GetPopulatedGroups();
        if (groups.Count <= 0)
        {
            return;
        }

        if (!groups.Contains(selectedGroup))
        {
            selectedGroup = groups[0];
            groupPageStartIndex = 0;
            equipmentPageStartIndex = 0;
        }
    }

    private List<PrototypeUiGroup> GetPopulatedGroups()
    {
        cachedPopulatedGroups.Clear();

        for (int groupIndex = 0; groupIndex < 8; groupIndex++)
        {
            PrototypeUiGroup group = (PrototypeUiGroup)groupIndex;

            for (int i = 0; i < definitions.Count; i++)
            {
                EquipmentDefinition definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (GetGroupForDefinition(definition) == group)
                {
                    cachedPopulatedGroups.Add(group);
                    break;
                }
            }
        }

        return cachedPopulatedGroups;
    }

    private List<EquipmentDefinition> GetDefinitionsForGroup(PrototypeUiGroup group)
    {
        cachedVisibleDefinitions.Clear();

        for (int i = 0; i < definitions.Count; i++)
        {
            EquipmentDefinition definition = definitions[i];
            if (definition == null)
            {
                continue;
            }

            if (GetGroupForDefinition(definition) == group)
            {
                cachedVisibleDefinitions.Add(definition);
            }
        }

        return cachedVisibleDefinitions;
    }

    private PrototypeUiGroup GetGroupForDefinition(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return PrototypeUiGroup.Other;
        }

        string source = $"{definition.EquipmentId} {definition.DisplayName}".ToLowerInvariant();

        if (ContainsAny(source, "treadmill", "bike", "cycle", "ellipt", "cardio", "run", "rower"))
        {
            return PrototypeUiGroup.Cardio;
        }

        if (ContainsAny(source, "bench", "press", "chest", "push", "shoulder", "dip"))
        {
            return PrototypeUiGroup.Push;
        }

        if (ContainsAny(source, "row", "pull", "lat", "chin", "bicep", "curl"))
        {
            return PrototypeUiGroup.Pull;
        }

        if (ContainsAny(source, "squat", "leg", "calf", "lunge", "hack", "glute"))
        {
            return PrototypeUiGroup.Legs;
        }

        if (ContainsAny(source, "stretch", "recovery", "massage", "foam"))
        {
            return PrototypeUiGroup.Recovery;
        }

        if (ContainsAny(source, "locker", "counter", "desk", "facility", "water", "vending"))
        {
            return PrototypeUiGroup.Other;
        }

        switch (definition.Category)
        {
            case EquipmentCategory.Cardio:
                return PrototypeUiGroup.Cardio;

            case EquipmentCategory.Push:
                return PrototypeUiGroup.Push;

            case EquipmentCategory.Pull:
                return PrototypeUiGroup.Pull;

            case EquipmentCategory.Legs:
                return PrototypeUiGroup.Legs;

            case EquipmentCategory.Recovery:
                return PrototypeUiGroup.Recovery;

            default:
                return PrototypeUiGroup.Other;
        }
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(source) || keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (source.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetGroupDisplayName(PrototypeUiGroup group)
    {
        switch (group)
        {
            case PrototypeUiGroup.Cardio: return "카디오";
            case PrototypeUiGroup.Push: return "푸쉬";
            case PrototypeUiGroup.Pull: return "풀";
            case PrototypeUiGroup.Legs: return "하체";
            case PrototypeUiGroup.Recovery: return "회복";
            default: return "기타";
        }
    }

    private bool WasSelectKeyPressed(int zeroBasedIndex)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        switch (zeroBasedIndex)
        {
            case 0: return keyboard.digit1Key.wasPressedThisFrame;
            case 1: return keyboard.digit2Key.wasPressedThisFrame;
            case 2: return keyboard.digit3Key.wasPressedThisFrame;
            case 3: return keyboard.digit4Key.wasPressedThisFrame;
            case 4: return keyboard.digit5Key.wasPressedThisFrame;
            default: return false;
        }
#else
        switch (zeroBasedIndex)
        {
            case 0: return Input.GetKeyDown(KeyCode.Alpha1);
            case 1: return Input.GetKeyDown(KeyCode.Alpha2);
            case 2: return Input.GetKeyDown(KeyCode.Alpha3);
            case 3: return Input.GetKeyDown(KeyCode.Alpha4);
            case 4: return Input.GetKeyDown(KeyCode.Alpha5);
            default: return false;
        }
#endif
    }

    private bool WasCycleKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(legacyCycleNextKey);
#endif
    }
}
