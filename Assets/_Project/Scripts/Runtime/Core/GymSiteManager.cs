using UnityEngine;

public enum GymLocationType
{
    Neighborhood = 0,
    StationArea = 1,
    Downtown = 2,
    Premium = 3,
}

public enum GymSiteTier
{
    Starter8x8 = 0,
    Expansion16x16 = 1,
    FullOpen32x32 = 2,
    Mega64x64 = 3,
}

[System.Serializable]
public struct GymLocationPrototypeRules
{
    public GymLocationType locationType;
    public int monthlyRentFlatAdditive;
    public float monthlyRentMultiplier;
    public float leadMultiplier;
    public float ancillaryRevenueMultiplier;
    public float churnMultiplier;
    public float satisfactionTargetOffset;
    public int relocationContractSurcharge;

    public static GymLocationPrototypeRules CreateDefault()
    {
        return new GymLocationPrototypeRules
        {
            locationType = GymLocationType.Neighborhood,
            monthlyRentFlatAdditive = 0,
            monthlyRentMultiplier = 1f,
            leadMultiplier = 1f,
            ancillaryRevenueMultiplier = 1f,
            churnMultiplier = 1f,
            satisfactionTargetOffset = 0f,
            relocationContractSurcharge = 0,
        };
    }
}

[System.Serializable]
public class GymSiteSaveData
{
    public int schemaVersion = 1;
    public int locationType = (int)GymLocationType.Neighborhood;
    public int siteTier = (int)GymSiteTier.Starter8x8;
    public int gridWidth = 8;
    public int gridHeight = 8;

    public static GymSiteSaveData CreateDefault()
    {
        return new GymSiteSaveData();
    }
}

public class GymSiteManager : MonoBehaviour
{
    private static readonly GymLocationType[] PrototypeLocationOrder =
    {
        GymLocationType.Neighborhood,
        GymLocationType.StationArea,
        GymLocationType.Downtown,
        GymLocationType.Premium,
    };

    [Header("Default Starting Site")]
    [SerializeField] private GymLocationType defaultLocationType = GymLocationType.Neighborhood;
    [SerializeField] private GymSiteTier defaultSiteTier = GymSiteTier.Starter8x8;

    [Header("Prototype Availability")]
    [SerializeField] private bool unlock64x64InPrototype = false;
    [SerializeField] private bool unlockPremiumLocationInPrototype = false;

    private GymLocationType currentLocationType;
    private GymSiteTier currentSiteTier;
    private bool isInitialized = false;

    public GymLocationType CurrentLocationType => currentLocationType;
    public GymSiteTier CurrentSiteTier => currentSiteTier;
    public int CurrentGridWidth => GetGridWidthForTier(currentSiteTier);
    public int CurrentGridHeight => GetGridHeightForTier(currentSiteTier);
    public GymLocationPrototypeRules CurrentLocationRules => GetLocationRules(currentLocationType);

    public void InitializeSiteState()
    {
        if (isInitialized)
        {
            return;
        }

        ResetToDefaultSite("기본 시작 부지");
    }

    public void ResetToDefaultSite(string reason = "")
    {
        currentLocationType = SanitizeLocationForPrototype(defaultLocationType);
        currentSiteTier = defaultSiteTier;

        ClampCurrentSiteTierToPrototypeAvailability();

        isInitialized = true;
        LogCurrentSite(reason);
    }

    public void ApplySaveData(GymSiteSaveData saveData, string reason = "")
    {
        if (saveData == null)
        {
            ResetToDefaultSite(string.IsNullOrWhiteSpace(reason)
                ? "저장된 부지 데이터가 없어서 기본값 적용"
                : $"{reason} / 저장된 부지 데이터가 없어서 기본값 적용");
            return;
        }

        currentLocationType = SanitizeLocationForPrototype(ParseLocationType(saveData.locationType));

        GymSiteTier parsedTier;
        if (TryParseSiteTier(saveData.siteTier, out parsedTier))
        {
            currentSiteTier = parsedTier;
        }
        else
        {
            GymSiteTier inferredTier;
            if (TryInferTierFromGridSize(saveData.gridWidth, saveData.gridHeight, out inferredTier))
            {
                currentSiteTier = inferredTier;
            }
            else
            {
                currentSiteTier = defaultSiteTier;
            }
        }

        ClampCurrentSiteTierToPrototypeAvailability();

        isInitialized = true;
        LogCurrentSite(reason);
    }

