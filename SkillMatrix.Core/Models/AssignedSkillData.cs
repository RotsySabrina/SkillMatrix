public class AssignedSkillData
{
    public int SkillId { get; set; }
    public string Name { get; set; }
    public bool Assigned { get; set; } // Est-ce que ce consultant a cette compétence ?
    public int? Level { get; set; }    // Niveau du consultant pour cette compétence
}