using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.ObjectPool;

namespace YS.EventBus.Impl.RabbitMQ
{
    [ServiceClass(typeof(TestConsumer))]
    public class TestConsumer
    {
        private readonly RabbitOptions _rabbitSettings;

        private readonly EventBusOptions _eventBusSettings;


        private readonly IDictionary<string, IEventConsumer> allConsumers;


        private IConnection _connection;
        private IModel channelForEventing;
        public TestConsumer(IPooledObjectPolicy<IModel> objectPolicy,IOptions<EventBusOptions> eventBusOptions, IOptions<RabbitOptions> rabbitOptions, IEnumerable<IEventConsumer> eventConsumers)
        {
            _eventBusSettings = eventBusOptions.Value;
            _rabbitSettings = rabbitOptions.Value;
            allConsumers = eventConsumers.ToDictionary(p => p.Exchange);
        }

        private IConnection GetConnection()
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

        public void ReceiveMessagesWithEvents()
        {
            _connection = GetConnection();
            channelForEventing = _connection.CreateModel();
            channelForEventing.BasicQos(0, _eventBusSettings.MaxConsumerCount, false);
            DeclareQueueAndBind(channelForEventing);
            BasicConsume(channelForEventing);
        }
        private string BuildQueueName(string exchangeName)
        {
            return $"q.{exchangeName}";
        }
        void DeclareQueueAndBind(IModel channelForEventing)
        {
            foreach (var exchange in allConsumers.Keys)
            {
                var queueName = BuildQueueName(exchange);
                channelForEventing.QueueDeclare(queueName, false, false, false, null);
                channelForEventing.QueueBind(queueName, exchange, exchange, null);
            }
        }
        void BasicConsume(IModel channelForEventing)
        {
            //exposes the message handling functions as events
            var eventingBasicConsumer = new EventingBasicConsumer(channelForEventing);
            eventingBasicConsumer.Received += EventingBasicConsumer_Received;
            foreach (var exchange in allConsumers.Keys)
            {
                var queueName = BuildQueueName(exchange);
                channelForEventing.BasicConsume(queueName, false, eventingBasicConsumer);
            }
        }
        void EventingBasicConsumer_Received(object sender, BasicDeliverEventArgs basicDeliveryEventArgs)
        {
            IBasicProperties basicProperties = basicDeliveryEventArgs.BasicProperties;
            Console.WriteLine("Message received by the event based consumer. Check the debug window for details.");
            Debug.WriteLine(string.Concat("Message received from the exchange ", basicDeliveryEventArgs.Exchange));
            Debug.WriteLine(string.Concat("Content type: ", basicProperties.ContentType));
            Debug.WriteLine(string.Concat("Consumer tag: ", basicDeliveryEventArgs.ConsumerTag));
            Debug.WriteLine(string.Concat("Delivery tag: ", basicDeliveryEventArgs.DeliveryTag));
            Debug.WriteLine(string.Concat("Message: ", Encoding.UTF8.GetString(basicDeliveryEventArgs.Body)));
            //channelForEventing.BasicNack(basicDeliveryEventArgs.DeliveryTag, false,true);
            if (allConsumers.TryGetValue(basicDeliveryEventArgs.Exchange, out var consumer))
            {
                if (consumer.HandlerData(basicDeliveryEventArgs.Body).Result)
                {
                    channelForEventing.BasicAck(basicDeliveryEventArgs.DeliveryTag, false);
                }
                else
                {
                    // requeue
                    channelForEventing.BasicReject(basicDeliveryEventArgs.DeliveryTag, true);
                }
            }
            else
            {
                // requeue
                channelForEventing.BasicReject(basicDeliveryEventArgs.DeliveryTag, true);
            }
        }

        public void Dispose()
        {
            channelForEventing.Close();
            _connection.Close();
            // base.Dispose();
        }
    }
}
