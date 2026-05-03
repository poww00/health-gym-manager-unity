using System;

[System.Serializable]
public enum StaffRole
{
    Receptionist, // 안내원 
    Trainer,      // 트레이너
    Cleaner       // 청소부
}

[System.Serializable]
public enum StaffGender
{
    Male,
    Female
}

[System.Serializable]
public class StaffData
{
    public string staffId;
    public string staffName;
    public StaffRole role;
    public StaffGender gender;
    public int portraitIndex;
    
    // 외모 스탯: 안내원(회원권 유입률), 트레이너(PT 신청 확률)
    public int looks;
    
    // 지도력 스탯: 트레이너(PT 단가 높임, 만족도 상승폭 기여)
    public int leadership;
    
    // 청소 숙련도: 청소부(청결도 회복 속도/방어력)
    public int cleaningSkill;

    public int monthlySalary;
    public int ptMemberCount; // PT 회원 수

    public StaffData(string name, StaffRole role, int monthlySalary, int looks = 0, int leadership = 0, int cleaningSkill = 0, StaffGender gender = StaffGender.Male, int portraitIndex = 0)
    {
        this.staffId = Guid.NewGuid().ToString();
        this.staffName = name;
        this.role = role;
        this.gender = gender;
        this.portraitIndex = portraitIndex;
        this.monthlySalary = monthlySalary;
        this.looks = looks;
        this.leadership = leadership;
        this.cleaningSkill = cleaningSkill;
    }
}

