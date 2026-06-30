using System.Collections.Generic;
using UnityEngine;

public class LayeredCustomerAnimator : MonoBehaviour
{
    public CustomerFlowManager.ActiveCustomer customer;

    private SpriteRenderer bodyRenderer;
    private SpriteRenderer headRenderer;
    private GameObject headObject;

    private Sprite[] idleFrames;
    private Sprite[] walkHorizontalFrames;
    private Sprite[] walkVerticalFrames;
    private Sprite[] runFrames;
    private Sprite[] exerciseBikeFrames;
    private Sprite[] benchPressFrames;
    private Sprite[] latPulldownFrames;
    private Sprite[] legPressFrames;
    private Sprite[] dumbbellCurlFrames;
    private Sprite[] dumbbellShoulderPressFrames;
    private Sprite[] yogaOverheadFrames;
    private Sprite[] yogaSideBendFrames;
    private Sprite[] yogaToeTouchFrames;
    private Sprite[] lockerUseFrames;
    private Sprite[] headFrames;

    private Vector3 lastWorldPosition;
    private float animationOffset;
    private int selectedDumbbellRackAnimationIndex = -1;
    private string selectedDumbbellRackMachineKey = string.Empty;
    private int selectedYogaMatAnimationIndex = -1;
    private string selectedYogaMatMachineKey = string.Empty;

