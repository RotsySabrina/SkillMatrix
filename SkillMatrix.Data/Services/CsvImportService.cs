using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore; 
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
            var existingSkills = await _context.Skills
                .ToDictionaryAsync(s => s.Nom.ToLower(), s => s.Id);

            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var recordsDto = csv.GetRecords<ConsultantImportDto>().ToList();
            var newConsultants = new List<Consultant>();

            foreach (var dto in recordsDto)
            {
                var consultant = new Consultant
                {
                    Nom = dto.Nom,
                    Prenom = dto.Prenom,
                    Titre = dto.Titre,
                    ExperienceTotale = dto.ExperienceTotale,
                    Statut = dto.Statut,
                    ConsultantSkills = new List<ConsultantSkill>()
                };

                if (!string.IsNullOrWhiteSpace(dto.CompetencesString))
                {
                    var skillsRaw = dto.CompetencesString.Split(';');

                    foreach (var skillRaw in skillsRaw)
                    {
                        var parts = skillRaw.Split('|');
                        var skillName = parts[0].Trim();
                        
                        int level = 1;
                        if (parts.Length > 1 && int.TryParse(parts[1], out int parsedLevel))
                        {
                            level = parsedLevel;
                        }

                        if (existingSkills.TryGetValue(skillName.ToLower(), out int skillId))
                        {
                            consultant.ConsultantSkills.Add(new ConsultantSkill
                            {
                                SkillId = skillId,
                                Niveau = level,
                                DerniereUtilisation = DateTime.Now
                            });
                        }
                    }
                }

                newConsultants.Add(consultant);
            }

            if (newConsultants.Any())
            {
                _context.Consultants.AddRange(newConsultants);
                await _context.SaveChangesAsync();

                foreach (var consultant in newConsultants)
                {
                    var skillNames = consultant.ConsultantSkills
                                        .Select(cs => existingSkills.FirstOrDefault(x => x.Value == cs.SkillId).Key)
                                        .ToList();

                    var searchDto = new SearchConsultantDto
                    {
                        Id = consultant.Id,
                        NomComplet = $"{consultant.Prenom} {consultant.Nom}",
                        Titre = consultant.Titre,
                        Statut = consultant.Statut,
                        Competences = skillNames 
                    };

                    await _elasticService.IndexConsultantAsync(searchDto);
                }
            }

            return newConsultants.Count;
        }
    }
}