    public GymSiteSaveData BuildSaveData()
    {
        if (!isInitialized)
        {
            InitializeSiteState();
        }

        return new GymSiteSaveData
        {
            schemaVersion = 1,
            locationType = (int)currentLocationType,
            siteTier = (int)currentSiteTier,
            gridWidth = CurrentGridWidth,
            gridHeight = CurrentGridHeight,
        };
    }

    public void ApplyCurrentSiteToGridManager(GridManager targetGridManager)
    {
        if (targetGridManager == null)
        {
            Debug.LogWarning("[GymSiteManager] GridManager가 없어서 현재 부지를 적용할 수 없어.");
            return;
        }

        if (!isInitialized)
        {
            InitializeSiteState();
        }

        targetGridManager.SetGridSize(
            CurrentGridWidth,
            CurrentGridHeight,
            $"현재 부지 적용: {GetCurrentSiteLabel()}"
        );
    }

    public bool TryGetNextTier(out GymSiteTier nextTier)
    {
        if (!isInitialized)
        {
            InitializeSiteState();
        }

        switch (currentSiteTier)
        {
            case GymSiteTier.Starter8x8:
                nextTier = GymSiteTier.Expansion16x16;
                return true;

            case GymSiteTier.Expansion16x16:
                nextTier = GymSiteTier.FullOpen32x32;
                return true;

            case GymSiteTier.FullOpen32x32:
                if (unlock64x64InPrototype)
                {
                    nextTier = GymSiteTier.Mega64x64;
                    return true;
                }

                nextTier = currentSiteTier;
                return false;

            default:
                nextTier = currentSiteTier;
                return false;
        }
    }

    public void PromoteToTier(GymSiteTier targetTier, string reason = "")
    {
        if (!isInitialized)
        {
            InitializeSiteState();
        }

        currentSiteTier = targetTier;
        ClampCurrentSiteTierToPrototypeAvailability();

        LogCurrentSite(reason);
    }

    public void SetCurrentLocation(GymLocationType targetLocation, string reason = "")
    {
        if (!isInitialized)
        {
            InitializeSiteState();
        }

        currentLocationType = SanitizeLocationForPrototype(targetLocation);
        LogCurrentSite(reason);
    }

    public GymLocationPrototypeRules GetCurrentLocationRules()
    {
        if (!isInitialized)
        {
            InitializeSiteState();
        }

        return GetLocationRules(currentLocationType);
    }

    public GymLocationPrototypeRules GetLocationRules(GymLocationType type)
    {
        GymLocationType sanitized = SanitizeLocationForPrototype(type);

        switch (sanitized)
        {
            case GymLocationType.Neighborhood:
                return new GymLocationPrototypeRules
                {
                    locationType = GymLocationType.Neighborhood,
                    monthlyRentFlatAdditive = 0,
                    monthlyRentMultiplier = 1.00f,
                    leadMultiplier = 0.92f,
                    ancillaryRevenueMultiplier = 0.95f,
                    churnMultiplier = 0.90f,
                    satisfactionTargetOffset = 0.02f,
                    relocationContractSurcharge = 0,
                };

            case GymLocationType.StationArea:
                return new GymLocationPrototypeRules
                {
                    locationType = GymLocationType.StationArea,
                    monthlyRentFlatAdditive = 250,
                    monthlyRentMultiplier = 1.08f,
                    leadMultiplier = 1.08f,
                    ancillaryRevenueMultiplier = 1.05f,
                    churnMultiplier = 1.00f,
                    satisfactionTargetOffset = 0.00f,
                    relocationContractSurcharge = 5000,
                };

            case GymLocationType.Downtown:
                return new GymLocationPrototypeRules
                {
                    locationType = GymLocationType.Downtown,
                    monthlyRentFlatAdditive = 800,
                    monthlyRentMultiplier = 1.18f,
                    leadMultiplier = 1.18f,
                    ancillaryRevenueMultiplier = 1.12f,
                    churnMultiplier = 1.12f,
                    satisfactionTargetOffset = -0.02f,
                    relocationContractSurcharge = 18000,
                };

            case GymLocationType.Premium:
                return new GymLocationPrototypeRules
                {
                    locationType = GymLocationType.Premium,
                    monthlyRentFlatAdditive = 1600,
                    monthlyRentMultiplier = 1.30f,
                    leadMultiplier = 1.05f,
                    ancillaryRevenueMultiplier = 1.22f,
                    churnMultiplier = 1.03f,
                    satisfactionTargetOffset = 0.01f,
                    relocationContractSurcharge = 40000,
                };

            default:
                return GymLocationPrototypeRules.CreateDefault();
        }
    }

