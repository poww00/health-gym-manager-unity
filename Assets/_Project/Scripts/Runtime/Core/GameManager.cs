using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private WalletManager walletManager;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private MonthlySettlementManager monthlySettlementManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private GymSiteManager gymSiteManager;
    [SerializeField] private RelocationManager relocationManager;
    [SerializeField] private GridCameraController gridCameraController;
    [SerializeField] private CustomerFlowManager customerFlowManager;

    private void Start()
    {
        if (walletManager == null)
        {
            walletManager = FindFirstObjectByType<WalletManager>();
        }

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }

        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }

        if (monthlySettlementManager == null)
        {
            monthlySettlementManager = FindFirstObjectByType<MonthlySettlementManager>();
        }

        if (saveManager == null)
        {
            saveManager = FindFirstObjectByType<SaveManager>();
        }

        gymSiteManager = EnsureRuntimeManager(gymSiteManager, "GymSiteManager");
        relocationManager = EnsureRuntimeManager(relocationManager, "RelocationManager");

        if (walletManager != null)
        {
            walletManager.InitializeWallet();
        }
        else
        {
            Debug.LogWarning("[GameManager] WalletManager가 없어서 돈 시스템 없이 실행돼.");
        }

        if (gymSiteManager != null)
        {
            gymSiteManager.InitializeSiteState();
        }
        else
        {
            Debug.LogError("[GameManager] GymSiteManager를 준비하지 못했어.");
            return;
        }

        if (saveManager != null)
        {
            saveManager.PrepareBootstrapStateBeforeGridGeneration();
        }
        else
        {
            Debug.LogError("[GameManager] SaveManager를 찾지 못했어.");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError("[GameManager] GridManager를 찾지 못했어.");
            return;
        }

        gymSiteManager.ApplyCurrentSiteToGridManager(gridManager);
        EnsureMainCameraController();
        gridManager.GenerateGrid();

        if (placementManager != null)
        {
            placementManager.Initialize();
        }
        else
        {
            Debug.LogWarning("[GameManager] PlacementManager가 없어서 배치 기능은 비활성화 상태야.");
        }

        if (timeManager != null)
        {
            timeManager.InitializeTime();
        }
        else
        {
            Debug.LogWarning("[GameManager] TimeManager가 없어서 날짜 시스템 없이 실행돼.");
        }

        if (monthlySettlementManager != null)
        {
            monthlySettlementManager.InitializeSettlement();
        }
        else
        {
            Debug.LogWarning("[GameManager] MonthlySettlementManager가 없어서 월말 결산 없이 실행돼.");
        }

        Debug.Log("[GameManager] SaveManager 초기화 호출");
        saveManager.InitializeSaveSystem();

        customerFlowManager = EnsureRuntimeManager(customerFlowManager, "CustomerFlowManager");
        if (customerFlowManager != null)
        {
            customerFlowManager.InitializePrototype();
        }
    }

    private T EnsureRuntimeManager<T>(T currentReference, string managerName) where T : MonoBehaviour
    {
        if (currentReference != null)
        {
            return currentReference;
        }

        T found = FindFirstObjectByType<T>();
        if (found != null)
        {
            return found;
        }

        T created = gameObject.AddComponent<T>();
        Debug.Log($"[GameManager] {managerName}가 없어서 Systems에 런타임으로 추가했어. (프로토타입 편의용)");

        return created;
    }

    private void EnsureMainCameraController()
    {
        if (gridCameraController != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[GameManager] Main Camera를 찾지 못해서 GridCameraController를 자동 연결하지 못했어.");
            return;
        }

        gridCameraController = mainCamera.GetComponent<GridCameraController>();
        if (gridCameraController != null)
        {
            return;
        }

        gridCameraController = mainCamera.gameObject.AddComponent<GridCameraController>();
        Debug.Log("[GameManager] GridCameraController가 없어서 Main Camera에 런타임으로 추가했어. (프로토타입 편의용)");
    }
}
