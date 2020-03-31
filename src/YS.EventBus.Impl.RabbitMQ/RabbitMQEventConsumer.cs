using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using YS.Knife;

namespace YS.EventBus.Impl.RabbitMQ
{
    [HostedClass]

    public sealed class RabbitMQEventConsumer : BackgroundService
    {
        private readonly RabbitOptions rabbitSettings;

        private readonly EventBusOptions eventBusSettings;

        private readonly IEnumerable<IEventConsumer> allConsumers;

        private readonly ILogger logger;

        private IDictionary<string, IEventConsumer> consumerTags = new ConcurrentDictionary<string, IEventConsumer>();

        private IConnection connection;
        private IModel channel;
        public RabbitMQEventConsumer(ILogger<RabbitMQEventConsumer> logger, IOptions<EventBusOptions> eventBusOptions, IOptions<RabbitOptions> rabbitOptions, IEnumerable<IEventConsumer> eventConsumers)
        {
            if (eventBusOptions == null)
            {
                throw new ArgumentNullException(nameof(eventBusOptions));
            }
            if (rabbitOptions == null)
            {
                throw new ArgumentNullException(nameof(rabbitOptions));
            }
            this.logger = logger;
            this.eventBusSettings = eventBusOptions.Value;
            this.rabbitSettings = rabbitOptions.Value;
            this.allConsumers = eventConsumers;
        }

        private IConnection InitConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = rabbitSettings.HostName,
                UserName = rabbitSettings.UserName,
                Password = rabbitSettings.Password,
                Port = rabbitSettings.Port,
                VirtualHost = rabbitSettings.VHost,
            };
            return factory.CreateConnection();
        }


        private void InitChannel()
        {
            connection = InitConnection();
            channel = connection.CreateModel();
            channel.BasicQos(0, eventBusSettings.MaxConsumerCount, false);
        }

        private string GetQueueName(IEventConsumer consume)
        {
            //return $"{consume.Exchange}";
            if (consume.EventType == EventType.Queue)
            {
                return $"{consume.Exchange}";
            }
            else
            {
                return $"{consume.Exchange}#{consume.GetHashCode().ToString("X", CultureInfo.InvariantCulture)}";
            }

        }
        private void BasicConsume()
        {
            lock (this)
            {
                consumerTags.Clear();
                var eventingBasicConsumer = new EventingBasicConsumer(this.channel);
                eventingBasicConsumer.Received += OnConsumeDataReceived;
                foreach (var consume in allConsumers)
                {
                    channel.ExchangeDeclare(consume.Exchange, consume.EventType);

                    var queueName = consume.EventType == EventType.Queue ?
                        channel.QueueDeclareAndBind(consume.Exchange) :
                        channel.TemporaryQueueDeclareAndBind(consume.Exchange);
                    var consumeTag = channel.BasicConsume(queueName, false, eventingBasicConsumer);
                    consumerTags.Add(consumeTag, consume);
                }
            }
        }
        private void CancelConsume()
        {
            lock (this)
            {
                foreach (var consumerTag in consumerTags.Keys)
                {
                    channel.BasicCancel(consumerTag);
                }
                consumerTags.Clear();
            }
        }
        private void OnConsumeDataReceived(object sender, BasicDeliverEventArgs e)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Message received from the exchange {exchange}", e.Exchange);
                logger.LogInformation("Consumer tag: {consumerTag}", e.ConsumerTag);
                logger.LogInformation("Delivery tag: {deliveryTag}", e.DeliveryTag);
                logger.LogInformation("Message: {message}", Encoding.UTF8.GetString(e.Body));
            }

            //channelForEventing.BasicNack(basicDeliveryEventArgs.DeliveryTag, false,true);
            if (consumerTags.TryGetValue(e.ConsumerTag, out var consumer))
            {
                if (consumer.HandlerData(e.Body).Result)
                {
                    channel.BasicAck(e.DeliveryTag, false);
                }
                else
                {
                    // requeue
                    channel.BasicReject(e.DeliveryTag, true);
                }
            }
            else
            {
                logger.LogWarning($"Can not find consumer by tag '{e.ConsumerTag}'.");
                // requeue
                channel.BasicReject(e.DeliveryTag, true);
            }
        }

        public override void Dispose()
        {
            channel?.Close();
            connection?.Close();
            base.Dispose();
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.CancelConsume();
            return base.StopAsync(cancellationToken);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (this.channel == null)
            {
                this.InitChannel();
            }
            this.BasicConsume();
            return Task.CompletedTask;
        }
    }
}
