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

        int skip = (page - 1) * pageSize;

        string countSql = "SELECT COUNT(Id) FROM Consultants";

        string sql = $@"
            SELECT c.Id, c.Nom, c.Prenom, c.Titre, c.Statut,
                (SELECT STRING_AGG(s.Nom, ',') 
                    FROM ConsultantSkills cs 
                    JOIN Skills s ON cs.SkillId = s.Id 
                    WHERE cs.ConsultantId = c.Id) as Skills
            FROM Consultants c
            ORDER BY Nom
            OFFSET {skip} ROWS
            FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(_connectionString))
            {
            await connection.OpenAsync();

            using (var countCommand = new SqlCommand(countSql, connection))
            {
                // ExecuteScalar renvoie la première colonne de la première ligne
                totalCount = (int)await countCommand.ExecuteScalarAsync();
            }

            using (var command = new SqlCommand(sql, connection))
            {
                //Utilisation du Data Reader pour lire ligne par ligne
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        consultants.Add(new ConsultantListingDto
                        {
                            Id = reader.GetInt32(0),
                            NomComplet = reader.GetString(2) + " " + reader.GetString(1), // Prenom + Nom
                            Titre = reader.GetString(3),
                            //DescriptionCourte = reader.GetString(4).Substring(0, 50) + "...",
                            Statut = reader.GetString(4),
                            Competences = reader.IsDBNull(5) ? new List<string>() : reader.GetString(5).Split(',').ToList()
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

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // --- 1. INFOS DE BASE + SKILLS ---
            string sqlInfo = @"
                SELECT c.Id, c.Nom, c.Prenom, c.Titre, c.ExperienceTotale, c.Statut,
                    s.Nom as SkillNom, cs.Niveau
                FROM Consultants c
                LEFT JOIN ConsultantSkills cs ON c.Id = cs.ConsultantId
                LEFT JOIN Skills s ON cs.SkillId = s.Id
                WHERE c.Id = @id";

            using (var command = new SqlCommand(sqlInfo, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (consultant == null)
                        {
                            consultant = new Consultant {
                                Id = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Prenom = reader.GetString(2),
                                Titre = reader.GetString(3),
                                ExperienceTotale = reader.GetInt32(4),
                                Statut = reader.GetString(5),
                                ConsultantSkills = new List<ConsultantSkill>(),
                                Missions = new List<Mission>()
                            };
                        }
                        if (!reader.IsDBNull(7)) {
                            consultant.ConsultantSkills.Add(new ConsultantSkill {
                                Skill = new Skill { Nom = reader.GetString(6) },
                                Niveau = reader.GetInt32(7)
                            });
                        }
                    }
                }
            }

            if (consultant == null) return null;

            // --- 2. MISSIONS + CLIENTS ---
            string sqlMissions = @"
                SELECT m.TitreProjet, m.RoleOccupe, m.DateDebut, m.DateFin, m.Description, cl.Nom as ClientNom
                FROM Missions m
                JOIN Clients cl ON m.ClientId = cl.Id
                WHERE m.ConsultantId = @id
                ORDER BY m.DateDebut DESC";

            using (var command = new SqlCommand(sqlMissions, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        consultant.Missions.Add(new Mission {
                            TitreProjet = reader.GetString(0),
                            RoleOccupe = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            DateDebut = reader.GetDateTime(2),
                            DateFin = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                            Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Client = new Client { Nom = reader.GetString(5) } // On peuple l'objet Client
                        });
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

            string sqlStats = @"
                -- 1. Compteurs par statut
                SELECT Statut, COUNT(*) as Total 
                FROM Consultants 
                GROUP BY Statut;

                -- 2. Top 5 compétences
                SELECT TOP 5 s.Nom, COUNT(cs.ConsultantId) as UsageCount
                FROM Skills s
                JOIN ConsultantSkills cs ON s.Id = cs.SkillId
                GROUP BY s.Nom
                ORDER BY UsageCount DESC;

                -- 3. Missions actives aujourd'hui
                SELECT COUNT(*) 
                FROM Missions
                WHERE DateDebut <= CAST(GETDATE() AS DATE)
                AND (DateFin IS NULL OR DateFin >= CAST(GETDATE() AS DATE));

                -- 4. 5 consultants les plus récemment ajoutés
                SELECT TOP 5 Id, Nom, Prenom, Titre, Statut
                FROM Consultants
                ORDER BY Id DESC;";

            using (var command = new SqlCommand(sqlStats, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                // Résultat 1 : statuts
                while (await reader.ReadAsync())
                {
                    string status = reader.GetString(0);
                    int count = reader.GetInt32(1);

                    stats.TotalConsultants += count;

                    if (status == "En Mission")
                        stats.EnMissionCount = count;
                    else if (status == "Disponible")
                        stats.DisponibleCount = count;
                    else if (status == "Intercontrat")
                        stats.IntercontratCount = count;
                }

                // Résultat 2 : top compétences
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.TopSkills.Add(new SkillStat
                        {
                            SkillNom = reader.GetString(0),
                            Count = reader.GetInt32(1)
                        });
                    }
                }

                // Résultat 3 : missions actives aujourd'hui
                if (await reader.NextResultAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        stats.MissionsActivesAujourdHui = reader.GetInt32(0);
                    }
                }

                // Résultat 4 : consultants récents
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.RecentConsultants.Add(new Consultant
                        {
                            Id = reader.GetInt32(0),
                            Nom = reader.GetString(1),
                            Prenom = reader.GetString(2),
                            Titre = reader.GetString(3),
                            Statut = reader.GetString(4),
                            ConsultantSkills = new List<ConsultantSkill>(),
                            Missions = new List<Mission>()
                        });
                    }
                }
            }
        }

        return stats;
    }

    public async Task UpdateExpiredStatusesAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            string lazyUpdateSql = @"
                UPDATE Consultants 
                SET Statut = 'Intercontrat' 
                WHERE Statut = 'En Mission' 
                AND Id NOT IN (
                    SELECT ConsultantId FROM Missions 
                    WHERE DateDebut <= GETDATE() 
                    AND (DateFin IS NULL OR DateFin >= CAST(GETDATE() AS DATE))
                )";
            
            using (var updateCmd = new SqlCommand(lazyUpdateSql, connection))
            {
                await updateCmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<Dictionary<int, string>> GetRealtimeStatusesAsync(List<int> ids)
    {
        var statuses = new Dictionary<int, string>();
        if (!ids.Any()) return statuses;

        string sql = $"SELECT Id, Statut FROM Consultants WHERE Id IN ({string.Join(",", ids)})";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var cmd = new SqlCommand(sql, connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    statuses.Add(reader.GetInt32(0), reader.GetString(1));
                }
            }
        }
        return statuses;
    }

    public async Task<TimelineViewModel> GetTimelineDataAsync(int monthsToDisplay = 6)
    {
        var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var endDateExclusive = startDate.AddMonths(monthsToDisplay);

        var viewModel = new TimelineViewModel
        {
            StartDate = startDate,
            TotalDays = (endDateExclusive - startDate).Days
        };

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string sql = @"
                SELECT c.Nom, c.Prenom, c.Statut, m.TitreProjet, m.DateDebut, m.DateFin, cl.Nom as ClientNom
                FROM Consultants c
                LEFT JOIN Missions m ON c.Id = m.ConsultantId
                LEFT JOIN Clients cl ON m.ClientId = cl.Id
                ORDER BY c.Nom, c.Prenom, m.DateDebut";

            using (var cmd = new SqlCommand(sql, connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                string currentConsultant = "";
                ConsultantTimelineDto? currentDto = null;

                while (await reader.ReadAsync())
                {
                    string fullName = $"{reader.GetString(1)} {reader.GetString(0)}";
                    string statut = reader.IsDBNull(2) ? "" : reader.GetString(2);

                    if (currentConsultant != fullName)
                    {
                        currentDto = new ConsultantTimelineDto
                        {
                            NomComplet = fullName,
                            Statut = statut
                        };
                        viewModel.Consultants.Add(currentDto);
                        currentConsultant = fullName;
                    }

                    if (!reader.IsDBNull(4) && currentDto != null)
                    {
                        DateTime missionStart = reader.GetDateTime(4).Date;
                        DateTime missionEnd = reader.IsDBNull(5)
                            ? endDateExclusive.AddDays(-1)
                            : reader.GetDateTime(5).Date;

                        var visibleStart = missionStart < startDate ? startDate : missionStart;
                        var visibleEnd = missionEnd >= endDateExclusive ? endDateExclusive.AddDays(-1) : missionEnd;

                        if (visibleEnd >= visibleStart)
                        {
                            int startColumn = (visibleStart - startDate).Days + 1;
                            int columnSpan = (visibleEnd - visibleStart).Days + 1;

                            string clientName = reader.IsDBNull(6) ? "Client inconnu" : reader.GetString(6);
                            string missionTitle = reader.IsDBNull(3) ? "Mission" : reader.GetString(3);

                            currentDto.MissionBars.Add(new MissionBarDto
                            {
                                Label = $"{clientName} - {missionTitle}",
                                StartColumn = startColumn,
                                ColumnSpan = Math.Max(columnSpan, 1)
                            });
                        }
                    }
                }
            }
        }

        return viewModel;
    }
}