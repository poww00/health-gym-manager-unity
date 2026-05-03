using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int year = 1;
    public int month = 1;
    public int day = 1;

    public int cash;
    public string lastSettlementText;
    public GymSiteSaveData siteState = GymSiteSaveData.CreateDefault();
    public List<PlacedObjectSaveData> placedObjects = new List<PlacedObjectSaveData>();
    public int totalMaintenanceCost;
    public int currentStarCoin;
    public List<StaffData> hiredStaff = new List<StaffData>();
}
