using Microsoft.Extensions.Configuration; // Pour lire la config (chaîne de connexion)
using Microsoft.Data.SqlClient;             // Le client ADO.NET
using SkillMatrix.Core.DTOs;

public class AdoNetService
{
    private readonly string _connectionString;

    // 1. Injection de la configuration pour obtenir la chaîne de connexion
    public AdoNetService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // 2. Méthode pour lire les données brutes (sans EF Core)
    public async Task<(List<ConsultantListingDto> Consultants, int TotalCount)> GetConsultantsAsync(int page = 1, int pageSize = 3)
    {
        var consultants = new List<ConsultantListingDto>();
        int totalCount = 0;

        // Calculer le nombre de lignes à sauter
        int skip = (page - 1) * pageSize;

        // 🛑 1. REQUÊTE POUR OBTENIR LE COMPTE TOTAL (pour l'affichage "Page 1 de X")
        string countSql = "SELECT COUNT(Id) FROM Consultants";

        // 🛑 Requête SQL brute (sans filtre de pagination pour l'instant)
        string sql = $@"
            SELECT Id, Nom, Prenom, Titre, Statut 
            FROM Consultants
            ORDER BY Nom
            OFFSET {skip} ROWS
            FETCH NEXT {pageSize} ROWS ONLY";

            // 3. Utilisation de l'objet de connexion ADO.NET
            using (var connection = new SqlConnection(_connectionString))
            {
            await connection.OpenAsync();

            // --- EXÉCUTION DU COUNT ---
            using (var countCommand = new SqlCommand(countSql, connection))
            {
                // ExecuteScalar renvoie la première colonne de la première ligne
                totalCount = (int)await countCommand.ExecuteScalarAsync();
            }

            using (var command = new SqlCommand(sql, connection))
            {
                // 4. Utilisation du Data Reader pour lire ligne par ligne
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        consultants.Add(new ConsultantListingDto
                        {
                            Id = reader.GetInt32(0),
                            NomComplet = reader.GetString(2) + " " + reader.GetString(1), // Prenom + Nom
                            Titre = reader.GetString(3),
                            //DescriptionCourte = reader.GetString(4).Substring(0, 50) + "...", // Tronqué
                            Statut = reader.GetString(4) // 🛑 Récupération de la logique métier
                        });
                    }
                }
            }
        }
        return (consultants, totalCount);
    }
}