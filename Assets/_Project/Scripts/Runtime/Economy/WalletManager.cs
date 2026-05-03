using System;
using UnityEngine;

public class WalletManager : MonoBehaviour
{
    [Header("Wallet Settings")]
    [SerializeField] private int startingCash = 30000;
    [SerializeField] public int startingStarCoin = 50; 
    [SerializeField] private bool showDebugHud = false;

    private int currentCash;
    private int currentStarCoin; // Added currentStarCoin
    private bool isInitialized = false;
    private GUIStyle hudStyle;

    public int CurrentCash => currentCash;
    public int CurrentStarCoin => currentStarCoin; // Added CurrentStarCoin property

    // Added events for cash and star coin changes
    public event Action<int, int> CashChanged;
    public event Action<int, int> StarCoinChanged;

    public void InitializeWallet()
    {
        if (isInitialized)
        {
            return;
        }

        currentCash = startingCash;
        currentStarCoin = startingStarCoin; // Initialize star coin
        isInitialized = true;

        Debug.Log($"[WalletManager] 시작 자금 설정 완료: 현금 {currentCash:N0}, 스타코인 {currentStarCoin:N0}");

        // Invoke events after initialization
        CashChanged?.Invoke(currentCash, 0); // Initial cash, change 0
        StarCoinChanged?.Invoke(currentStarCoin, 0); // Initial star coin, change 0
    }

    public bool CanSpend(int amount)
    {
        if (!isInitialized)
        {
            InitializeWallet();
        }

        if (amount < 0)
        {
            return false;
        }

        return currentCash >= amount;
    }

    public bool TrySpend(int amount, string reason = "")
    {
        if (!isInitialized)
        {
            InitializeWallet();
        }

        if (amount < 0)
        {
            Debug.LogWarning("[WalletManager] 음수 금액은 사용할 수 없어.");
            return false;
        }

        if (currentCash < amount)
        {
            Debug.Log($"[WalletManager] 돈 부족 / 현재: {currentCash:N0}, 필요: {amount:N0}");
            return false;
        }

        currentCash -= amount;

        if (string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"[WalletManager] {amount:N0} 사용 / 잔액: {currentCash:N0}");
        }
        else
        {
            Debug.Log($"[WalletManager] {reason} / {amount:N0} 사용 / 잔액: {currentCash:N0}");
        }

        CashChanged?.Invoke(currentCash, -amount); // Invoke event
        return true;
    }

    public void SpendMandatory(int amount, string reason = "")
    {
        if (!isInitialized)
        {
            InitializeWallet();
        }

        if (amount < 0)
        {
            Debug.LogWarning("[WalletManager] 음수 금액은 강제 지출할 수 없어.");
            return;
        }

        currentCash -= amount;

        if (string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"[WalletManager] 강제 지출 {amount:N0} / 잔액: {currentCash:N0}");
        }
        else
        {
            Debug.Log($"[WalletManager] {reason} / 강제 지출 {amount:N0} / 잔액: {currentCash:N0}");
        }
    }

    public void AddCash(int amount, string reason = "")
    {
        if (!isInitialized)
        {
            InitializeWallet();
        }

        if (amount <= 0)
        {
            return;
        }

        currentCash += amount;

        if (string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"[WalletManager] {amount:N0} 획득 / 잔액: {currentCash:N0}");
        }
        else
        {
            Debug.Log($"[WalletManager] {reason} / {amount:N0} 획득 / 잔액: {currentCash:N0}");
        }
    }

    public void AddStarCoin(int amount, string reason = "")
    {
        if (!isInitialized)
        {
            InitializeWallet();
        }

        if (amount <= 0)
        {
            return;
        }

        currentStarCoin += amount;

        if (string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"[WalletManager] 스타코인 {amount:N0} 획득 / 잔액: {currentStarCoin:N0}");
        }
        else
        {
            Debug.Log($"[WalletManager] {reason} / 스타코인 {amount:N0} 획득 / 잔액: {currentStarCoin:N0}");
        }

        StarCoinChanged?.Invoke(currentStarCoin, amount);
    }

    public bool TrySpendStarCoin(int amount, string reason = "")
    {
        if (!isInitialized)
        {
            InitializeWallet();
        }

        if (amount < 0)
        {
            Debug.LogWarning("[WalletManager] 음수 스타코인은 사용할 수 없습니다.");
            return false;
        }

        if (currentStarCoin < amount)
        {
            Debug.LogWarning($"[WalletManager] 스타코인 부족 / 현재: {currentStarCoin:N0}, 필요: {amount:N0}");
            return false;
        }

        currentStarCoin -= amount;

        if (string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"[WalletManager] 스타코인 {amount:N0} 사용 / 잔액: {currentStarCoin:N0}");
        }
        else
        {
            Debug.Log($"[WalletManager] {reason} / 스타코인 {amount:N0} 사용 / 잔액: {currentStarCoin:N0}");
        }

        StarCoinChanged?.Invoke(currentStarCoin, -amount);
        return true;
    }

    public void LoadWallet(int cash, int starCoin)
    {
        if (!isInitialized)
        {
            InitializeWallet();
        }
        currentCash = cash;
        currentStarCoin = starCoin;
        
        CashChanged?.Invoke(currentCash, 0);
        StarCoinChanged?.Invoke(currentStarCoin, 0);
    }

    public void SetCash(int amount, string reason = "")
    {
        if (!isInitialized)
        {
            InitializeWallet();
        }

        currentCash = amount;

        if (string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"[WalletManager] 현금 설정 / 잔액: {currentCash:N0}");
        }
        else
        {
            Debug.Log($"[WalletManager] {reason} / 잔액: {currentCash:N0}");
        }
    }

    // Legacy UI Disabled
    private void Disabled_OnGUI()
    {
        if (!showDebugHud || !isInitialized)
        {
            return;
        }

        bool isPortrait = Screen.height > Screen.width;
        float panelWidth = isPortrait ? Mathf.Min(Screen.width - 24f, 190f) : 240f;
        float panelHeight = 72f;

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.box);
            hudStyle.alignment = TextAnchor.UpperLeft;
            hudStyle.padding = new RectOffset(12, 12, 12, 8);
            hudStyle.normal.textColor = Color.white;
            hudStyle.richText = true;
        }

        hudStyle.fontSize = isPortrait ? 18 : 20;

        GUI.Box(
            new Rect(12f, 12f, panelWidth, panelHeight),
            $"현금: {currentCash:N0}\n<color=#4CFFD1>스타코인: {currentStarCoin:N0} ★</color>",
            hudStyle
        );
    }
}