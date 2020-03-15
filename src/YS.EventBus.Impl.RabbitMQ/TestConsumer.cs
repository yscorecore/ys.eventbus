using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace YS.EventBus.Impl.RabbitMQ
{
    [ServiceClass(typeof(TestConsumer))]
    public class TestConsumer
    {
        private readonly RabbitOptions _options;

        private readonly IConnection _connection;

        public TestConsumer(IOptions<RabbitOptions> optionsAccs)
        {
            _options = optionsAccs.Value;
            _connection = GetConnection();
        }

        private IConnection GetConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                Port = _options.Port,
                VirtualHost = _options.VHost,
            };

            return factory.CreateConnection();
        }
        IModel channelForEventing;
        public void ReceiveMessagesWithEvents()
        {
            IConnection connection = GetConnection();
            channelForEventing = connection.CreateModel();
            channelForEventing.BasicQos(0, 1, false);
            DeclareQueueAndBind(channelForEventing);
            BasicConsume(channelForEventing);
        }
        void DeclareQueueAndBind(IModel channelForEventing){
            channelForEventing.QueueDeclare("xxx", false, false, false, null);
            channelForEventing.QueueBind("xxx", "mycompany.queues.accounting", "mycompany.queues.accounting", null);

            channelForEventing.QueueDeclare("yyy", false, false, false, null);
            channelForEventing.QueueBind("yyy", "mycompany.queues.accounting2", "mycompany.queues.accounting2", null);
        }
        void BasicConsume(IModel channelForEventing){
             //exposes the message handling functions as events
            EventingBasicConsumer eventingBasicConsumer = new EventingBasicConsumer(channelForEventing);
            eventingBasicConsumer.Received += EventingBasicConsumer_Received;
            channelForEventing.BasicConsume("xxx", false, eventingBasicConsumer);
            channelForEventing.BasicConsume("yyy", false, eventingBasicConsumer);
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
            channelForEventing.BasicAck(basicDeliveryEventArgs.DeliveryTag, false);
        }

        public void Dispose()
        {
            channelForEventing.Close();
            _connection.Close();
           // base.Dispose();
        }
    }
}