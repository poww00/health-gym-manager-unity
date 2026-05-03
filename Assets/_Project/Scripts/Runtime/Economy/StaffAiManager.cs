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
        
        Vector3 spawnPos = GetEntryPosOrDefault();
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
            targetPosition = spawnPos
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
            staff.gameObject.transform.position = Vector3.MoveTowards(
                staff.gameObject.transform.position, 
                staff.targetPosition, 
                currentSpeed * dt
            );
        }
    }

    private void UpdateReceptionist(ActiveStaff staff, float dt)
    {
        Vector3 entry = GetEntryPosOrDefault();
        // Stand near entrance desk area
        staff.targetPosition = new Vector3(entry.x + 1.2f, entry.y + 0.5f, 0f); 
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

    private Vector3 GetEntryPosOrDefault()
    {
        if (gridManager == null) return Vector3.zero;
        float hw = gridManager.Width * gridManager.CellSize * 0.5f;
        float hh = gridManager.Height * gridManager.CellSize * 0.5f;
        return new Vector3(-hw - 0.5f, -hh + (gridManager.Height * gridManager.CellSize * 0.25f), 0f);
    }

    private Vector3 GetRandomFloorPos()
    {
        if (gridManager == null) return Vector3.zero;
        float hw = gridManager.Width * gridManager.CellSize * 0.5f;
        float hh = gridManager.Height * gridManager.CellSize * 0.5f;
        float rX = Random.Range(-hw + 1f, hw - 1f);
        float rY = Random.Range(-hh + 1f, hh - 1f);
        return new Vector3(rX, rY, 0f);
    }
}