    private const float FallbackHeadAnchorInsetY = 0.12f;
    private const float RunHeadAttachmentYOffset = -0.05f;
    private const float RunHeadAttachmentPostFlipXOffset = 0.015f;
    private static readonly Vector3 ExerciseBikeBodyLocalOffset = new Vector3(-0.17f, 0.16f, 0f);
    private const float ExerciseBikeCustomerVisualScale = 0.75f;
    private static readonly Vector2 ExerciseBikeBodyPlacementPivotPixels = new Vector2(83f, 52f);
    private static readonly Vector3 BenchPressBodyLocalOffset = new Vector3(-0.07f, 0.64f, 0f);
    private const float BenchPressCustomerVisualScale = 0.75f;
    private const float BenchPressHeadVisualScale = 0.85f;
    private const float BenchPressHeadRotationZ = 90f;
    private const float BenchPressAnimationFps = 9f;
    private static readonly Vector3 LatPulldownBodyLocalOffset = new Vector3(0.28f, -0.24f, 0f);
    private const float LatPulldownCustomerVisualScale = 0.75f;
    private static readonly Vector3 LegPressBodyLocalOffset = Vector3.zero;
    private const float LegPressCustomerVisualScale = 0.75f;
    private const float LegPressHeadRotationZ = -18f;
    private static readonly Vector3 DumbbellRackBodyLocalOffset = Vector3.zero;
    private const float DumbbellRackCustomerVisualScale = 0.675f;
    private const float DumbbellRackAnimationFps = 6f;
    private static readonly Vector3 YogaMatBodyLocalOffset = new Vector3(0f, 0.45f, 0f);
    private const float YogaMatCustomerVisualScale = 0.675f;
    private static readonly Vector3 YogaToeTouchBodyLocalOffset = YogaMatBodyLocalOffset * 0.9f;
    private const float YogaToeTouchCustomerVisualScale = YogaMatCustomerVisualScale * 0.9f;
    private const float YogaMatAnimationFps = 4f;
    private static readonly Vector3 LockerBodyLocalOffset = new Vector3(0f, 0.20f, 0f);
    private const float LockerCustomerVisualScale = 0.6075f;
    private const float LockerAnimationFps = 4f;
    private const int DefaultBodySortingOrder = 30;
    private const int DefaultHeadSortingOrder = 31;
    private static readonly int[] BenchPressBodyFrameMap = { 2, 1, 0, 4, 1, 2 };
    private static readonly int[] LatPulldownBodyFrameMap = { 0, 1, 5, 2, 3, 2, 5, 1 };
    private static readonly int[] LegPressBodyFrameMap = { 0, 1, 2, 6, 7 };
    private static readonly int[] YogaOverheadBodyFrameMap = { 0, 3, 1, 3 };
    private static readonly Dictionary<string, Vector2> BenchPressHeadAnchorPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_bench_press_2x3_0", new Vector2(21f, 98f) },
        { "body_male_chubby_bench_press_2x3_1", new Vector2(19f, 97f) },
        { "body_male_chubby_bench_press_2x3_2", new Vector2(20f, 94f) },
        { "body_male_chubby_bench_press_2x3_3", new Vector2(22f, 90f) },
        { "body_male_chubby_bench_press_2x3_4", new Vector2(19f, 97f) },
        { "body_male_chubby_bench_press_2x3_5", new Vector2(22f, 99f) },
    };

    private static readonly Vector2 LatPulldownHeadAnchorPixels = new Vector2(78f, 52f);
    private static readonly Vector2 LegPressFallbackHeadAnchorPixels = new Vector2(100f, 44f);

    private static readonly Dictionary<string, Vector2> LegPressHeadAnchorPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_leg_press_4x2_0", new Vector2(91f, 37f) },
        { "body_male_chubby_leg_press_4x2_1", new Vector2(97f, 36f) },
        { "body_male_chubby_leg_press_4x2_2", new Vector2(99f, 40f) },
        { "body_male_chubby_leg_press_4x2_3", new Vector2(98f, 40f) },
        { "body_male_chubby_leg_press_4x2_4", new Vector2(98f, 44f) },
        { "body_male_chubby_leg_press_4x2_5", new Vector2(98f, 40f) },
        { "body_male_chubby_leg_press_4x2_6", new Vector2(95f, 37f) },
        { "body_male_chubby_leg_press_4x2_7", new Vector2(89f, 37f) },
    };

    private static readonly Dictionary<string, Vector2> LegPressBodyFrameOffsetPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_leg_press_4x2_0", new Vector2(0f, 0f) },
        { "body_male_chubby_leg_press_4x2_1", new Vector2(-5f, 0f) },
        { "body_male_chubby_leg_press_4x2_2", new Vector2(-6f, 1f) },
        { "body_male_chubby_leg_press_4x2_6", new Vector2(-4f, 0f) },
        { "body_male_chubby_leg_press_4x2_7", new Vector2(0f, 0f) },
    };

    private static readonly Vector2 DumbbellCurlFallbackHeadAnchorPixels = new Vector2(64f, 13f);
    private static readonly Vector2 DumbbellShoulderPressFallbackHeadAnchorPixels = new Vector2(80f, 55f);

    private static readonly Dictionary<string, Vector2> DumbbellCurlHeadAnchorPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_dumbbell_curl_2x2_0", new Vector2(64f, 13f) },
        { "body_male_chubby_dumbbell_curl_2x2_1", new Vector2(64f, 13f) },
        { "body_male_chubby_dumbbell_curl_2x2_2", new Vector2(64f, 13f) },
        { "body_male_chubby_dumbbell_curl_2x2_3", new Vector2(64f, 13f) },
    };

    private static readonly Dictionary<string, Vector2> DumbbellShoulderPressHeadAnchorPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_dumbbell_shoulder_press_2x2_0", new Vector2(80f, 55f) },
        { "body_male_chubby_dumbbell_shoulder_press_2x2_1", new Vector2(80f, 55f) },
        { "body_male_chubby_dumbbell_shoulder_press_2x2_2", new Vector2(80f, 53f) },
        { "body_male_chubby_dumbbell_shoulder_press_2x2_3", new Vector2(80f, 56f) },
    };

    private static readonly Dictionary<string, Vector2> YogaOverheadHeadAnchorPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_yoga_overhead_2x2_0", new Vector2(80f, 62f) },
        { "body_male_chubby_yoga_overhead_2x2_1", new Vector2(80f, 62f) },
        { "body_male_chubby_yoga_overhead_2x2_2", new Vector2(80f, 62f) },
        { "body_male_chubby_yoga_overhead_2x2_3", new Vector2(80f, 62f) },
    };

    private static readonly Dictionary<string, Vector2> YogaSideBendHeadAnchorPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_yoga_side_bend_2x2_0", new Vector2(79f, 57f) },
        { "body_male_chubby_yoga_side_bend_2x2_1", new Vector2(72f, 60f) },
        { "body_male_chubby_yoga_side_bend_2x2_2", new Vector2(79f, 57f) },
        { "body_male_chubby_yoga_side_bend_2x2_3", new Vector2(88f, 61f) },
    };

    private static readonly Dictionary<string, Vector2> YogaToeTouchHeadAnchorPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_yoga_toe_touch_2x2_0", new Vector2(44f, 58f) },
        { "body_male_chubby_yoga_toe_touch_2x2_1", new Vector2(65f, 67f) },
        { "body_male_chubby_yoga_toe_touch_2x2_2", new Vector2(61f, 89f) },
        { "body_male_chubby_yoga_toe_touch_2x2_3", new Vector2(59f, 63f) },
    };

    private static readonly Dictionary<string, Vector2> LockerHeadAnchorPixelsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_locker_use_2x2_0", new Vector2(79f, 36f) },
        { "body_male_chubby_locker_use_2x2_1", new Vector2(76f, 36f) },
        { "body_male_chubby_locker_use_2x2_2", new Vector2(82f, 36f) },
        { "body_male_chubby_locker_use_2x2_3", new Vector2(75f, 36f) },
    };

    private static readonly float[] YogaSideBendHeadRotationByFrame = { 0f, -18f, 0f, 18f };
    private static readonly float[] YogaToeTouchHeadRotationByFrame = { 0f, -25f, -65f, -35f };

    private static readonly Dictionary<string, Vector2> BodyHeadAnchorsBySpriteName = new Dictionary<string, Vector2>
    {
        { "body_male_chubby_idle_base_00", new Vector2(-0.0009f, 0.6319f) },
        { "body_male_chubby_idle_base_01", new Vector2(-0.0009f, 0.6319f) },
        { "body_male_chubby_idle_base_02", new Vector2(-0.0009f, 0.6319f) },
        { "body_male_chubby_idle_base_03", new Vector2(-0.0017f, 0.6111f) },
        { "body_male_chubby_idle_base_04", new Vector2(-0.0009f, 0.6163f) },
        { "body_male_chubby_idle_base_05", new Vector2(-0.0009f, 0.6163f) },
        { "body_male_chubby_idle_base_06", new Vector2(-0.0009f, 0.6319f) },
        { "body_male_chubby_idle_base_07", new Vector2(-0.0009f, 0.6319f) },
        { "body_male_chubby_walk_front_32x48_4x2_0", new Vector2(0.0000f, 0.6510f) },
        { "body_male_chubby_walk_front_32x48_4x2_1", new Vector2(-0.0035f, 0.6380f) },
        { "body_male_chubby_walk_front_32x48_4x2_2", new Vector2(-0.0113f, 0.6363f) },
        { "body_male_chubby_walk_front_32x48_4x2_3", new Vector2(-0.0035f, 0.6380f) },
        { "body_male_chubby_walk_front_32x48_4x2_4", new Vector2(0.0009f, 0.6476f) },
        { "body_male_chubby_walk_front_32x48_4x2_5", new Vector2(0.0043f, 0.6467f) },
        { "body_male_chubby_walk_front_32x48_4x2_6", new Vector2(0.0009f, 0.6476f) },
        { "body_male_chubby_walk_front_32x48_4x2_7", new Vector2(-0.0122f, 0.6502f) },
        { "body_male_chubby_walk_up_32x48_4x1_0", new Vector2(0.0000f, 0.6319f) },
        { "body_male_chubby_walk_up_32x48_4x1_1", new Vector2(-0.0017f, 0.6337f) },
        { "body_male_chubby_walk_up_32x48_4x1_2", new Vector2(-0.0009f, 0.6319f) },
        { "body_male_chubby_walk_up_32x48_4x1_3", new Vector2(-0.0104f, 0.6363f) },
    };

    public void Initialize(CustomerFlowManager.ActiveCustomer activeCustomer)
    {
        customer = activeCustomer;

        // 기존 렌더러 숨기기
        if (customer.renderer != null)
        {
            customer.renderer.enabled = false;
        }

        // 몸통 오브젝트 및 렌더러 생성
        GameObject bodyObject = new GameObject("Body");
        bodyObject.transform.SetParent(transform, false);
        bodyObject.transform.localPosition = Vector3.zero;
        bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
        bodyRenderer.sortingOrder = 30; // Customer 기본 SortingOrder

        // 머리 오브젝트 및 렌더러 생성
        headObject = new GameObject("Head");
        headObject.transform.SetParent(transform, false);
        headObject.transform.localPosition = new Vector3(0, 0.68f, 0);
        headRenderer = headObject.AddComponent<SpriteRenderer>();
        headRenderer.sortingOrder = 31; // 몸통보다 위에 렌더링

        // 스프라이트 로드
        idleFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_idle_base_32x48_4x2");
        walkHorizontalFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_walk_front_32x48_4x2");
        walkVerticalFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_walk_up_32x48_4x1");
        runFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_run_32x48_4x2");
        exerciseBikeFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_exercise_bike_4x2");
        benchPressFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_bench_press_2x3");
        latPulldownFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_lat_pulldown_4x2");
        legPressFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_leg_press_4x2");
        dumbbellCurlFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_dumbbell_curl_2x2");
        dumbbellShoulderPressFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_dumbbell_shoulder_press_2x2");
        yogaOverheadFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_yoga_overhead_2x2");
        yogaSideBendFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_yoga_side_bend_2x2");
        yogaToeTouchFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_yoga_toe_touch_2x2");
        lockerUseFrames = LoadBodyAnimationFrames("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_locker_use_2x2");
        headFrames = Resources.LoadAll<Sprite>("GeneratedRuntimeUI/characters/customer/head/male_chubby/head_male_chubby_3dir_32x48_3x1");

        animationOffset = Random.Range(0f, 1f);
        if (customer != null)
        {
            lastWorldPosition = customer.worldPosition;
        }
    }

    private static Sprite[] LoadBodyAnimationFrames(string resourcePath)
    {
        Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
        if (frames == null || frames.Length <= 1)
        {
            return frames;
        }

        System.Array.Sort(frames, CompareSpritesByName);
        return frames;
    }

    private static int CompareSpritesByName(Sprite left, Sprite right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    private static Vector2 GetHeadAnchorForBodySprite(Sprite bodySprite, bool bodyFlipX)
    {
        if (bodySprite == null)
        {
            return Vector2.zero;
        }

        bool usesRunHeadAttachment = TryGetRunHeadAttachmentAnchor(bodySprite, out Vector2 anchor);

        if (IsExerciseBikeBodySprite(bodySprite))
        {
            return Vector2.zero;
        }

        if (IsBenchPressBodySprite(bodySprite))
        {
            anchor = GetBenchPressHeadAnchor(bodySprite);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (IsLatPulldownBodySprite(bodySprite))
        {
            anchor = GetLatPulldownHeadAnchor(bodySprite);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (IsLegPressBodySprite(bodySprite))
        {
            anchor = GetLegPressHeadAnchor(bodySprite);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (IsDumbbellCurlBodySprite(bodySprite))
        {
            anchor = GetDumbbellCurlHeadAnchor(bodySprite);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (IsDumbbellShoulderPressBodySprite(bodySprite))
        {
            anchor = GetDumbbellShoulderPressHeadAnchor(bodySprite);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (IsYogaOverheadBodySprite(bodySprite))
        {
            anchor = GetYogaHeadAnchor(bodySprite, YogaOverheadHeadAnchorPixelsBySpriteName);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (IsYogaSideBendBodySprite(bodySprite))
        {
            anchor = GetYogaHeadAnchor(bodySprite, YogaSideBendHeadAnchorPixelsBySpriteName);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (IsYogaToeTouchBodySprite(bodySprite))
        {
            anchor = GetYogaHeadAnchor(bodySprite, YogaToeTouchHeadAnchorPixelsBySpriteName);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (IsLockerUseBodySprite(bodySprite))
        {
            anchor = GetLockerHeadAnchor(bodySprite);
            if (bodyFlipX)
            {
                anchor.x = -anchor.x;
            }

            return anchor;
        }

        if (!usesRunHeadAttachment &&
            !BodyHeadAnchorsBySpriteName.TryGetValue(bodySprite.name, out anchor))
        {
            anchor = new Vector2(0f, bodySprite.bounds.max.y - FallbackHeadAnchorInsetY);
        }

        if (bodyFlipX)
        {
            anchor.x = -anchor.x;
        }

        if (usesRunHeadAttachment)
        {
            anchor.x += RunHeadAttachmentPostFlipXOffset;
        }

        return anchor;
    }

    private static bool IsRunBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_run_32x48_4x2", System.StringComparison.Ordinal);
    }

    private static bool IsExerciseBikeBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_exercise_bike_4x2", System.StringComparison.Ordinal);
    }

    private static bool IsBenchPressBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_bench_press_2x3", System.StringComparison.Ordinal);
    }

    private static bool IsLatPulldownBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_lat_pulldown_4x2", System.StringComparison.Ordinal);
    }

    private static bool IsLegPressBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_leg_press_4x2", System.StringComparison.Ordinal);
    }

    private static bool IsDumbbellCurlBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_dumbbell_curl_2x2", System.StringComparison.Ordinal);
    }

    private static bool IsDumbbellShoulderPressBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_dumbbell_shoulder_press_2x2", System.StringComparison.Ordinal);
    }

    private static bool IsYogaOverheadBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_yoga_overhead_2x2", System.StringComparison.Ordinal);
    }

    private static bool IsYogaSideBendBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_yoga_side_bend_2x2", System.StringComparison.Ordinal);
    }

    private static bool IsYogaToeTouchBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_yoga_toe_touch_2x2", System.StringComparison.Ordinal);
    }

    private static bool IsLockerUseBodySprite(Sprite bodySprite)
    {
        return bodySprite != null &&
            bodySprite.name.StartsWith("body_male_chubby_locker_use_2x2", System.StringComparison.Ordinal);
    }

    private static Vector2 GetBenchPressHeadAnchor(Sprite bodySprite)
    {
        if (bodySprite != null &&
            BenchPressHeadAnchorPixelsBySpriteName.TryGetValue(bodySprite.name, out Vector2 topLeftPixels))
        {
            return SpriteTopLeftPixelsToLocal(bodySprite, topLeftPixels);
        }

        return bodySprite != null
            ? SpriteTopLeftPixelsToLocal(bodySprite, new Vector2(20f, 97f))
            : Vector2.zero;
    }

    private static Vector2 GetLatPulldownHeadAnchor(Sprite bodySprite)
    {
        return bodySprite != null
            ? SpriteTopLeftPixelsToLocal(bodySprite, LatPulldownHeadAnchorPixels)
            : Vector2.zero;
    }

    private static Vector2 GetLegPressHeadAnchor(Sprite bodySprite)
    {
        if (bodySprite == null)
        {
            return Vector2.zero;
        }

        Vector2 topLeftPixels = LegPressHeadAnchorPixelsBySpriteName.TryGetValue(bodySprite.name, out Vector2 frameAnchorPixels)
            ? frameAnchorPixels
            : LegPressFallbackHeadAnchorPixels;

        return SpriteTopLeftPixelsToLocal(bodySprite, topLeftPixels);
    }

    private static Vector2 GetDumbbellCurlHeadAnchor(Sprite bodySprite)
    {
        return GetDumbbellHeadAnchor(
            bodySprite,
            DumbbellCurlHeadAnchorPixelsBySpriteName,
            DumbbellCurlFallbackHeadAnchorPixels);
    }

    private static Vector2 GetDumbbellShoulderPressHeadAnchor(Sprite bodySprite)
    {
        return GetDumbbellHeadAnchor(
            bodySprite,
            DumbbellShoulderPressHeadAnchorPixelsBySpriteName,
            DumbbellShoulderPressFallbackHeadAnchorPixels);
    }

    private static Vector2 GetDumbbellHeadAnchor(
        Sprite bodySprite,
        Dictionary<string, Vector2> anchorsBySpriteName,
        Vector2 fallbackTopLeftPixels)
    {
        if (bodySprite == null)
        {
            return Vector2.zero;
        }

        Vector2 topLeftPixels = anchorsBySpriteName.TryGetValue(bodySprite.name, out Vector2 frameAnchorPixels)
            ? frameAnchorPixels
            : fallbackTopLeftPixels;

        return SpriteTopLeftPixelsToLocal(bodySprite, topLeftPixels);
    }

    private static Vector2 GetYogaHeadAnchor(
        Sprite bodySprite,
        Dictionary<string, Vector2> anchorsBySpriteName)
    {
        if (bodySprite == null)
        {
            return Vector2.zero;
        }

        Vector2 topLeftPixels = anchorsBySpriteName.TryGetValue(bodySprite.name, out Vector2 frameAnchorPixels)
            ? frameAnchorPixels
            : new Vector2(bodySprite.rect.width * 0.5f, bodySprite.rect.height * 0.35f);

        return SpriteTopLeftPixelsToLocal(bodySprite, topLeftPixels);
    }

    private static Vector2 GetLockerHeadAnchor(Sprite bodySprite)
    {
        if (bodySprite == null)
        {
            return Vector2.zero;
        }

        Vector2 topLeftPixels = LockerHeadAnchorPixelsBySpriteName.TryGetValue(bodySprite.name, out Vector2 frameAnchorPixels)
            ? frameAnchorPixels
            : new Vector2(bodySprite.rect.width * 0.5f, 36f);

        return SpriteTopLeftPixelsToLocal(bodySprite, topLeftPixels);
    }

    private static int GetLatPulldownCustomerFrameIndex(int frameCount)
    {
        if (frameCount <= 0)
        {
            return 0;
        }

        int machineFrame = GymPlacedObjectVisual.GetLatPulldownAnimationFrameIndex(LatPulldownBodyFrameMap.Length);
        if (frameCount < LatPulldownBodyFrameMap.Length)
        {
            return machineFrame % frameCount;
        }

        return Mathf.Clamp(LatPulldownBodyFrameMap[machineFrame], 0, frameCount - 1);
    }

    private static int GetLegPressCustomerFrameIndex(int frameCount)
    {
        if (frameCount <= 0)
        {
            return 0;
        }

        int stepIndex = GymPlacedObjectVisual.GetLegPressAnimationStepIndex() % LegPressBodyFrameMap.Length;
        return Mathf.Clamp(LegPressBodyFrameMap[stepIndex], 0, frameCount - 1);
    }

    private static Vector2 GetLegPressBodyFrameLocalOffset(Sprite bodySprite, float visualScale)
    {
        if (bodySprite == null ||
            !LegPressBodyFrameOffsetPixelsBySpriteName.TryGetValue(bodySprite.name, out Vector2 offsetPixels))
        {
            return Vector2.zero;
        }

        return offsetPixels * (visualScale / bodySprite.pixelsPerUnit);
    }

    private static Vector2 SpriteTopLeftPixelsToLocal(Sprite sprite, Vector2 topLeftPixels)
    {
        Vector2 bottomLeftPixels = new Vector2(topLeftPixels.x, sprite.rect.height - topLeftPixels.y);
        return (bottomLeftPixels - sprite.pivot) / sprite.pixelsPerUnit;
    }

    private static bool IsExerciseBikeMachineKey(string machineKey)
    {
        return !string.IsNullOrWhiteSpace(machineKey) &&
            (machineKey.Equals("exercise_bike", System.StringComparison.OrdinalIgnoreCase) ||
             machineKey.StartsWith("exercise_bike_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBenchPressMachineKey(string machineKey)
    {
        return !string.IsNullOrWhiteSpace(machineKey) &&
            (machineKey.Equals("bench_press", System.StringComparison.OrdinalIgnoreCase) ||
             machineKey.StartsWith("bench_press_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLatPulldownMachineKey(string machineKey)
    {
        return !string.IsNullOrWhiteSpace(machineKey) &&
            (machineKey.Equals("lat_pulldown", System.StringComparison.OrdinalIgnoreCase) ||
             machineKey.StartsWith("lat_pulldown_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLegPressMachineKey(string machineKey)
    {
        return !string.IsNullOrWhiteSpace(machineKey) &&
            (machineKey.Equals("leg_press", System.StringComparison.OrdinalIgnoreCase) ||
             machineKey.StartsWith("leg_press_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDumbbellRackMachineKey(string machineKey)
    {
        return !string.IsNullOrWhiteSpace(machineKey) &&
            (machineKey.Equals("dumbbell_rack", System.StringComparison.OrdinalIgnoreCase) ||
             machineKey.StartsWith("dumbbell_rack_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsYogaMatMachineKey(string machineKey)
    {
        return !string.IsNullOrWhiteSpace(machineKey) &&
            (machineKey.Equals("yoga_mat", System.StringComparison.OrdinalIgnoreCase) ||
             machineKey.StartsWith("yoga_mat_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLockerMachineKey(string machineKey)
    {
        return !string.IsNullOrWhiteSpace(machineKey) &&
            (machineKey.Equals("locker", System.StringComparison.OrdinalIgnoreCase) ||
             machineKey.StartsWith("locker_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static Vector2 GetBodyPivotCompensation(Sprite bodySprite, bool bodyFlipX, float visualScale)
    {
        if (bodySprite == null)
        {
            return Vector2.zero;
        }

        Rect rect = bodySprite.rect;
        Vector2 placementPivot;
        if (IsRunBodySprite(bodySprite))
        {
            placementPivot = new Vector2(rect.width * 0.5f, 0f);
        }
        else if (IsExerciseBikeBodySprite(bodySprite))
        {
            placementPivot = ExerciseBikeBodyPlacementPivotPixels;
        }
        else
        {
            return Vector2.zero;
        }

        Vector2 compensation = (bodySprite.pivot - placementPivot) / bodySprite.pixelsPerUnit;

        if (bodyFlipX)
        {
            compensation.x = -compensation.x;
        }

        return compensation * visualScale;
    }

    private static bool TryGetRunHeadAttachmentAnchor(Sprite bodySprite, out Vector2 anchor)
    {
        if (!IsRunBodySprite(bodySprite))
        {
            anchor = Vector2.zero;
            return false;
        }

        anchor = new Vector2(0f, RunHeadAttachmentYOffset);
        return true;
    }

    private Sprite[] GetDumbbellRackAnimationFrames(string machineKey)
    {
        bool hasCurlFrames = dumbbellCurlFrames != null && dumbbellCurlFrames.Length > 0;
        bool hasShoulderPressFrames = dumbbellShoulderPressFrames != null && dumbbellShoulderPressFrames.Length > 0;
        if (!hasCurlFrames && !hasShoulderPressFrames)
        {
            return idleFrames;
        }

        if (selectedDumbbellRackAnimationIndex < 0 ||
            !string.Equals(selectedDumbbellRackMachineKey, machineKey, System.StringComparison.OrdinalIgnoreCase))
        {
            selectedDumbbellRackMachineKey = machineKey ?? string.Empty;
            selectedDumbbellRackAnimationIndex = hasCurlFrames && hasShoulderPressFrames
                ? Random.Range(0, 2)
                : hasCurlFrames ? 0 : 1;
        }

        if (selectedDumbbellRackAnimationIndex == 1 && hasShoulderPressFrames)
        {
            return dumbbellShoulderPressFrames;
        }

        return hasCurlFrames ? dumbbellCurlFrames : dumbbellShoulderPressFrames;
    }

    private void ResetDumbbellRackAnimationChoice()
    {
        selectedDumbbellRackAnimationIndex = -1;
        selectedDumbbellRackMachineKey = string.Empty;
    }

    private Sprite[] GetYogaMatAnimationFrames(string machineKey)
    {
        bool hasOverheadFrames = yogaOverheadFrames != null && yogaOverheadFrames.Length > 0;
        bool hasSideBendFrames = yogaSideBendFrames != null && yogaSideBendFrames.Length > 0;
        bool hasToeTouchFrames = yogaToeTouchFrames != null && yogaToeTouchFrames.Length > 0;
        if (!hasOverheadFrames && !hasSideBendFrames && !hasToeTouchFrames)
        {
            return idleFrames;
        }

        bool selectedAnimationAvailable =
            (selectedYogaMatAnimationIndex == 0 && hasOverheadFrames) ||
            (selectedYogaMatAnimationIndex == 1 && hasSideBendFrames) ||
            (selectedYogaMatAnimationIndex == 2 && hasToeTouchFrames);
        if (!selectedAnimationAvailable ||
            !string.Equals(selectedYogaMatMachineKey, machineKey, System.StringComparison.OrdinalIgnoreCase))
        {
            List<int> availableAnimations = new List<int>(3);
            if (hasOverheadFrames) availableAnimations.Add(0);
            if (hasSideBendFrames) availableAnimations.Add(1);
            if (hasToeTouchFrames) availableAnimations.Add(2);

            selectedYogaMatMachineKey = machineKey ?? string.Empty;
            selectedYogaMatAnimationIndex = availableAnimations[Random.Range(0, availableAnimations.Count)];
        }

        switch (selectedYogaMatAnimationIndex)
        {
            case 1:
                return yogaSideBendFrames;
            case 2:
                return yogaToeTouchFrames;
            default:
                return yogaOverheadFrames;
        }
    }

    private void ResetYogaMatAnimationChoice()
    {
        selectedYogaMatAnimationIndex = -1;
        selectedYogaMatMachineKey = string.Empty;
    }

    private static float GetYogaMatHeadRotationZ(int animationIndex, int frameIndex)
    {
        float[] rotations = animationIndex == 1
            ? YogaSideBendHeadRotationByFrame
            : animationIndex == 2
                ? YogaToeTouchHeadRotationByFrame
                : null;
        if (rotations == null || rotations.Length <= 0)
        {
            return 0f;
        }

        return rotations[Mathf.Abs(frameIndex) % rotations.Length];
    }

    void Update()
    {
        if (customer == null || bodyRenderer == null || headRenderer == null) return;
        ApplySortingOrders();

        bool isUsingDumbbellRack = customer.state == CustomerFlowManager.CustomerState.UsingMachine &&
            IsDumbbellRackMachineKey(customer.targetMachineKey);
        if (!isUsingDumbbellRack)
        {
            ResetDumbbellRackAnimationChoice();
        }

        bool isUsingYogaMat = customer.state == CustomerFlowManager.CustomerState.UsingMachine &&
            IsYogaMatMachineKey(customer.targetMachineKey);
        if (!isUsingYogaMat)
        {
            ResetYogaMatAnimationChoice();
        }

        Vector3 moveDelta = customer.worldPosition - lastWorldPosition;
        lastWorldPosition = customer.worldPosition;

        bool isMoving = moveDelta.sqrMagnitude > 0.000001f;
        float fps = 4f; // 기본(Idle) 상태의 FPS를 4로 낮추어 자연스러운 숨쉬기 연출
        int headIndex = 0; // 0=정면, 1=측면, 2=후면
        bool flipX = false;
        bool headFlipX = false;
        bool hideDetachedHead = false;
        bool syncMachineAnimation = false;
        bool reverseBodyAnimation = false;
        bool useBenchPressBodyFrameMap = false;
        bool useLatPulldownFrameSync = false;
        bool useLegPressFrameSync = false;
        bool useYogaOverheadBodyFrameMap = false;
        bool useYogaHeadRotation = false;
        float customerVisualScale = 1f;
        float headVisualScale = 1f;
        float headRotationZ = 0f;
        bool useSpecialHeadTransform = false;
        bool renderHeadBehindBody = false;
        Vector3 visualOffset = Vector3.zero;

        Sprite[] currentBodyFrames = idleFrames;

        if (customer.state == CustomerFlowManager.CustomerState.UsingMachine)
        {
            if (!string.IsNullOrEmpty(customer.targetMachineKey) && customer.targetMachineKey.StartsWith("treadmill_", System.StringComparison.OrdinalIgnoreCase))
            {
                currentBodyFrames = runFrames;
                fps = 10f; // 런닝머신 속도
                flipX = true; // 좌우 반전
                headIndex = 1;
                headFlipX = true;
                visualOffset = Vector3.zero;
            }
            else if (IsExerciseBikeMachineKey(customer.targetMachineKey))
            {
                currentBodyFrames = exerciseBikeFrames != null && exerciseBikeFrames.Length > 0
                    ? exerciseBikeFrames
                    : idleFrames;
                fps = 10f;
                flipX = false;
                headIndex = 1;
                headFlipX = true;
                syncMachineAnimation = true;
                reverseBodyAnimation = true;
                customerVisualScale = ExerciseBikeCustomerVisualScale;
                visualOffset = ExerciseBikeBodyLocalOffset;
            }
            else if (IsBenchPressMachineKey(customer.targetMachineKey))
            {
                currentBodyFrames = benchPressFrames != null && benchPressFrames.Length > 0
                    ? benchPressFrames
                    : idleFrames;
                fps = BenchPressAnimationFps;
                flipX = false;
                headIndex = 1;
                headFlipX = false;
                syncMachineAnimation = true;
                useBenchPressBodyFrameMap = true;
                customerVisualScale = BenchPressCustomerVisualScale;
                headVisualScale = BenchPressHeadVisualScale;
                headRotationZ = BenchPressHeadRotationZ;
                useSpecialHeadTransform = true;
                visualOffset = BenchPressBodyLocalOffset;
            }
            else if (IsLatPulldownMachineKey(customer.targetMachineKey))
            {
                currentBodyFrames = latPulldownFrames != null && latPulldownFrames.Length > 0
                    ? latPulldownFrames
                    : idleFrames;
                fps = GymPlacedObjectVisual.LatPulldownAnimationFps;
                flipX = false;
                headIndex = 1;
                headFlipX = true;
                syncMachineAnimation = true;
                useLatPulldownFrameSync = true;
                customerVisualScale = LatPulldownCustomerVisualScale;
                renderHeadBehindBody = true;
                visualOffset = LatPulldownBodyLocalOffset;
            }
            else if (IsLegPressMachineKey(customer.targetMachineKey))
            {
                currentBodyFrames = legPressFrames != null && legPressFrames.Length > 0
                    ? legPressFrames
                    : idleFrames;
                fps = GymPlacedObjectVisual.LegPressAnimationFps;
                flipX = false;
                headIndex = 1;
                headFlipX = true;
                syncMachineAnimation = true;
                useLegPressFrameSync = true;
                customerVisualScale = LegPressCustomerVisualScale;
                headRotationZ = LegPressHeadRotationZ;
                useSpecialHeadTransform = true;
                visualOffset = LegPressBodyLocalOffset;
            }
            else if (IsDumbbellRackMachineKey(customer.targetMachineKey))
            {
                currentBodyFrames = GetDumbbellRackAnimationFrames(customer.targetMachineKey);
                fps = DumbbellRackAnimationFps;
                flipX = false;
                headIndex = 0;
                headFlipX = false;
                customerVisualScale = DumbbellRackCustomerVisualScale;
                visualOffset = DumbbellRackBodyLocalOffset;
            }
            else if (IsYogaMatMachineKey(customer.targetMachineKey))
            {
                currentBodyFrames = GetYogaMatAnimationFrames(customer.targetMachineKey);
                fps = YogaMatAnimationFps;
                flipX = false;
                headIndex = selectedYogaMatAnimationIndex == 2 ? 1 : 0;
                headFlipX = false;
                bool useToeTouchScale = selectedYogaMatAnimationIndex == 2;
                customerVisualScale = useToeTouchScale
                    ? YogaToeTouchCustomerVisualScale
                    : YogaMatCustomerVisualScale;
                visualOffset = useToeTouchScale
                    ? YogaToeTouchBodyLocalOffset
                    : YogaMatBodyLocalOffset;
                useYogaOverheadBodyFrameMap = selectedYogaMatAnimationIndex == 0;
                useYogaHeadRotation = selectedYogaMatAnimationIndex != 0;
            }
            else if (IsLockerMachineKey(customer.targetMachineKey))
            {
                currentBodyFrames = lockerUseFrames != null && lockerUseFrames.Length > 0
                    ? lockerUseFrames
                    : idleFrames;
                fps = LockerAnimationFps;
                flipX = false;
                headIndex = 2;
                headFlipX = false;
                customerVisualScale = LockerCustomerVisualScale;
                visualOffset = LockerBodyLocalOffset;
            }
            else
            {
                currentBodyFrames = idleFrames;
                fps = 6f; // 기구 사용 시 속도
                flipX = true;
                headIndex = 1;
                headFlipX = true;
            }
        }
        else if (isMoving)
        {
            fps = 8f; // 걷기 애니메이션은 8 FPS
            if (Mathf.Abs(moveDelta.x) > 0.0001f)
            {
                currentBodyFrames = walkHorizontalFrames;
                headIndex = 1; // 측면
                flipX = moveDelta.x < 0;
                headFlipX = flipX;
            }
            else
            {
                currentBodyFrames = walkVerticalFrames;
                if (moveDelta.y > 0)
                {
                    headIndex = 2; // 위로 (뒷모습)
                }
                else
                {
                    headIndex = 0; // 아래로 (정면)
                }
            }
        }

        // 몸통 위치 및 애니메이션 업데이트
        if (currentBodyFrames != null && currentBodyFrames.Length > 0)
        {
            float frameTime = syncMachineAnimation ? Time.time : Time.time + animationOffset;
            int frame = Mathf.FloorToInt(frameTime * fps) % currentBodyFrames.Length;
            if (useLatPulldownFrameSync)
            {
                frame = GetLatPulldownCustomerFrameIndex(currentBodyFrames.Length);
            }
            if (useLegPressFrameSync)
            {
                frame = GetLegPressCustomerFrameIndex(currentBodyFrames.Length);
            }
            if (reverseBodyAnimation && currentBodyFrames.Length > 1)
            {
                frame = currentBodyFrames.Length - 1 - frame;
            }
            if (useBenchPressBodyFrameMap && currentBodyFrames.Length >= BenchPressBodyFrameMap.Length)
            {
                frame = BenchPressBodyFrameMap[frame % BenchPressBodyFrameMap.Length];
            }
            if (useYogaOverheadBodyFrameMap && currentBodyFrames.Length >= YogaOverheadBodyFrameMap.Length)
            {
                frame = YogaOverheadBodyFrameMap[frame % YogaOverheadBodyFrameMap.Length];
            }
            bodyRenderer.sprite = currentBodyFrames[frame];
            bodyRenderer.flipX = flipX;

            if (useYogaHeadRotation)
            {
                headRotationZ = GetYogaMatHeadRotationZ(selectedYogaMatAnimationIndex, frame);
                useSpecialHeadTransform = true;
            }
        }

        Vector2 bodyPivotCompensation = GetBodyPivotCompensation(bodyRenderer.sprite, bodyRenderer.flipX, customerVisualScale);
        Vector2 legPressFrameOffset = useLegPressFrameSync
            ? GetLegPressBodyFrameLocalOffset(bodyRenderer.sprite, customerVisualScale)
            : Vector2.zero;
        Vector3 bodyLocalPosition = new Vector3(
            visualOffset.x + bodyPivotCompensation.x + legPressFrameOffset.x,
            visualOffset.y + bodyPivotCompensation.y + legPressFrameOffset.y,
            0f);
        bodyRenderer.gameObject.transform.localPosition = bodyLocalPosition;
        bodyRenderer.gameObject.transform.localScale = new Vector3(customerVisualScale, customerVisualScale, 1f);
        if (renderHeadBehindBody)
        {
            headRenderer.sortingOrder = bodyRenderer.sortingOrder - 2;
        }

        if (useSpecialHeadTransform)
        {
            headRenderer.gameObject.transform.localScale = new Vector3(headVisualScale, headVisualScale, 1f);
            headRenderer.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, headRotationZ);
        }
        else
        {
            headRenderer.gameObject.transform.localScale = Vector3.one;
            headRenderer.gameObject.transform.localRotation = Quaternion.identity;
        }

        // 머리 애니메이션 업데이트
        headRenderer.enabled = !hideDetachedHead;

        if (!hideDetachedHead && headFrames != null && headFrames.Length > headIndex)
        {
            headRenderer.sprite = headFrames[headIndex];
            headRenderer.flipX = headFlipX;
        }

        // --- 동적 머리 위치 (바운싱) ---
        if (!hideDetachedHead && bodyRenderer.sprite != null)
        {
            Vector2 headAnchor = GetHeadAnchorForBodySprite(bodyRenderer.sprite, bodyRenderer.flipX);
            headAnchor *= customerVisualScale;
            // Keep the head attached to the current body frame's attachment point.
            headObject.transform.localPosition = new Vector3(
                bodyLocalPosition.x + headAnchor.x,
                bodyLocalPosition.y + headAnchor.y,
                0f);
        }
    }

    private void ApplySortingOrders()
    {
        if (bodyRenderer == null || headRenderer == null)
        {
            return;
        }

        bodyRenderer.sortingOrder = customer != null ? customer.bodySortingOrder : DefaultBodySortingOrder;
        headRenderer.sortingOrder = customer != null ? customer.headSortingOrder : DefaultHeadSortingOrder;
    }
}
