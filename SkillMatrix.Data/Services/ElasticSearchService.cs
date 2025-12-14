using Nest;

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
    }
}