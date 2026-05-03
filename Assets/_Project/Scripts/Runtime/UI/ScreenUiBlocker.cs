using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// [프로토타입/MVP]
/// OnGUI 기반 임시 UI가 월드 입력(그리드 클릭/배치)과 겹칠 때,
/// 해당 화면 영역을 "UI가 점유 중인 영역"으로 등록해 주는 정적 유틸.
///
/// 주의:
/// - 정식 Canvas/EventSystem 대체제가 아님
/// - 현재 프로젝트의 임시 OnGUI UI 충돌 방지용
/// </summary>
public static class ScreenUiBlocker
{
    private sealed class OwnerEntry
    {
        public int lastRegisteredFrame = -9999;
        public readonly List<Rect> rects = new List<Rect>();
    }

    private static readonly Dictionary<int, OwnerEntry> entries = new Dictionary<int, OwnerEntry>();
    private static readonly List<int> staleOwnerIds = new List<int>();

    public static void RegisterRect(int ownerId, Rect rect)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        PruneStaleEntries();

        if (!entries.TryGetValue(ownerId, out OwnerEntry entry))
        {
            entry = new OwnerEntry();
            entries.Add(ownerId, entry);
        }

        int currentFrame = Time.frameCount;

        if (entry.lastRegisteredFrame != currentFrame)
        {
            entry.rects.Clear();
            entry.lastRegisteredFrame = currentFrame;
        }

        entry.rects.Add(rect);
    }

    public static bool IsScreenPositionBlocked(Vector2 screenPosition)
    {
        // UGUI 위에 있으면 무조건 차단 (버튼 클릭 투과 방지)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        PruneStaleEntries();


        Vector2 guiPoint = ConvertToGuiPoint(screenPosition);
        int currentFrame = Time.frameCount;

        foreach (KeyValuePair<int, OwnerEntry> pair in entries)
        {
            OwnerEntry entry = pair.Value;
            if (entry == null)
            {
                continue;
            }

            // Update보다 OnGUI가 늦게 올 수 있어서 이전 프레임 등록도 잠깐 허용
            if (entry.lastRegisteredFrame < currentFrame - 1)
            {
                continue;
            }

            for (int i = 0; i < entry.rects.Count; i++)
            {
                if (entry.rects[i].Contains(guiPoint))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Vector2 ConvertToGuiPoint(Vector2 screenPosition)
    {
        return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
    }

    private static void PruneStaleEntries()
    {
        if (entries.Count == 0)
        {
            return;
        }

        staleOwnerIds.Clear();
        int staleFrameThreshold = Time.frameCount - 2;

        foreach (KeyValuePair<int, OwnerEntry> pair in entries)
        {
            if (pair.Value == null || pair.Value.lastRegisteredFrame < staleFrameThreshold)
            {
                staleOwnerIds.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleOwnerIds.Count; i++)
        {
            entries.Remove(staleOwnerIds[i]);
        }
    }
}