    public bool IsLocationAvailableInPrototype(GymLocationType type)
    {
        switch (type)
        {
            case GymLocationType.Neighborhood:
            case GymLocationType.StationArea:
            case GymLocationType.Downtown:
                return true;

            case GymLocationType.Premium:
                return unlockPremiumLocationInPrototype;

            default:
                return false;
        }
    }

    public GymLocationType SanitizeLocationForPrototype(GymLocationType type)
    {
        if (IsLocationAvailableInPrototype(type))
        {
            return type;
        }

        if (type == GymLocationType.Premium && !unlockPremiumLocationInPrototype)
        {
            return GymLocationType.Downtown;
        }

        return GymLocationType.Neighborhood;
    }

    public bool TryGetAdjacentSelectableLocation(GymLocationType currentSelection, int direction, out GymLocationType nextLocation)
    {
        nextLocation = SanitizeLocationForPrototype(currentSelection);

        int step = direction >= 0 ? 1 : -1;
        int currentIndex = GetLocationOrderIndex(nextLocation);

        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        for (int i = 1; i <= PrototypeLocationOrder.Length; i++)
        {
            int candidateIndex = (currentIndex + (step * i) + PrototypeLocationOrder.Length) % PrototypeLocationOrder.Length;
            GymLocationType candidate = PrototypeLocationOrder[candidateIndex];

            if (!IsLocationAvailableInPrototype(candidate))
            {
                continue;
            }

            nextLocation = candidate;
            return candidate != currentSelection;
        }

        return false;
    }

    public string GetCurrentSiteLabel()
    {
        if (!isInitialized)
        {
            InitializeSiteState();
        }

        return GetSiteLabel(currentLocationType, currentSiteTier);
    }

    public string GetSiteLabelForTier(GymSiteTier tier)
    {
        return GetSiteLabel(currentLocationType, tier);
    }

    public string GetSiteLabel(GymLocationType locationType, GymSiteTier tier)
    {
        return $"{GetLocationDisplayName(locationType)} / {GetSiteTierDisplayName(tier)}";
    }

    public string GetCurrentLocationSummary()
    {
        if (!isInitialized)
        {
            InitializeSiteState();
        }

        return GetLocationSummary(currentLocationType);
    }

    public string GetLocationSummary(GymLocationType type)
    {
        GymLocationType sanitized = SanitizeLocationForPrototype(type);

        switch (sanitized)
        {
            case GymLocationType.Neighborhood:
                return "리스크 낮음 / 유입 낮~중 / 월세 낮음";

            case GymLocationType.StationArea:
                return "밸런스형 / 유입 중~높음 / 월세 중간";

            case GymLocationType.Downtown:
                return "고위험·고수익 / 유입 높음 / 월세 높음";

            case GymLocationType.Premium:
                return "상류층 중심 / 객단가 높음 / 월세 매우 높음";

            default:
                return "리스크 낮음 / 유입 낮~중 / 월세 낮음";
        }
    }

    public static int GetGridWidthForTier(GymSiteTier tier)
    {
        switch (tier)
        {
            case GymSiteTier.Starter8x8:
                return 8;

            case GymSiteTier.Expansion16x16:
                return 16;

            case GymSiteTier.FullOpen32x32:
                return 32;

            case GymSiteTier.Mega64x64:
                return 64;

            default:
                return 8;
        }
    }

