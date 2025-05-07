using System.Text.Json;
using babbly_auth_service.Models;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace babbly_auth_service.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly KafkaProducerService _producerService;
        private const string AUTHORIZATION_REQUEST_TOPIC = "auth-requests";

        public KafkaConsumerService(
            IConfiguration configuration, 
            ILogger<KafkaConsumerService> logger,
            IServiceProvider serviceProvider,
            KafkaProducerService producerService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _producerService = producerService;
            
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
                GroupId = "babbly-auth-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _consumer.Subscribe(AUTHORIZATION_REQUEST_TOPIC);
            
            _logger.LogInformation("Kafka consumer initialized with bootstrap servers: {servers}", 
                consumerConfig.BootstrapServers);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kafka consumer service starting");
            
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);
                        
                        _logger.LogInformation("Message received from {topic} [partition: {partition}, offset: {offset}]", 
                            consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);

                        await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error consuming message");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                _logger.LogInformation("Kafka consumer service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Kafka consumer service");
            }
            finally
            {
                _consumer?.Close();
                _consumer?.Dispose();
            }
        }

        private async Task ProcessMessageAsync(string messageJson, CancellationToken cancellationToken)
        {
            try
            {
                var authRequest = JsonSerializer.Deserialize<AuthMessage>(messageJson);
                
                if (authRequest == null)
                {
                    _logger.LogWarning("Failed to deserialize auth request");
                    return;
                }

                _logger.LogInformation("Processing auth request for user {userId}, resource {resource}, operation {operation}", 
                    authRequest.UserId, authRequest.ResourcePath, authRequest.Operation);

                // Create a scope to resolve the scoped TokenService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
                    
                    // Process the authorization request and determine if it's authorized
                    authRequest.IsAuthorized = await tokenService.IsAuthorizedForResourceAsync(
                        authRequest.UserId, 
                        authRequest.Roles, 
                        authRequest.ResourcePath, 
                        authRequest.Operation);
                }

                // Send the response back through Kafka
                await _producerService.ProduceAuthorizationResponseAsync(authRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        }

        public override void Dispose()
        {
            _consumer?.Close();
            _consumer?.Dispose();
            base.Dispose();
        }
    }
} 