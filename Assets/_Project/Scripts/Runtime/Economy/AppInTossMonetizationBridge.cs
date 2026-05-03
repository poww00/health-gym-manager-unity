using UnityEngine;
using System;

/// <summary>
/// 앱인토스(AppInToss) 전용 결제(IAP) 및 보상형 광고(IAA)의 연동 뼈대 (Mock 브릿지).
/// 3천만 토스 앱 사용자 대상 플랫폼으로 배포 시, 토스페이 SDK와 애드몹 SDK를 플러그인 형태로 감쌉니다.
/// MVP 단계에서는 이 브릿지를 통해 이벤트를 흉내내고 WalletManager로 재화를 즉시 지급합니다.
/// </summary>
public class AppInTossMonetizationBridge : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private WalletManager walletManager;

    private void Awake()
    {
        if (walletManager == null)
        {
            walletManager = FindFirstObjectByType<WalletManager>();
        }
    }

    /// <summary>
    /// [Mock] TossPay IAP 결제 연동 (인앱 결제)
    /// 외부 토스페이 SDK를 호출하고 콜백을 비동기로 받아야 하지만, 즉각 성공으로 가상 연동.
    /// </summary>
    /// <param name="productId">상품 ID</param>
    /// <param name="rewardAmount">지급할 스타코인/현금 보상량</param>
    /// <param name="onComplete">성공 여부 콜백</param>
    public void PurchaseIAP(string productId, int rewardAmount, Action<bool> onComplete = null)
    {
        Debug.Log($"[AppInToss] 토스페이 IAP 결제 요청 진행중: {productId} (보상량: {rewardAmount})");
        
        // 실제 연동 시: TossPayment_SDK.RequestPurchase(productId, (result) => { ... });
        bool isDummySuccess = true; 
        
        if (isDummySuccess)
        {
            if (walletManager != null)
            {
                walletManager.AddCash(rewardAmount, "앱인토스 IAP 토스페이 결제 보상");
            }
            else
            {
                Debug.LogWarning("[AppInToss] WalletManager가 없어 결제 보상 지급을 스킵했습니다.");
            }
        }
        
        onComplete?.Invoke(isDummySuccess);
    }

    /// <summary>
    /// [Mock] AdMob 보상형 동영상 광고 (IAA) 연동
    /// 앱인토스 토스 제공 애드몹 SDK를 통해 OnUserEarnedReward 등 콜백 대기 후 지급해야 함.
    /// MVP 에서는 즉각 보상 처리로 가상 연동.
    /// </summary>
    /// <param name="rewardAmount">지급할 광고 보상량</param>
    /// <param name="onComplete">성공 여부 콜백</param>
    public void ShowRewardedAd(int rewardAmount, Action<bool> onComplete = null)
    {
        Debug.Log($"[AppInToss] 애드몹 보상형 광고 시청 요청");

        // 실제 연동 시: AdMob_SDK.ShowRewardedAd((reward) => { ... });
        bool adWatchedSuccessfully = true;

        if (adWatchedSuccessfully)
        {
            if (walletManager != null)
            {
                walletManager.AddCash(rewardAmount, "애드몹 광고 시청 보상");
            }
            else
            {
                Debug.LogWarning("[AppInToss] WalletManager가 없어 광고 보상 지급을 스킵했습니다.");
            }
        }

        onComplete?.Invoke(adWatchedSuccessfully);
    }
}
