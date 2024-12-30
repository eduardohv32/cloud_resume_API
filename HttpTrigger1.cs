using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace Company.Function
{
    public class HttpTrigger1
    {
        private readonly ILogger<HttpTrigger1> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseName;
        private readonly string _containerName;

        public HttpTrigger1(ILogger<HttpTrigger1> logger, IConfiguration configuration)
        {
            _logger = logger;
            _cosmosClient = new CosmosClient(configuration["CosmosDBConnectionString"]);
            _databaseName = configuration["CosmosDBDatabaseName"];
            _containerName = configuration["CosmosDBContainerName"];
        }

        [Function("HttpTrigger1")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var container = _cosmosClient.GetContainer(_databaseName, _containerName);
            var sqlQueryText = "SELECT * FROM c WHERE c.id = 'visitorCounter'";
            var queryDefinition = new QueryDefinition(sqlQueryText);
            var queryResultSetIterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

            int count = 0;
            string id = string.Empty;
            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (var item in currentResultSet)
                {
                    count = item.count;
                    id = item.id;
                }
            }

            count += 1;

            var updatedItem = new { id = id, count = count };
            await container.UpsertItemAsync(updatedItem, new PartitionKey(id));

            return new OkObjectResult($"{count}");
        }
    }
}