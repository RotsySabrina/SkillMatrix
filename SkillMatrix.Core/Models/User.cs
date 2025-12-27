namespace SkillMatrix.Core.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = ""; // On ne stocke JAMAIS le mot de passe en clair
        public string NomComplet { get; set; } = "";
        public string Role { get; set; } = "User"; // Admin, Recruteur, User
        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}