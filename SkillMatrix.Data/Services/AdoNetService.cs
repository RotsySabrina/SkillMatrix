using Microsoft.Extensions.Configuration; // Pour lire la config (chaîne de connexion)
using Microsoft.Data.SqlClient;             // Le client ADO.NET
using SkillMatrix.Core.DTOs;
using SkillMatrix.Core.Models;
using SkillMatrix.Core.ViewModels;

public class AdoNetService
{
    private readonly string _connectionString;

    // 1. Injection de la configuration pour obtenir la chaîne de connexion
    public AdoNetService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

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

    public async Task<Consultant?> GetConsultantDetailsAsync(int id)
    {
        Consultant? consultant = null;

        // Requête pour récupérer le consultant ET ses skills via des JOIN
        string sql = @"
            SELECT c.Id, c.Nom, c.Prenom, c.Titre, c.ExperienceTotale, c.Statut,
                s.Nom as SkillNom, cs.Niveau
            FROM Consultants c
            LEFT JOIN ConsultantSkills cs ON c.Id = cs.ConsultantId
            LEFT JOIN Skills s ON cs.SkillId = s.Id
            WHERE c.Id = @id";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (consultant == null)
                        {
                            consultant = new Consultant
                            {
                                Id = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Prenom = reader.GetString(2),
                                Titre = reader.GetString(3),
                                ExperienceTotale = reader.GetInt32(4),
                                Statut = reader.GetString(5),
                                ConsultantSkills = new List<ConsultantSkill>()
                            };
                        }

                        // Si une compétence existe (LEFT JOIN peut retourner null)
                        if (!reader.IsDBNull(6)) 
                        {
                            consultant.ConsultantSkills.Add(new ConsultantSkill
                            {
                                Skill = new Skill { Nom = reader.GetString(6) },
                                Niveau = reader.GetInt32(7)
                            });
                        }
                    }
                }
            }
        }
        return consultant;
    }

    public async Task<DashboardViewModel> GetDashboardStatsAsync()
    {
        var stats = new DashboardViewModel();
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 1. Compteurs par statut
            string sqlStats = @"
                SELECT Statut, COUNT(*) as Total 
                FROM Consultants 
                GROUP BY Statut;
                
                SELECT TOP 5 s.Nom, COUNT(cs.ConsultantId) as UsageCount
                FROM Skills s
                JOIN ConsultantSkills cs ON s.Id = cs.SkillId
                GROUP BY s.Nom
                ORDER BY UsageCount DESC;";

            using (var command = new SqlCommand(sqlStats, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                // Lecture des statuts
                while (await reader.ReadAsync())
                {
                    string status = reader.GetString(0);
                    int count = reader.GetInt32(1);
                    stats.TotalConsultants += count;

                    if (status == "En Mission") stats.EnMissionCount = count;
                    else if (status == "Disponible") stats.DisponibleCount = count;
                    else if (status == "Intercontrat") stats.IntercontratCount = count;
                }

                // Lecture du Top Skills (Résultat suivant)
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.TopSkills.Add(new SkillStat { 
                            SkillNom = reader.GetString(0), 
                            Count = reader.GetInt32(1) 
                        });
                    }
                }
            }
        }
        return stats;
    }
}