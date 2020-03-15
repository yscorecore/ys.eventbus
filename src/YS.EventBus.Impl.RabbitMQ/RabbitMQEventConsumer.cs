using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace YS.EventBus.Impl.RabbitMQ
{
    [ServiceClass(typeof(RabbitMQEventConsumer))]
    public sealed class RabbitMQEventConsumer:BackgroundService
    {
        private readonly RabbitOptions _rabbitSettings;

        private readonly EventBusOptions _eventBusSettings;

        private readonly IDictionary<string, IEventConsumer> allConsumers;

        private List<string> consumerTags = new List<string>();

        private IConnection connection;
        private IModel channel;
        public RabbitMQEventConsumer(IOptions<EventBusOptions> eventBusOptions, IOptions<RabbitOptions> rabbitOptions, IEnumerable<IEventConsumer> eventConsumers)
        {
            if (eventBusOptions == null)
            {
                throw new ArgumentNullException(nameof(eventBusOptions));
            }
            if (rabbitOptions == null)
            {
                throw new ArgumentNullException(nameof(rabbitOptions));
            }
            _eventBusSettings = eventBusOptions.Value;
            _rabbitSettings = rabbitOptions.Value;
            allConsumers = eventConsumers.ToDictionary(p => p.Exchange);
        }

        private IConnection InitConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _rabbitSettings.HostName,
                UserName = _rabbitSettings.UserName,
                Password = _rabbitSettings.Password,
                Port = _rabbitSettings.Port,
                VirtualHost = _rabbitSettings.VHost,
            };
            return factory.CreateConnection();
        }


        private void InitChannel()
        {
            connection = InitConnection();
            channel = connection.CreateModel();
            channel.BasicQos(0, _eventBusSettings.MaxConsumerCount, false);
        }
        private void DeclareQueueAndBind()
        {
            foreach (var exchange in allConsumers.Keys)
            {
                var queueName = $"q.{exchange}";
                channel.QueueDeclare(queueName, false, false, false, null);
                channel.QueueBind(queueName, exchange, exchange, null);
            }
        }
        private void BasicConsume()
        {
            lock (this)
            {
                consumerTags.Clear();
                var eventingBasicConsumer = new EventingBasicConsumer(this.channel);
                eventingBasicConsumer.Received += EventingBasicConsumer_Received;
                foreach (var exchange in allConsumers.Keys)
                {
                    var queueName = $"q.{exchange}";
                    var consumeTag = channel.BasicConsume(queueName, false, eventingBasicConsumer);
                    consumerTags.Add(consumeTag);
                }
            }

        }
        private  void CancelConsume()
        {
            lock (this)
            {
                foreach (var consumerTag in consumerTags)
                {
                    channel.BasicCancel(consumerTag);
                }
                consumerTags.Clear();
            }

        }
        void EventingBasicConsumer_Received(object sender, BasicDeliverEventArgs basicDeliveryEventArgs)
        {
            IBasicProperties basicProperties = basicDeliveryEventArgs.BasicProperties;
            Console.WriteLine("Message received by the event based consumer. Check the debug window for details.");
            Debug.WriteLine(string.Concat("Message received from the exchange ", basicDeliveryEventArgs.Exchange));
            Debug.WriteLine(string.Concat("Consumer tag: ", basicDeliveryEventArgs.ConsumerTag));
            Debug.WriteLine(string.Concat("Delivery tag: ", basicDeliveryEventArgs.DeliveryTag));
            Debug.WriteLine(string.Concat("Message: ", Encoding.UTF8.GetString(basicDeliveryEventArgs.Body)));
            //channelForEventing.BasicNack(basicDeliveryEventArgs.DeliveryTag, false,true);
            if (allConsumers.TryGetValue(basicDeliveryEventArgs.Exchange, out var consumer))
            {
                if (consumer.HandlerData(basicDeliveryEventArgs.Body).Result)
                {
                    channel.BasicAck(basicDeliveryEventArgs.DeliveryTag, false);
                }
                else
                {
                    // requeue
                    channel.BasicReject(basicDeliveryEventArgs.DeliveryTag, true);
                }
            }
            else
            {
                // requeue
                channel.BasicReject(basicDeliveryEventArgs.DeliveryTag, true);
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
            this.DeclareQueueAndBind();
            this.BasicConsume();
            return Task.CompletedTask;
        }
    }
}
