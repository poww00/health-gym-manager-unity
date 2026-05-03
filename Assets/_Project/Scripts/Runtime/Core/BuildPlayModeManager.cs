using System;
using UnityEngine;

/// <summary>
/// [프로토타입/MVP]
/// 플레이 모드 / 설치 모드 전환 상태를 전역으로 들고 있는 간단한 상태 관리자.
///
/// 기본값은 플레이 모드로 시작한다.
/// - 플레이 모드: 배치 프리뷰/이동/철거/설치 비활성화
/// - 설치 모드: 배치 프리뷰/설치/이동/철거 활성화
/// </summary>
public static class BuildPlayModeManager
{
    private const string PlayerPrefsKey = "GYM_BUILD_PLAY_MODE";
    private const int PlayModeValue = 0;
    private const int BuildModeValue = 1;

    private static bool isInitialized;
    private static bool isBuildMode;

    public static event Action<bool> ModeChanged;

    public static bool IsBuildMode
    {
        get
        {
            EnsureInitialized();
            return isBuildMode;
        }
    }

    public static bool IsPlayMode => !IsBuildMode;

    public static void EnterBuildMode()
    {
        SetBuildMode(true);
    }

    public static void EnterPlayMode()
    {
        SetBuildMode(false);
    }

    public static void ToggleMode()
    {
        SetBuildMode(!IsBuildMode);
    }

    public static void SetBuildMode(bool buildMode)
    {
        EnsureInitialized();

        if (isBuildMode == buildMode)
        {
            return;
        }

        isBuildMode = buildMode;
        PlayerPrefs.SetInt(PlayerPrefsKey, isBuildMode ? BuildModeValue : PlayModeValue);
        PlayerPrefs.Save();

        ModeChanged?.Invoke(isBuildMode);

        Debug.Log($"[BuildPlayModeManager] 모드 전환: {(isBuildMode ? "설치 모드" : "플레이 모드")}");
    }

    private static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        isBuildMode = PlayerPrefs.GetInt(PlayerPrefsKey, PlayModeValue) == BuildModeValue;
    }
}