    public static int GetGridHeightForTier(GymSiteTier tier)
    {
        switch (tier)
        {
            case GymSiteTier.Starter8x8:
                return 8;

            case GymSiteTier.Expansion16x16:
                return 16;

            case GymSiteTier.FullOpen32x32:
                return 32;

            case GymSiteTier.Mega64x64:
                return 64;

            default:
                return 8;
        }
    }

    public static string GetLocationDisplayName(GymLocationType type)
    {
        switch (type)
        {
            case GymLocationType.Neighborhood:
                return "동네";

            case GymLocationType.StationArea:
                return "역세권";

            case GymLocationType.Downtown:
                return "번화가";

            case GymLocationType.Premium:
                return "부촌";

            default:
                return "동네";
        }
    }

    public static string GetSiteTierDisplayName(GymSiteTier tier)
    {
        switch (tier)
        {
            case GymSiteTier.Starter8x8:
                return "8x8";

            case GymSiteTier.Expansion16x16:
                return "16x16";

            case GymSiteTier.FullOpen32x32:
                return "32x32";

            case GymSiteTier.Mega64x64:
                return "64x64";

            default:
                return "8x8";
        }
    }

    private void ClampCurrentSiteTierToPrototypeAvailability()
    {
        if (!unlock64x64InPrototype && currentSiteTier == GymSiteTier.Mega64x64)
        {
            Debug.LogWarning("[GymSiteManager] 64x64는 아직 업데이트 범위라서 현재 프로토타입에서는 32x32까지만 사용해.");
            currentSiteTier = GymSiteTier.FullOpen32x32;
        }
    }

    private void LogCurrentSite(string reason)
    {
        string reasonSuffix = string.IsNullOrWhiteSpace(reason)
            ? string.Empty
            : $" / 사유: {reason}";

        Debug.Log($"[GymSiteManager] 현재 부지 = {GetCurrentSiteLabel()}{reasonSuffix}");
    }

    private static GymLocationType ParseLocationType(int rawValue)
    {
        switch (rawValue)
        {
            case (int)GymLocationType.Neighborhood:
                return GymLocationType.Neighborhood;

            case (int)GymLocationType.StationArea:
                return GymLocationType.StationArea;

            case (int)GymLocationType.Downtown:
                return GymLocationType.Downtown;

            case (int)GymLocationType.Premium:
                return GymLocationType.Premium;

            default:
                return GymLocationType.Neighborhood;
        }
    }

    private static bool TryParseSiteTier(int rawValue, out GymSiteTier siteTier)
    {
        switch (rawValue)
        {
            case (int)GymSiteTier.Starter8x8:
                siteTier = GymSiteTier.Starter8x8;
                return true;

            case (int)GymSiteTier.Expansion16x16:
                siteTier = GymSiteTier.Expansion16x16;
                return true;

            case (int)GymSiteTier.FullOpen32x32:
                siteTier = GymSiteTier.FullOpen32x32;
                return true;

            case (int)GymSiteTier.Mega64x64:
                siteTier = GymSiteTier.Mega64x64;
                return true;

            default:
                siteTier = GymSiteTier.Starter8x8;
                return false;
        }
    }

    private static bool TryInferTierFromGridSize(int gridWidth, int gridHeight, out GymSiteTier siteTier)
    {
        if (gridWidth == 8 && gridHeight == 8)
        {
            siteTier = GymSiteTier.Starter8x8;
            return true;
        }

        if (gridWidth == 16 && gridHeight == 16)
        {
            siteTier = GymSiteTier.Expansion16x16;
            return true;
        }

        if (gridWidth == 32 && gridHeight == 32)
        {
            siteTier = GymSiteTier.FullOpen32x32;
            return true;
        }

        if (gridWidth == 64 && gridHeight == 64)
        {
            siteTier = GymSiteTier.Mega64x64;
            return true;
        }

        siteTier = GymSiteTier.Starter8x8;
        return false;
    }

    private static int GetLocationOrderIndex(GymLocationType locationType)
    {
        for (int i = 0; i < PrototypeLocationOrder.Length; i++)
        {
            if (PrototypeLocationOrder[i] == locationType)
            {
                return i;
            }
        }

        return -1;
    }
}