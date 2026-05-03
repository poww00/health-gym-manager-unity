using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StaffManager : MonoBehaviour
{
    private static readonly string[] MaleApplicantNames =
    {
        "박도윤",
        "이준호",
        "최현우",
        "정민재",
        "강태오",
        "한지훈",
        "오서준",
        "윤재민"
    };

    private static readonly string[] FemaleApplicantNames =
    {
        "김하윤",
        "박서연",
        "이지아",
        "최유진",
        "정다은",
        "강수아",
        "한채원",
        "윤소희"
    };

    private readonly List<StaffData> hiredStaff = new List<StaffData>();
    private readonly List<StaffData> availableApplicants = new List<StaffData>();

    public event System.Action ApplicantsChanged;
    public event System.Action HiredStaffChanged;

    public IReadOnlyList<StaffData> HiredStaff => hiredStaff.AsReadOnly();
    public IReadOnlyList<StaffData> AvailableApplicants => availableApplicants.AsReadOnly();

    private void Start()
    {
        TimeManager timeManager = FindFirstObjectByType<TimeManager>();
        if (timeManager != null)
        {
            timeManager.DayChanged += OnDayChanged;
        }

        RefreshApplicants();
    }

    private void OnDayChanged(int currentDay)
    {
        RefreshApplicants();
    }

    public void RefreshApplicants()
    {
        availableApplicants.Clear();

        int newCount = Random.Range(3, 6);
        for (int i = 0; i < newCount; i++)
        {
            availableApplicants.Add(GenerateRandomApplication());
        }

        ApplicantsChanged?.Invoke();
    }

    public void HireApplicant(StaffData app)
    {
        if (app == null || !availableApplicants.Contains(app))
        {
            return;
        }

        availableApplicants.Remove(app);
        HireStaff(app);
        ApplicantsChanged?.Invoke();
    }

    public void SyncPtMemberCounts(IReadOnlyList<CustomerFlowManager.ActiveCustomer> activeCustomers)
    {
        foreach (StaffData staff in hiredStaff)
        {
            if (staff.role == StaffRole.Trainer)
            {
                staff.ptMemberCount = 0;
            }
        }

        if (activeCustomers == null)
        {
            return;
        }

        foreach (CustomerFlowManager.ActiveCustomer customer in activeCustomers)
        {
            if (!customer.isPtCustomer || string.IsNullOrEmpty(customer.assignedTrainerId))
            {
                continue;
            }

            if (customer.state == CustomerFlowManager.CustomerState.Leaving)
            {
                continue;
            }

            StaffData trainer = hiredStaff.FirstOrDefault(s => s.staffId == customer.assignedTrainerId);
            if (trainer != null)
            {
                trainer.ptMemberCount++;
            }
        }
    }

    public int GetTotalMonthlySalary()
    {
        return hiredStaff.Sum(s => s.monthlySalary);
    }

    public void LoadStaff(List<StaffData> staffList)
    {
        hiredStaff.Clear();
        if (staffList != null)
        {
            for (int i = 0; i < staffList.Count; i++)
            {
                NormalizeStaffIdentity(staffList[i]);
                hiredStaff.Add(staffList[i]);
            }
        }

        HiredStaffChanged?.Invoke();
    }

    public void HireStaff(StaffData staff)
    {
        if (staff == null)
        {
            return;
        }

        NormalizeStaffIdentity(staff);
        hiredStaff.Add(staff);
        Debug.Log($"[StaffManager] Hired: {staff.staffName} ({staff.role}) / Salary: {staff.monthlySalary}");
        HiredStaffChanged?.Invoke();
    }

    public void FireStaff(string staffId)
    {
        StaffData staff = hiredStaff.FirstOrDefault(s => s.staffId == staffId);
        if (staff == null)
        {
            return;
        }

        hiredStaff.Remove(staff);
        Debug.Log($"[StaffManager] Fired: {staff.staffName}");
        HiredStaffChanged?.Invoke();
    }

    public int GetTotalReceptionistLooks()
    {
        return hiredStaff.Where(s => s.role == StaffRole.Receptionist).Sum(s => s.looks);
    }

    public int GetTotalTrainerLeadership()
    {
        return hiredStaff.Where(s => s.role == StaffRole.Trainer).Sum(s => s.leadership);
    }

    public int GetTotalTrainerLooks()
    {
        return hiredStaff.Where(s => s.role == StaffRole.Trainer).Sum(s => s.looks);
    }

    public int GetTotalCleaningSkill()
    {
        return hiredStaff.Where(s => s.role == StaffRole.Cleaner).Sum(s => s.cleaningSkill);
    }

    public int GetHiredTrainerCount()
    {
        return hiredStaff.Count(s => s.role == StaffRole.Trainer);
    }

    public StaffData GenerateRandomApplication()
    {
        StaffRole randomRole = (StaffRole)Random.Range(0, 3);
        StaffGender randomGender = Random.value < 0.5f ? StaffGender.Male : StaffGender.Female;
        string[] names = randomGender == StaffGender.Female ? FemaleApplicantNames : MaleApplicantNames;
        string randomName = names[Random.Range(0, names.Length)];
        int portraitIndex = Random.Range(0, 10);

        int looks = 0;
        int leadership = 0;
        int cleaning = 0;
        int salary = Random.Range(100, 300);

        if (randomRole == StaffRole.Receptionist)
        {
            looks = Random.Range(1, 10);
        }
        else if (randomRole == StaffRole.Trainer)
        {
            looks = Random.Range(1, 10);
            leadership = Random.Range(1, 10);
        }
        else if (randomRole == StaffRole.Cleaner)
        {
            cleaning = Random.Range(1, 10);
        }

        return new StaffData(randomName, randomRole, salary, looks, leadership, cleaning, randomGender, portraitIndex);
    }

    public string GetRoleNameKOR(StaffRole role)
    {
        if (role == StaffRole.Receptionist) return "안내";
        if (role == StaffRole.Trainer) return "트레이너";
        return "청소부";
    }

    private static void NormalizeStaffIdentity(StaffData staff)
    {
        if (staff == null)
        {
            return;
        }

        if (staff.staffName == "김국일")
        {
            staff.gender = StaffGender.Male;
            staff.staffName = MaleApplicantNames[0];
        }

        staff.portraitIndex = Mathf.Clamp(staff.portraitIndex, 0, 9);
    }
}
