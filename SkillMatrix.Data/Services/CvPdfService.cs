using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkillMatrix.Core.Models;

namespace SkillMatrix.Data.Services
{
    public class CvPdfService
    {
        public byte[] GenerateAnonymousCv(Consultant consultant)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                    // --- 1. EN-TÊTE ---
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text($"{consultant.Prenom} {consultant.Nom}").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text(consultant.Titre).FontSize(16).Italic();
                        });

                        row.ConstantItem(100).Column(col =>
                        {
                            col.Item().Text($"Réf: {consultant.Id}").AlignRight().FontSize(10).FontColor(Colors.Grey.Medium);
                            col.Item().Text(consultant.Statut).AlignRight().FontSize(10).Bold();
                        });
                    });

                    // --- 2. CONTENU ---
                    page.Content().PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
                    {

                        // --- SECTION COMPÉTENCES ---
                        col.Item().Text("COMPÉTENCES TECHNIQUES").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().PaddingTop(2).PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                            });

                            if (consultant.ConsultantSkills != null)
                            {
                                foreach (var skillLink in consultant.ConsultantSkills)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text(skillLink.Skill?.Nom);
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).AlignRight().Text($"{skillLink.Niveau} / 5");
                                }
                            }
                        });

                        col.Item().PaddingVertical(15);

                        // --- SECTION EXPÉRIENCES (MISSIONS) ---
                        col.Item().Text("EXPÉRIENCES PROFESSIONNELLES").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().PaddingTop(2).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        if (consultant.Missions != null && consultant.Missions.Any())
                        {
                            foreach (var mission in consultant.Missions)
                            {
                                col.Item().PaddingBottom(15).Column(mCol =>
                                {
                                    mCol.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(mission.TitreProjet).Bold().FontSize(12);
                                        row.ConstantItem(150).AlignRight().Text($"{mission.DateDebut:MM/yyyy} - {(mission.DateFin.HasValue ? mission.DateFin.Value.ToString("MM/yyyy") : "Présent")}").FontSize(10);
                                    });

                                    mCol.Item().Text($"{mission.Client?.Nom} | {mission.RoleOccupe}").Italic().FontColor(Colors.Grey.Darken2);
                                    
                                    if (!string.IsNullOrEmpty(mission.Description))
                                    {
                                        mCol.Item().PaddingTop(5).Text(mission.Description).FontSize(10).Justify();
                                    }
                                });
                            }
                        }
                        else
                        {
                            col.Item().Text("Aucune expérience enregistrée.").Italic();
                        }
                    });

                    // --- 3. PIED DE PAGE ---
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("TalentTris - Document confidentiel - Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}