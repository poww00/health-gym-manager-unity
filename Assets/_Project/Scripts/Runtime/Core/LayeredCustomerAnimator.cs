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
    private Sprite[] headFrames;

    private Vector3 lastWorldPosition;
    private float animationOffset;

    private const float FallbackHeadAnchorInsetY = 0.12f;
    private const float RunHeadAttachmentYOffset = -0.05f;
    private const float RunHeadAttachmentPostFlipXOffset = 0.015f;
    private static readonly Vector3 ExerciseBikeBodyLocalOffset = new Vector3(-0.2f, 0.33f, 0f);
    private const float ExerciseBikeCustomerVisualScale = 0.90f;
    private const int DefaultBodySortingOrder = 30;
    private const int DefaultHeadSortingOrder = 31;

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
        { "body_male_chubby_exercise_bike_4x2_0", new Vector2(-0.037f, 0.625f) },
        { "body_male_chubby_exercise_bike_4x2_1", new Vector2(-0.052f, 0.625f) },
        { "body_male_chubby_exercise_bike_4x2_2", new Vector2(-0.098f, 0.625f) },
        { "body_male_chubby_exercise_bike_4x2_3", new Vector2(-0.153f, 0.625f) },
        { "body_male_chubby_exercise_bike_4x2_4", new Vector2(-0.055f, 0.655f) },
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

    private static bool IsExerciseBikeMachineKey(string machineKey)
    {
        return !string.IsNullOrWhiteSpace(machineKey) &&
            (machineKey.Equals("exercise_bike", System.StringComparison.OrdinalIgnoreCase) ||
             machineKey.StartsWith("exercise_bike_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static Vector2 GetBodyPivotCompensation(Sprite bodySprite, bool bodyFlipX)
    {
        if (!IsRunBodySprite(bodySprite))
        {
            return Vector2.zero;
        }

        Rect rect = bodySprite.rect;
        Vector2 oldBottomCenterPivot = new Vector2(rect.width * 0.5f, 0f);
        Vector2 compensation = (bodySprite.pivot - oldBottomCenterPivot) / bodySprite.pixelsPerUnit;

        if (bodyFlipX)
        {
            compensation.x = -compensation.x;
        }

        return compensation;
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

    void Update()
    {
        if (customer == null || bodyRenderer == null || headRenderer == null) return;
        ApplySortingOrders();

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
        float customerVisualScale = 1f;
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
            if (reverseBodyAnimation && currentBodyFrames.Length > 1)
            {
                frame = currentBodyFrames.Length - 1 - frame;
            }
            bodyRenderer.sprite = currentBodyFrames[frame];
            bodyRenderer.flipX = flipX;
        }

        Vector2 bodyPivotCompensation = GetBodyPivotCompensation(bodyRenderer.sprite, bodyRenderer.flipX);
        Vector3 bodyLocalPosition = new Vector3(
            visualOffset.x + bodyPivotCompensation.x,
            visualOffset.y + bodyPivotCompensation.y,
            0f);
        bodyRenderer.gameObject.transform.localPosition = bodyLocalPosition;
        bodyRenderer.gameObject.transform.localScale = new Vector3(customerVisualScale, customerVisualScale, 1f);
        headRenderer.gameObject.transform.localScale = new Vector3(customerVisualScale, customerVisualScale, 1f);

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
