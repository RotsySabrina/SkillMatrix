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
            var settings = new ConnectionSettings(new Uri(url))
                .DefaultIndex(IndexName);
            _client = new ElasticClient(settings);
        }

        public async Task IndexConsultantAsync(SearchConsultantDto consultantDto)
        {
            var response = await _client.IndexDocumentAsync(consultantDto);

            if (!response.IsValid)
            {
                throw new Exception("Indexation Elastic a échoué: " + response.OriginalException?.Message);
            }
        }
        
        public async Task CreateIndexAsync()
        {
            var createIndexResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Map<SearchConsultantDto>(m => m.AutoMap())
            );
        }

        public async Task<List<SearchConsultantDto>> SearchConsultantsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<SearchConsultantDto>();
            }

            var searchResponse = await _client.SearchAsync<SearchConsultantDto>(s => s
                .Index("consultant_index")
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(query)
                        .Fuzziness(Fuzziness.Auto) 
                        .Fields(f => f
                            .Field(p => p.NomComplet, 3.0)      
                            .Field(p => p.Competences, 2.0)     
                            .Field(p => p.Titre, 1.5)          
                            .Field(p => p.Statut) 
                        )
                    )
                )
                .Size(20)
            );

            return searchResponse.Documents.ToList();
        }
        
        public async Task DeleteConsultantAsync(int id)
        {
            var response = await _client.DeleteAsync<SearchConsultantDto>(id);
            if (!response.IsValid && response.ServerError != null)
            {
                throw new Exception($"Suppression Elastic échouée : {response.ServerError.Error.Reason}");
            }
        }

        public async Task SyncAllConsultantsAsync(IEnumerable<SearchConsultantDto> consultants)
        {
            if (consultants == null || !consultants.Any()) return;

            var bulkDescriptor = new BulkDescriptor();

            foreach (var consultant in consultants)
            {
                bulkDescriptor.Index<SearchConsultantDto>(op => op
                    .Id(consultant.Id) 
                    .Document(consultant)
                );
            }

            var response = await _client.BulkAsync(bulkDescriptor);

            if (!response.IsValid)
            {
                Console.WriteLine($"Erreur Bulk Indexing: {response.OriginalException?.Message}");
            }
        }

        public async Task ReindexAllAsync(List<SearchConsultantDto> allConsultants)
        {
            await _client.Indices.DeleteAsync(IndexName);
            
            var createResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Map<SearchConsultantDto>(m => m.AutoMap())
            );

            if (allConsultants != null && allConsultants.Any())
            {
                var bulkResponse = await _client.IndexManyAsync(allConsultants, IndexName);
                
                await _client.Indices.RefreshAsync(IndexName);

                if (!bulkResponse.IsValid)
                {
                    throw new Exception($"Erreur lors de la réindexation massive : {bulkResponse.DebugInformation}");
                }
            }
        }

    }
}