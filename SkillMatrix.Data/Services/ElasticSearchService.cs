using Nest;
using SkillMatrix.Core.DTOs;

namespace SkillMatrix.Data.Services
{
    public class ElasticSearchService
    {
        private readonly ElasticClient _client;
        private const string IndexName = "consultant_index";

        public ElasticSearchService(string url)
        {
            // COMMENT: Configuration de la connexion NEST
            var settings = new ConnectionSettings(new Uri(url))
                .DefaultIndex(IndexName);
            _client = new ElasticClient(settings);
        }

        // 🛑 CRITIQUE : Méthode pour indexer un document
        public async Task IndexConsultantAsync(SearchConsultantDto consultantDto)
        {
            // Indexe ou met à jour le document. L'Id est utilisé comme clé unique
            var response = await _client.IndexDocumentAsync(consultantDto);

            if (!response.IsValid)
            {
                // Gérer l'erreur d'indexation
                throw new Exception("Indexation Elastic a échoué: " + response.OriginalException?.Message);
            }
        }
        
        // COMMENT: Méthode pour créer l'index au démarrage (si non existant)
        public async Task CreateIndexAsync()
        {
            var createIndexResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Map<SearchConsultantDto>(m => m.AutoMap()) // Mappe automatiquement le DTO
            );
        }

        public async Task<List<SearchConsultantDto>> SearchConsultantsAsync(string query)
        {
            // Si la recherche est vide, on retourne une liste vide (le contrôleur gérera le repli sur SQL)
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<SearchConsultantDto>();
            }

            // Requête Elasticsearch
            var searchResponse = await _client.SearchAsync<SearchConsultantDto>(s => s
                .Index("consultant_index") // Assurez-vous que c'est le bon nom d'index
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(query) // Le mot tapé par l'utilisateur
                        .Fuzziness(Fuzziness.Auto) // 💡 Autorise les fautes de frappe légères
                        .Fields(f => f
                            .Field(p => p.NomComplet, 3.0)      // Boost x3 : Le nom est très important
                            .Field(p => p.Competences, 2.0)     // Boost x2 : Les compétences sont importantes
                            .Field(p => p.Titre, 1.5)           // Boost x1.5 : Le titre compte
                            .Field(p => p.Statut)    // Description standard
                        )
                    )
                )
                .Size(20) // On limite à 20 résultats pour la recherche pour l'instant
            );

            return searchResponse.Documents.ToList();
        }
        
    }
}