using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StaffAiManager : MonoBehaviour
{
    [System.Serializable]
    public class ActiveStaff
    {
        public StaffData data;
        public GameObject gameObject;
        public SpriteRenderer renderer;
        public Vector3 targetPosition;
        public Vector3 routedTargetPosition;
        public readonly List<Vector3> routeWaypoints = new List<Vector3>();
        public int routeWaypointIndex;
        public float idleTimer;
        public float actionTimer;
        public CustomerFlowManager.ActiveCustomer targetCustomer;
    }

    [SerializeField] private StaffManager staffManager;
    [SerializeField] private CustomerFlowManager customerFlowManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TimeManager timeManager;

    [Header("Visuals")]
    public Color receptionistColor = new Color(1f, 0.95f, 0.6f); // Yellowish
    public Color trainerColor = new Color(1f, 0.5f, 0.5f); // Reddish
    public Color cleanerColor = new Color(0.5f, 0.8f, 1f); // Bluish
    public Vector3 scale = new Vector3(0.55f, 0.55f, 1f);
    public float moveSpeed = 2.8f;

    private List<ActiveStaff> activeStaffList = new List<ActiveStaff>();
    private Transform staffRoot;
    private Sprite defaultSprite;
    private float ptSyncTimer = 0f;
    private const float PtSyncInterval = 3f; // 3초마다 PT 배정 동기화

    private void Start()
    {
        staffRoot = new GameObject("StaffRoot").transform;
        staffRoot.SetParent(transform, false);

        if (staffManager == null) staffManager = FindObjectOfType<StaffManager>();
        if (customerFlowManager == null) customerFlowManager = FindObjectOfType<CustomerFlowManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (timeManager == null) timeManager = FindObjectOfType<TimeManager>();

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        defaultSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void Update()
    {
        SyncStaffList();

        if (timeManager != null && timeManager.IsPaused) return;

        float dt = timeManager != null ? timeManager.GetSimulationDeltaTime() : Time.deltaTime;

        // PT 배정 수 동기화 (실제 isPtCustomer 손님 기반)
        ptSyncTimer += Time.deltaTime;
        if (ptSyncTimer >= PtSyncInterval)
        {
            ptSyncTimer = 0f;
            if (staffManager != null && customerFlowManager != null)
            {
                staffManager.SyncPtMemberCounts(customerFlowManager.activeCustomers);
            }
        }

        UpdateStaffAI(dt);
    }

    private void SyncStaffList()
    {
        if (staffManager == null) return;
        var hiredList = staffManager.HiredStaff;

        // Remove fired staff
        for (int i = activeStaffList.Count - 1; i >= 0; i--)
        {
            var active = activeStaffList[i];
            if (!hiredList.Any(s => s.staffId == active.data.staffId))
            {
                if (active.gameObject != null) Destroy(active.gameObject);
                activeStaffList.RemoveAt(i);
            }
        }

        // Add newly hired staff
        foreach (var staff in hiredList)
        {
            if (!activeStaffList.Any(a => a.data.staffId == staff.staffId))
            {
                SpawnStaff(staff);
            }
        }
    }

    private void SpawnStaff(StaffData data)
    {
        GameObject go = new GameObject($"Staff_{data.role}_{data.staffName}");
        go.transform.SetParent(staffRoot, false);
        
        Vector3 spawnPos = GetStaffSpawnPosOrDefault();
        go.transform.position = spawnPos;
        go.transform.localScale = scale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = defaultSprite;
        sr.sortingOrder = 15; // Above machines and potentially customers

        if (data.role == StaffRole.Receptionist) sr.color = receptionistColor;
        else if (data.role == StaffRole.Trainer) sr.color = trainerColor;
        else sr.color = cleanerColor;

        ActiveStaff newStaff = new ActiveStaff
        {
            data = data,
            gameObject = go,
            renderer = sr,
            targetPosition = spawnPos,
            routedTargetPosition = spawnPos
        };

        activeStaffList.Add(newStaff);
    }

    private void UpdateStaffAI(float dt)
    {
        foreach (var staff in activeStaffList)
        {
            if (staff.gameObject == null) continue;

            if (staff.data.role == StaffRole.Receptionist) UpdateReceptionist(staff, dt);
            else if (staff.data.role == StaffRole.Trainer) UpdateTrainer(staff, dt);
            else if (staff.data.role == StaffRole.Cleaner) UpdateCleaner(staff, dt);

            // Move smoothly towards target
            float currentSpeed = staff.data.role == StaffRole.Trainer ? moveSpeed * 1.6f : moveSpeed;
            MoveStaffAlongRoute(staff, currentSpeed * dt);
        }
    }

    private void UpdateReceptionist(ActiveStaff staff, float dt)
    {
        // Stand near entrance desk area
        staff.targetPosition = GetReceptionistWorkPosOrDefault();
    }

    private void UpdateTrainer(ActiveStaff staff, float dt)
    {
        if (staff.actionTimer > 0f)
        {
            staff.actionTimer -= dt;
            return;
        }

        var myDedicatedCustomer = customerFlowManager != null 
            ? customerFlowManager.activeCustomers.FirstOrDefault(c => c.isPtCustomer && c.assignedTrainerId == staff.data.staffId && c.state != CustomerFlowManager.CustomerState.Leaving)
            : null;

        if (myDedicatedCustomer != null)
        {
            staff.targetCustomer = myDedicatedCustomer;
            
            if (myDedicatedCustomer.state == CustomerFlowManager.CustomerState.UsingMachine)
            {
                staff.targetPosition = myDedicatedCustomer.targetMachineWorldPosition + new Vector3(0.5f, 0.4f, 0f);
                if (Vector3.Distance(staff.gameObject.transform.position, staff.targetPosition) < 0.1f)
                    staff.actionTimer = 2.5f;
            }
            else
            {
                if (myDedicatedCustomer.visual != null)
                    staff.targetPosition = myDedicatedCustomer.visual.transform.position + new Vector3(0.5f, 0.5f, 0f);
            }
            return;
        }

        if (staff.targetCustomer != null && staff.targetCustomer.state != CustomerFlowManager.CustomerState.UsingMachine)
        {
            staff.targetCustomer = null; 
        }

        if (staff.targetCustomer != null)
        {
            staff.targetPosition = staff.targetCustomer.targetMachineWorldPosition + new Vector3(0.5f, 0.4f, 0f);
            if (Vector3.Distance(staff.gameObject.transform.position, staff.targetPosition) < 0.1f)
                staff.actionTimer = 2.5f;
        }
        else
        {
            if (customerFlowManager != null)
            {
                var possibleTargets = customerFlowManager.activeCustomers
                    .Where(c => c.state == CustomerFlowManager.CustomerState.UsingMachine &&
                                !c.isPtCustomer && 
                                !activeStaffList.Any(s => s != staff && s.targetCustomer == c))
                    .ToList();

                if (possibleTargets.Count > 0)
                {
                    staff.targetCustomer = possibleTargets[Random.Range(0, possibleTargets.Count)];
                }
                else
                {
                    if (Vector3.Distance(staff.gameObject.transform.position, staff.targetPosition) < 0.1f)
                    {
                        staff.targetPosition = GetRandomFloorPos();
                        staff.actionTimer = 1.5f;
                    }
                }
            }
        }
    }

    private void UpdateCleaner(ActiveStaff staff, float dt)
    {
        if (staff.actionTimer > 0f)
        {
            staff.actionTimer -= dt;
            return;
        }

        if (Vector3.Distance(staff.gameObject.transform.position, staff.targetPosition) < 0.1f)
        {
            staff.actionTimer = RandRange(1.5f, 3.5f);
            staff.targetPosition = GetRandomFloorPos();
        }
    }

    private float RandRange(float min, float max) => Random.Range(min, max);

    private Vector3 GetRandomFloorPos()
    {
        if (gridManager == null) return Vector3.zero;
        for (int attempt = 0; attempt < 32; attempt++)
        {
            int x = Random.Range(0, gridManager.Width);
            int y = Random.Range(0, gridManager.Height);
            if (gridManager.IsAreaAvailable(x, y, 1, 1))
            {
                return gridManager.GetAreaCenterWorldPosition(x, y, 1, 1);
            }
        }

        return GetStaffSpawnPosOrDefault();
    }

    private Vector3 GetStaffSpawnPosOrDefault()
    {
        if (gridManager != null && gridManager.TryGetDefaultStaffSpawnWorldPosition(out Vector3 spawnPosition))
        {
            return spawnPosition;
        }

        return Vector3.zero;
    }

    private Vector3 GetReceptionistWorkPosOrDefault()
    {
        if (gridManager != null && gridManager.TryGetDefaultEntrancePassCell(out int entranceX, out int entranceY))
        {
            int preferredX = Mathf.Clamp(entranceX + 2, 0, Mathf.Max(0, gridManager.Width - 1));
            int preferredY = Mathf.Clamp(entranceY + 1, 0, Mathf.Max(0, gridManager.Height - 1));
            Vector3 preferredWorld = gridManager.GetAreaCenterWorldPosition(preferredX, preferredY, 1, 1);
            if (TryFindNearestAvailableCell(preferredWorld, out Vector2Int workCell))
            {
                return gridManager.GetAreaCenterWorldPosition(workCell.x, workCell.y, 1, 1);
            }
        }

        return GetStaffSpawnPosOrDefault();
    }

    private void MoveStaffAlongRoute(ActiveStaff staff, float moveStep)
    {
        if (staff == null || staff.gameObject == null)
        {
            return;
        }

        if (ShouldRebuildStaffRoute(staff))
        {
            SetStaffRoute(staff, BuildStaffRoute(staff.gameObject.transform.position, staff.targetPosition));
        }

        if (staff.routeWaypoints.Count <= 0 || staff.routeWaypointIndex >= staff.routeWaypoints.Count)
        {
            if (gridManager != null)
            {
                return;
            }

            staff.gameObject.transform.position = Vector3.MoveTowards(
                staff.gameObject.transform.position,
                staff.targetPosition,
                moveStep);
            return;
        }

        Vector3 waypoint = staff.routeWaypoints[staff.routeWaypointIndex];
        float distance = Vector3.Distance(staff.gameObject.transform.position, waypoint);
        if (distance <= moveStep)
        {
            staff.gameObject.transform.position = waypoint;
            staff.routeWaypointIndex += 1;
        }
        else
        {
            staff.gameObject.transform.position = Vector3.MoveTowards(
                staff.gameObject.transform.position,
                waypoint,
                moveStep);
        }
    }

    private bool ShouldRebuildStaffRoute(ActiveStaff staff)
    {
        if (staff == null)
        {
            return false;
        }

        if (staff.routeWaypoints.Count <= 0 || staff.routeWaypointIndex >= staff.routeWaypoints.Count)
        {
            return true;
        }

        float rebuildDistance = gridManager != null ? Mathf.Max(0.2f, gridManager.CellSize * 0.25f) : 0.2f;
        return Vector3.Distance(staff.routedTargetPosition, staff.targetPosition) > rebuildDistance;
    }

    private void SetStaffRoute(ActiveStaff staff, List<Vector3> waypoints)
    {
        if (staff == null)
        {
            return;
        }

        staff.routeWaypoints.Clear();
        staff.routeWaypointIndex = 0;
        staff.routedTargetPosition = staff.targetPosition;

        if (waypoints == null)
        {
            return;
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            AddStaffWaypointIfDistinct(staff.routeWaypoints, waypoints[i]);
        }
    }

    private List<Vector3> BuildStaffRoute(Vector3 startWorld, Vector3 targetWorld)
    {
        List<Vector3> route = new List<Vector3>();
        if (gridManager == null)
        {
            AddStaffWaypointIfDistinct(route, targetWorld);
            return route;
        }

        if (gridManager.TryGetCellIndexFromWorldPosition(startWorld, out int startX, out int startY) &&
            gridManager.TryGetCellIndexFromWorldPosition(targetWorld, out int targetX, out int targetY))
        {
            var pathCells = AStarPathfinder.FindPath(gridManager, new Vector2Int(startX, startY), new Vector2Int(targetX, targetY), true);
            if (pathCells != null && pathCells.Count > 0)
            {
                foreach (var cell in pathCells)
                {
                    AddStaffWaypointIfDistinct(route, gridManager.GetAreaCenterWorldPosition(cell.x, cell.y, 1, 1));
                }
                if (route.Count > 0)
                {
                    route[route.Count - 1] = targetWorld;
                }
                return route;
            }
        }

        AddStaffWaypointIfDistinct(route, targetWorld);
        return route;
    }



    private bool TryFindNearestAvailableCell(Vector3 worldPosition, out Vector2Int cell)
    {
        cell = default;
        if (gridManager == null)
        {
            return false;
        }

        float bestDistance = float.PositiveInfinity;
        bool found = false;

        for (int y = 0; y < gridManager.Height; y++)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                if (!gridManager.IsAreaAvailable(x, y, 1, 1))
                {
                    continue;
                }

                Vector3 cellWorld = gridManager.GetAreaCenterWorldPosition(x, y, 1, 1);
                float distance = (cellWorld - worldPosition).sqrMagnitude;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                cell = new Vector2Int(x, y);
                found = true;
            }
        }

        return found;
    }

    private static void AddStaffWaypointIfDistinct(List<Vector3> route, Vector3 waypoint)
    {
        if (route == null)
        {
            return;
        }

        if (route.Count > 0 && Vector3.Distance(route[route.Count - 1], waypoint) <= 0.01f)
        {
            return;
        }

        route.Add(waypoint);
    }
}

