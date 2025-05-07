using System.Text.Json;
using babbly_auth_service.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace babbly_auth_service.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;
        private const string AUTHORIZATION_RESPONSE_TOPIC = "auth-responses";

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
                ClientId = "babbly-auth-producer"
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            
            _logger.LogInformation("Kafka producer initialized with bootstrap servers: {servers}", 
                producerConfig.BootstrapServers);
        }

        public async Task ProduceAuthorizationResponseAsync(AuthMessage authMessage)
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(authMessage);
                var message = new Message<string, string>
                {
                    Key = authMessage.CorrelationId,
                    Value = messageJson
                };

                var deliveryResult = await _producer.ProduceAsync(AUTHORIZATION_RESPONSE_TOPIC, message);
                
                _logger.LogInformation("Authorization response delivered to {topic} [partition: {partition}, offset: {offset}]", 
                    deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error producing authorization response message");
                throw;
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
} 