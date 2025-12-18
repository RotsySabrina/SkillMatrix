using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore; // Important pour ToListAsync
using SkillMatrix.Core.Models;
using SkillMatrix.Core.DTOs;
using SkillMatrix.Data.EF;

namespace SkillMatrix.Data.Services
{
    public class CsvImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ElasticSearchService _elasticService;

        public CsvImportService(ApplicationDbContext context, ElasticSearchService elasticService)
        {
            _context = context;
            _elasticService = elasticService;
        }

        public async Task<int> ImportConsultantsAsync(Stream fileStream)
        {
            // 1. Charger TOUTES les compétences existantes en mémoire (Dictionnaire Nom -> ID)
            // Cela évite de faire une requête SQL à chaque ligne du CSV.
            // On met le nom en minuscule pour éviter les soucis de majuscules (c# vs C#).
            var existingSkills = await _context.Skills
                .ToDictionaryAsync(s => s.Nom.ToLower(), s => s.Id);

            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            // On lit le CSV dans notre DTO temporaire
            var recordsDto = csv.GetRecords<ConsultantImportDto>().ToList();
            var newConsultants = new List<Consultant>();

            foreach (var dto in recordsDto)
            {
                // A. Création du Consultant de base
                var consultant = new Consultant
                {
                    Nom = dto.Nom,
                    Prenom = dto.Prenom,
                    Titre = dto.Titre,
                    ExperienceTotale = dto.ExperienceTotale,
                    Statut = dto.Statut,
                    ConsultantSkills = new List<ConsultantSkill>()
                };

                // B. Traitement des Compétences (Logique Many-to-Many)
                // Format attendu : "C#|3;Java|2"
                if (!string.IsNullOrWhiteSpace(dto.CompetencesString))
                {
                    var skillsRaw = dto.CompetencesString.Split(';'); // Sépare les compétences

                    foreach (var skillRaw in skillsRaw)
                    {
                        // skillRaw ressemble à "C#|3"
                        var parts = skillRaw.Split('|');
                        var skillName = parts[0].Trim();
                        
                        // Gestion du niveau (par défaut 1 si mal renseigné)
                        int level = 1;
                        if (parts.Length > 1 && int.TryParse(parts[1], out int parsedLevel))
                        {
                            level = parsedLevel;
                        }

                        // C. Recherche de l'ID de la compétence
                        if (existingSkills.TryGetValue(skillName.ToLower(), out int skillId))
                        {
                            // LA LOGIQUE QUE TU CONNAIS : Création de la jointure
                            consultant.ConsultantSkills.Add(new ConsultantSkill
                            {
                                SkillId = skillId,
                                Niveau = level,
                                DerniereUtilisation = DateTime.Now
                            });
                        }
                        else
                        {
                            // Optionnel : Ici, tu pourrais décider de CRÉER la compétence si elle n'existe pas.
                            // Pour l'instant, on l'ignore si elle n'est pas dans la base.
                        }
                    }
                }

                newConsultants.Add(consultant);
            }

            if (newConsultants.Any())
            {
                // 2. Sauvegarde SQL (Génération des IDs Consultant)
                _context.Consultants.AddRange(newConsultants);
                await _context.SaveChangesAsync();

                // 3. Synchronisation Elasticsearch
                foreach (var consultant in newConsultants)
                {
                    // Note : Pour Elastic, on veut la liste des noms des compétences qu'on vient d'ajouter
                    // On peut les récupérer directement depuis l'objet consultant car on vient de remplir ConsultantSkills
                    var skillNames = consultant.ConsultantSkills
                                        .Select(cs => existingSkills.FirstOrDefault(x => x.Value == cs.SkillId).Key) // Retrouver le nom via l'ID
                                        .ToList();

                    var searchDto = new SearchConsultantDto
                    {
                        Id = consultant.Id,
                        NomComplet = $"{consultant.Prenom} {consultant.Nom}",
                        Titre = consultant.Titre,
                        //DescriptionProfil = consultant.DescriptionProfil,
                        Statut = consultant.Statut,
                        Competences = skillNames // Liste de strings pour la recherche
                    };

                    await _elasticService.IndexConsultantAsync(searchDto);
                }
            }

            return newConsultants.Count;
        }
    }
}