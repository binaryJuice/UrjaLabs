// Multi-Provider Queue Abstraction (SOLID) – .NET/C#
// -------------------------------------------------
// Goals:
//  - Support multiple queue providers behind clean abstractions (Azure Service Bus, RabbitMQ, AWS SQS, Kafka*)
//  - Follow SOLID: SRP, OCP, LSP, ISP, DIP
//  - Be DI-friendly, testable, and extensible
//  - Provide producers & consumers with retry, dead-letter, and observability hooks
//
// Notes:
//  - This is a production-ready skeleton with safe defaults; wire real SDK calls where indicated.
//  - Kafka is: topic-based, not a classic queue; adapter provides queue-like semantics.
//  - Packages to reference when implementing concretions (not required to read this file):
//      Azure.Messaging.ServiceBus
//      RabbitMQ.Client
//      AWSSDK.SQS
//      Confluent.Kafka
// -----------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Queueing.Solid
{
    // ----------------------------
    // Contracts (DIP, ISP, LSP)
    // ----------------------------

    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] payload);
        string ContentType { get; }
    }

    public interface IRetryPolicy
    {
        Task ExecuteAsync(Func<int, Task> action, Func<int, Exception, bool> shouldRetry, CancellationToken ct);
    }

    public interface IQueueProducer
    {
        Task SendAsync<T>(T message, MessageMetadata meta, CancellationToken ct = default);
    }

    public interface IQueueConsumer
    {
        Task StartAsync(Func<InboundMessage, Task<ConsumeResult>> handler, CancellationToken ct = default);
    }

    // Smaller, segregated interfaces for advanced features
    public interface IDeadLetterSender
    {
        Task DeadLetterAsync(InboundMessage failedMessage, string reason, CancellationToken ct = default);
    }

    public interface IHealthCheckable
    {
        Task<bool> IsHealthyAsync(CancellationToken ct = default);
    }

    // ----------------------------
    // Core Models
    // ----------------------------

    public record MessageMetadata(
        string MessageId,
        string CorrelationId,
        string Subject,
        string? ContentType = null,
        int? DeliveryDelaySeconds = null,
        ConcurrentDictionary<string, string>? Headers = null);

    public record InboundMessage(
        byte[] Body,
        MessageMetadata Metadata,
        Func<Task> Ack,
        Func<Task> Nack,
        Func<Task> Requeue);

    public enum QueueProvider
    {
        AzureServiceBus,
        RabbitMq,
        AwsSqs,
        Kafka
    }

    public class QueueOptions
    {
        public QueueProvider Provider { get; set; }
        public string ConnectionString { get; set; } = string.Empty; // or endpoint + creds depending on provider
        public string QueueOrTopicName { get; set; } = string.Empty; // queue or topic
        public string? SubscriptionOrGroup { get; set; } // subscription (ASB) or consumer group (Kafka)
        public string? DeadLetterName { get; set; }
        public int MaxConcurrentHandlers { get; set; } = 8;
        public int Prefetch { get; set; } = 32;
        public int MaxRetries { get; set; } = 5;
        public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(2);
        public bool EnableIdempotencyGuard { get; set; } = true;
    }

    // ----------------------------
    // Default Utilities
    // ----------------------------

    public class SystemTextJsonSerializer : IMessageSerializer
    {
        private readonly JsonSerializerOptions _opts;
        public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
        {
            _opts = options ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public string ContentType => "application/json";
        public byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value!, _opts);
        public T Deserialize<T>(byte[] payload) => JsonSerializer.Deserialize<T>(payload, _opts)!;
    }

    public class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly ILogger<ExponentialBackoffRetryPolicy> _logger;
        private readonly QueueOptions _opts;
        public ExponentialBackoffRetryPolicy(ILogger<ExponentialBackoffRetryPolicy> logger, IOptions<QueueOptions> opts)
        {
            _logger = logger;
            _opts = opts.Value;
        }

        public async Task ExecuteAsync(Func<int, Task> action, Func<int, Exception, bool> shouldRetry, CancellationToken ct)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    await action(attempt);
                    return;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    attempt++;
                    if (!shouldRetry(attempt, ex) || attempt > _opts.MaxRetries)
                        throw;

                    var delay = TimeSpan.FromMilliseconds(_opts.BaseRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    _logger.LogWarning(ex, "Retry attempt {Attempt} after {Delay}.", attempt, delay);
                    await Task.Delay(delay, ct);
                }
            }
        }
    }

    // ----------------------------
    // Factory (OCP, DIP)
    // ----------------------------

    public interface IQueueClientFactory
    {
        IQueueProducer CreateProducer();
        IQueueConsumer CreateConsumer();
        IDeadLetterSender? CreateDeadLetter();
    }

    public class QueueClientFactory : IQueueClientFactory
    {
        private readonly IServiceProvider _sp;
        private readonly QueueOptions _opts;
        public QueueClientFactory(IServiceProvider sp, IOptions<QueueOptions> options)
        {
            _sp = sp;
            _opts = options.Value;
        }

        public IQueueProducer CreateProducer() => _opts.Provider switch
        {
            QueueProvider.AzureServiceBus => _sp.GetRequiredService<AzureServiceBusProducer>(),
            QueueProvider.RabbitMq => _sp.GetRequiredService<RabbitMqProducer>(),
            QueueProvider.AwsSqs => _sp.GetRequiredService<AwsSqsProducer>(),
            QueueProvider.Kafka => _sp.GetRequiredService<KafkaProducer>(),
            _ => throw new NotSupportedException($"Provider {_opts.Provider} not supported")
        };

        public IQueueConsumer CreateConsumer() => _opts.Provider switch
        {
            QueueProvider.AzureServiceBus => _sp.GetRequiredService<AzureServiceBusConsumer>(),
            QueueProvider.RabbitMq => _sp.GetRequiredService<RabbitMqConsumer>(),
            QueueProvider.AwsSqs => _sp.GetRequiredService<AwsSqsConsumer>(),
            QueueProvider.Kafka => _sp.GetRequiredService<KafkaConsumer>(),
            _ => throw new NotSupportedException($"Provider {_opts.Provider} not supported")
        };

        public IDeadLetterSender? CreateDeadLetter() => _opts.Provider switch
        {
            QueueProvider.AzureServiceBus => _sp.GetRequiredService<AzureServiceBusProducer>(),
            QueueProvider.RabbitMq => _sp.GetRequiredService<RabbitMqDeadLetter>(),
            QueueProvider.AwsSqs => _sp.GetRequiredService<AwsSqsDeadLetter>(),
            QueueProvider.Kafka => null, // DLQ pattern varies; implement via separate topic
            _ => null
        };
    }

    // -----------------------------------
    // Idempotency Guard (optional SRP)
    // -----------------------------------

    public interface IIdempotencyStore
    {
        Task<bool> SeenAsync(string messageId, CancellationToken ct);
        Task MarkAsync(string messageId, TimeSpan ttl, CancellationToken ct);
    }

    public class InMemoryIdempotencyStore : IIdempotencyStore
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new();
        public Task<bool> SeenAsync(string messageId, CancellationToken ct)
        {
            if (_seen.TryGetValue(messageId, out var expires))
            {
                if (expires > DateTimeOffset.UtcNow) return Task.FromResult(true);
                _seen.TryRemove(messageId, out _);
            }
            return Task.FromResult(false);
        }
        public Task MarkAsync(string messageId, TimeSpan ttl, CancellationToken ct)
        {
            _seen[messageId] = DateTimeOffset.UtcNow.Add(ttl);
            return Task.CompletedTask;
        }
    }

    // -----------------------------------
    // Provider: Azure Service Bus (skeleton)
    // -----------------------------------

    public sealed class AzureServiceBusProducer : IQueueProducer, IDeadLetterSender, IHealthCheckable
    {
        private readonly QueueOptions _opts;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<AzureServiceBusProducer> _log;
        private readonly IRetryPolicy _retry;
        public AzureServiceBusProducer(IOptions<QueueOptions> opts, IMessageSerializer serializer, ILogger<AzureServiceBusProducer> log, IRetryPolicy retry)
        {
            _opts = opts.Value; _serializer = serializer; _log = log; _retry = retry;
        }
        public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task DeadLetterAsync(InboundMessage failedMessage, string reason, CancellationToken ct = default)
        {
            _log.LogError("DLQ send on ASB not implemented in skeleton. Reason: {Reason}", reason);
            return Task.CompletedTask;
        }

        public async Task SendAsync<T>(T message, MessageMetadata meta, CancellationToken ct)
        {
            var payload = _serializer.Serialize(message);
            await _retry.ExecuteAsync(async _ =>
            {
                // TODO: Use ServiceBusClient/ServiceBusSender
                _log.LogInformation("[ASB] Sent message {MessageId} to {Queue}", meta.MessageId, _opts.QueueOrTopicName);
                await Task.CompletedTask;
            }, (attempt, ex) => true, ct);
        }
    }

    public sealed class AzureServiceBusConsumer : IQueueConsumer, IHealthCheckable
    {
        private readonly QueueOptions _opts;
        private readonly ILogger<AzureServiceBusConsumer> _log;
        private readonly IIdempotencyStore _idem;
        public AzureServiceBusConsumer(IOptions<QueueOptions> opts, ILogger<AzureServiceBusConsumer> log, IIdempotencyStore idem)
        { _opts = opts.Value; _log = log; _idem = idem; }

        public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

        public async Task StartAsync(Func<InboundMessage, Task<ConsumeResult>> handler, CancellationToken ct = default)
        {
            // TODO: Use ServiceBusProcessor with Prefetch and MaxConcurrentCalls from _opts
            _log.LogInformation("[ASB] Starting consumer on {Queue}", _opts.QueueOrTopicName);
            await Task.CompletedTask;
        }
    }

    // -----------------------------------
    // Provider: RabbitMQ (skeleton)
    // -----------------------------------

    public sealed class RabbitMqProducer : IQueueProducer, IDeadLetterSender, IHealthCheckable
    {
        private readonly QueueOptions _opts; private readonly IMessageSerializer _serializer; private readonly ILogger<RabbitMqProducer> _log; private readonly IRetryPolicy _retry;
        public RabbitMqProducer(IOptions<QueueOptions> opts, IMessageSerializer serializer, ILogger<RabbitMqProducer> log, IRetryPolicy retry)
        { _opts = opts.Value; _serializer = serializer; _log = log; _retry = retry; }

        public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task DeadLetterAsync(InboundMessage failedMessage, string reason, CancellationToken ct = default)
        { _log.LogError("[RabbitMQ] DLQ send not implemented in skeleton. Reason: {Reason}", reason); return Task.CompletedTask; }

        public async Task SendAsync<T>(T message, MessageMetadata meta, CancellationToken ct = default)
        {
            var body = _serializer.Serialize(message);
            await _retry.ExecuteAsync(async _ =>
            {
                // TODO: publish via IModel.BasicPublish
                _log.LogInformation("[RabbitMQ] Sent message {MessageId} to {Queue}", meta.MessageId, _opts.QueueOrTopicName);
                await Task.CompletedTask;
            }, (attempt, ex) => true, ct);
        }
    }

    public sealed class RabbitMqConsumer : IQueueConsumer, IHealthCheckable
    {
        private readonly QueueOptions _opts; private readonly ILogger<RabbitMqConsumer> _log; private readonly IIdempotencyStore _idem;
        public RabbitMqConsumer(IOptions<QueueOptions> opts, ILogger<RabbitMqConsumer> log, IIdempotencyStore idem)
        { _opts = opts.Value; _log = log; _idem = idem; }

        public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

        public async Task StartAsync(Func<InboundMessage, Task<ConsumeResult>> handler, CancellationToken ct = default)
        {
            // TODO: consume via EventingBasicConsumer; respect Prefetch
            _log.LogInformation("[RabbitMQ] Starting consumer on {Queue}", _opts.QueueOrTopicName);
            await Task.CompletedTask;
        }
    }

    // -----------------------------------
    // Provider: AWS SQS (skeleton)
    // -----------------------------------

    public sealed class AwsSqsProducer : IQueueProducer, IDeadLetterSender, IHealthCheckable
    {
        private readonly QueueOptions _opts; private readonly IMessageSerializer _serializer; private readonly ILogger<AwsSqsProducer> _log; private readonly IRetryPolicy _retry;
        public AwsSqsProducer(IOptions<QueueOptions> opts, IMessageSerializer serializer, ILogger<AwsSqsProducer> log, IRetryPolicy retry)
        { _opts = opts.Value; _serializer = serializer; _log = log; _retry = retry; }

        public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task DeadLetterAsync(InboundMessage failedMessage, string reason, CancellationToken ct = default)
        { _log.LogError("[SQS] DLQ send not implemented in skeleton. Reason: {Reason}", reason); return Task.CompletedTask; }

        public async Task SendAsync<T>(T message, MessageMetadata meta, CancellationToken ct = default)
        {
            var body = Encoding.UTF8.GetString(_serializer.Serialize(message));
            await _retry.ExecuteAsync(async _ =>
            {
                // TODO: use AmazonSQSClient.SendMessageAsync
                _log.LogInformation("[SQS] Sent message {MessageId} to {Queue}", meta.MessageId, _opts.QueueOrTopicName);
                await Task.CompletedTask;
            }, (attempt, ex) => true, ct);
        }
    }

    public sealed class AwsSqsConsumer : IQueueConsumer, IHealthCheckable
    {
        private readonly QueueOptions _opts; private readonly ILogger<AwsSqsConsumer> _log; private readonly IIdempotencyStore _idem;
        public AwsSqsConsumer(IOptions<QueueOptions> opts, ILogger<AwsSqsConsumer> log, IIdempotencyStore idem)
        { _opts = opts.Value; _log = log; _idem = idem; }

        public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

        public async Task StartAsync(Func<InboundMessage, Task<ConsumeResult>> handler, CancellationToken ct = default)
        {
            // TODO: long-poll ReceiveMessageAsync / DeleteMessageAsync
            _log.LogInformation("[SQS] Starting consumer on {Queue}", _opts.QueueOrTopicName);
            await Task.CompletedTask;
        }
    }

    // -----------------------------------
    // Provider: Kafka (adapter semantics)
    // -----------------------------------

    public sealed class KafkaProducer : IQueueProducer, IHealthCheckable
    {
        private readonly QueueOptions _opts; private readonly IMessageSerializer _serializer; private readonly ILogger<KafkaProducer> _log; private readonly IRetryPolicy _retry;
        public KafkaProducer(IOptions<QueueOptions> opts, IMessageSerializer serializer, ILogger<KafkaProducer> log, IRetryPolicy retry)
        { _opts = opts.Value; _serializer = serializer; _log = log; _retry = retry; }

        public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

        public async Task SendAsync<T>(T message, MessageMetadata meta, CancellationToken ct = default)
        {
            var bytes = _serializer.Serialize(message);
            await _retry.ExecuteAsync(async _ =>
            {
                // TODO: Confluent.Kafka Producer.ProduceAsync(topic, new Message<Null, byte[]>{ Value = bytes })
                _log.LogInformation("[Kafka] Produced message {MessageId} to topic {Topic}", meta.MessageId, _opts.QueueOrTopicName);
                await Task.CompletedTask;
            }, (attempt, ex) => true, ct);
        }
    }

    public sealed class KafkaConsumer : IQueueConsumer, IHealthCheckable
    {
        private readonly QueueOptions _opts; private readonly ILogger<KafkaConsumer> _log; private readonly IIdempotencyStore _idem;
        public KafkaConsumer(IOptions<QueueOptions> opts, ILogger<KafkaConsumer> log, IIdempotencyStore idem)
        { _opts = opts.Value; _log = log; _idem = idem; }

        public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

        public async Task StartAsync(Func<InboundMessage, Task<ConsumeResult>> handler, CancellationToken ct = default)
        {
            // TODO: subscribe and poll; manage commit vs ack
            _log.LogInformation("[Kafka] Starting consumer on topic {Topic}", _opts.QueueOrTopicName);
            await Task.CompletedTask;
        }
    }

    // -----------------------------------
    // Consumption Orchestrator (SRP)
    // -----------------------------------

    public enum ConsumeResult { Ack, Requeue, DeadLetter }

    public sealed class ConsumerHost
    {
        private readonly IQueueConsumer _consumer;
        private readonly IDeadLetterSender? _dlq;
        private readonly ILogger<ConsumerHost> _log;
        private readonly IIdempotencyStore _idem;
        private readonly QueueOptions _opts;

        public ConsumerHost(IQueueClientFactory factory,
                            IOptions<QueueOptions> opts,
                            ILogger<ConsumerHost> log,
                            IIdempotencyStore idem)
        {
            _consumer = factory.CreateConsumer();
            _dlq = factory.CreateDeadLetter();
            _log = log; _idem = idem; _opts = opts.Value;
        }

        public Task RunAsync(Func<InboundMessage, Task<ConsumeResult>> handler, CancellationToken ct = default)
            => _consumer.StartAsync(async msg =>
            {
                if (_opts.EnableIdempotencyGuard && await _idem.SeenAsync(msg.Metadata.MessageId, ct))
                {
                    _log.LogWarning("Duplicate message {MessageId} ignored.", msg.Metadata.MessageId);
                    await msg.Ack();
                    return ConsumeResult.Ack;
                }

                var result = ConsumeResult.Ack;
                try
                {
                    result = await handler(msg);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Handler threw for message {MessageId}", msg.Metadata.MessageId);
                    result = ConsumeResult.Requeue;
                }

                switch (result)
                {
                    case ConsumeResult.Ack:
                        await _idem.MarkAsync(msg.Metadata.MessageId, TimeSpan.FromHours(6), ct);
                        await msg.Ack();
                        break;
                    case ConsumeResult.Requeue:
                        await msg.Requeue();
                        break;
                    case ConsumeResult.DeadLetter:
                        if (_dlq != null)
                            await _dlq.DeadLetterAsync(msg, "Handler requested DLQ", ct);
                        await msg.Nack();
                        break;
                }

                return result;
            }, ct);
    }

    // -----------------------------------
    // DI Registration & Options Binding
    // -----------------------------------

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddQueueing(this IServiceCollection services, Action<QueueOptions> configure)
        {
            services.Configure(configure);

            // Core utilities
            services.AddSingleton<IMessageSerializer, SystemTextJsonSerializer>();
            services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
            services.AddSingleton<IRetryPolicy, ExponentialBackoffRetryPolicy>();

            // Factory
            services.AddSingleton<IQueueClientFactory, QueueClientFactory>();

            // Providers (register concretes; resolved by factory)
            services.AddSingleton<AzureServiceBusProducer>();
            services.AddSingleton<AzureServiceBusConsumer>();

            services.AddSingleton<RabbitMqProducer>();
            services.AddSingleton<RabbitMqConsumer>();
            services.AddSingleton<RabbitMqDeadLetter>();

            services.AddSingleton<AwsSqsProducer>();
            services.AddSingleton<AwsSqsConsumer>();
            services.AddSingleton<AwsSqsDeadLetter>();

            services.AddSingleton<KafkaProducer>();
            services.AddSingleton<KafkaConsumer>();

            // Orchestrator
            services.AddSingleton<ConsumerHost>();

            return services;
        }
    }

    // -----------------------------------
    // Dead-letter placeholders for providers needing separate sender impl
    // -----------------------------------

    public sealed class RabbitMqDeadLetter : IDeadLetterSender
    {
        private readonly ILogger<RabbitMqDeadLetter> _log; private readonly QueueOptions _opts;
        public RabbitMqDeadLetter(ILogger<RabbitMqDeadLetter> log, IOptions<QueueOptions> opts)
        { _log = log; _opts = opts.Value; }
        public Task DeadLetterAsync(InboundMessage failedMessage, string reason, CancellationToken ct = default)
        { _log.LogError("[RabbitMQ] DLQ: {Reason} for {MessageId}", reason, failedMessage.Metadata.MessageId); return Task.CompletedTask; }
    }

    public sealed class AwsSqsDeadLetter : IDeadLetterSender
    {
        private readonly ILogger<AwsSqsDeadLetter> _log; private readonly QueueOptions _opts;
        public AwsSqsDeadLetter(ILogger<AwsSqsDeadLetter> log, IOptions<QueueOptions> opts)
        { _log = log; _opts = opts.Value; }
        public Task DeadLetterAsync(InboundMessage failedMessage, string reason, CancellationToken ct = default)
        { _log.LogError("[SQS] DLQ: {Reason} for {MessageId}", reason, failedMessage.Metadata.MessageId); return Task.CompletedTask; }
    }

    // -----------------------------------
    // Usage Example (Program.cs)
    // -----------------------------------

    public static class Example
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(b => b.AddConsole());

            services.AddQueueing(opts =>
            {
                opts.Provider = QueueProvider.RabbitMq; // switch to AzureServiceBus / AwsSqs / Kafka
                opts.ConnectionString = "amqp://user:pass@host:5672/vhost"; // change per provider
                opts.QueueOrTopicName = "orders";
                opts.DeadLetterName = "orders.dlq";
                opts.MaxConcurrentHandlers = 16;
                opts.Prefetch = 64;
                opts.MaxRetries = 5;
            });
        }

        public static async Task RunAsync(IServiceProvider sp, CancellationToken ct)
        {
            var factory = sp.GetRequiredService<IQueueClientFactory>();
            var producer = factory.CreateProducer();

            var meta = new MessageMetadata(
                MessageId: Guid.NewGuid().ToString("N"),
                CorrelationId: Guid.NewGuid().ToString("N"),
                Subject: "OrderCreated",
                ContentType: sp.GetRequiredService<IMessageSerializer>().ContentType
            );

            await producer.SendAsync(new { orderId = 1234, amount = 42.50m }, meta, ct);

            var host = sp.GetRequiredService<ConsumerHost>();
            await host.RunAsync(async msg =>
            {
                var body = Encoding.UTF8.GetString(msg.Body);
                Console.WriteLine($"Processing: {body}");
                // domain logic ...
                return ConsumeResult.Ack;
            }, ct);
        }
    }
}
