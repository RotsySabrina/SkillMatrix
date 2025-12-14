public class SearchConsultantDto
{
    public int Id { get; set; } // Identifiant pour lier au consultant SQL
    public string NomComplet { get; set; } // Prénom + Nom (pour la recherche facile)
    public string Titre { get; set; }
    public string Statut { get; set; }

    // 🛑 CRITIQUE : Champ analysé pour la recherche textuelle
    public string DescriptionProfil { get; set; }

    // Liste plate des noms de compétences pour la recherche par facettes
    public List<string> Competences { get; set; } = new List<string>();
}