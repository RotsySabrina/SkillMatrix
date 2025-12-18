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
            // On crée le document
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // 1. En-tête (Header)
                    page.Header()
                        .Text($"DOSSIER DE COMPÉTENCES - Réf: {consultant.Id}")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    // 2. Contenu (Content)
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Titre du poste et Expérience
                        col.Item().Text(consultant.Titre).FontSize(16).Bold();
                        col.Item().Text($"{consultant.ExperienceTotale} ans d'expérience").FontSize(14).Italic().FontColor(Colors.Grey.Medium);
                        
                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        // Section Compétences Techniques
                        col.Item().Text("Compétences Techniques").FontSize(14).Bold().Underline();
                        col.Item().PaddingTop(5);

                        // Tableau des compétences
                        col.Item().Table(table =>
                        {
                            // Définition des colonnes
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Nom de la compétence
                                columns.ConstantColumn(100); // Niveau (Barre ou Texte)
                            });

                            // En-têtes du tableau
                            table.Header(header =>
                            {
                                header.Cell().Text("Technologie").Bold();
                                header.Cell().Text("Niveau (1-5)").Bold();
                            });

                            // Remplissage des lignes avec les compétences du consultant
                            if (consultant.ConsultantSkills != null)
                            {
                                foreach (var skillLink in consultant.ConsultantSkills)
                                {
                                    table.Cell().PaddingVertical(2).Text(skillLink.Skill?.Nom ?? "Inconnu");
                                    
                                    // Affichage visuel du niveau (ex: "4/5")
                                    table.Cell().PaddingVertical(2).AlignRight().Text($"{skillLink.Niveau}/5");
                                }
                            }
                        });

                        col.Item().PaddingVertical(15);

                        // Section Statut (Optionnel)
                        col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c => 
                        {
                            c.Item().Text("Informations de Disponibilité").Bold();
                            c.Item().Text($"Statut actuel : {consultant.Statut}");
                        });
                    });

                    // 3. Pied de page (Footer)
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Généré par SkillMatrix - ");
                            x.CurrentPageNumber();
                        });
                });
            });

            // Retourne le PDF sous forme de tableau d'octets
            return document.GeneratePdf();
        }
    }
}