using Confluent.Kafka;
using System.Text.Json;
using babbly_auth_service.Models;

namespace babbly_auth_service.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;
        private readonly string _userTopic;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            
            // Get Kafka configuration
            var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? 
                                   configuration["Kafka:BootstrapServers"] ?? 
                                   "localhost:9092";
                                   
            _userTopic = Environment.GetEnvironmentVariable("KAFKA_USER_TOPIC") ?? 
                         configuration["Kafka:UserTopic"] ?? 
                         "user-events";

            // Configure Kafka producer
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                ClientId = "babbly-auth-service",
                Acks = Acks.Leader, // Wait for the leader to acknowledge the message
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000, // 1 second backoff between retries
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            
            _logger.LogInformation("Kafka producer initialized with bootstrap servers: {BootstrapServers}", bootstrapServers);
        }

        /// <summary>
        /// Publishes a user created event to Kafka
        /// </summary>
        public async Task PublishUserCreatedEventAsync(User user)
        {
            try
            {
                var message = new UserCreatedEvent
                {
                    UserId = user.Id,
                    Auth0Id = user.Auth0Id ?? string.Empty,
                    Email = user.Email,
                    Name = user.Name,
                    Picture = user.Picture,
                    CreatedAt = user.CreatedAt,
                    EventType = "UserCreated",
                    Timestamp = DateTime.UtcNow
                };

                string json = JsonSerializer.Serialize(message);
                
                var kafkaMessage = new Message<string, string>
                {
                    Key = user.Id, // Using the user ID as the message key for partitioning
                    Value = json
                };

                // Publish message asynchronously
                var deliveryResult = await _producer.ProduceAsync(_userTopic, kafkaMessage);
                
                _logger.LogInformation(
                    "User created event published to Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                    deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
                    
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing user created event to Kafka");
                throw;
            }
        }

        /// <summary>
        /// Publishes a user updated event to Kafka
        /// </summary>
        public async Task PublishUserUpdatedEventAsync(User user)
        {
            try
            {
                var message = new UserUpdatedEvent
                {
                    UserId = user.Id,
                    Auth0Id = user.Auth0Id ?? string.Empty,
                    Email = user.Email,
                    Name = user.Name,
                    Picture = user.Picture,
                    UpdatedAt = user.UpdatedAt,
                    EventType = "UserUpdated",
                    Timestamp = DateTime.UtcNow
                };

                string json = JsonSerializer.Serialize(message);
                
                var kafkaMessage = new Message<string, string>
                {
                    Key = user.Id,
                    Value = json
                };

                var deliveryResult = await _producer.ProduceAsync(_userTopic, kafkaMessage);
                
                _logger.LogInformation(
                    "User updated event published to Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                    deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
                    
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing user updated event to Kafka");
                throw;
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
} 