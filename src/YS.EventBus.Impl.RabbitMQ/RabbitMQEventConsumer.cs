using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;

namespace YS.EventBus.Impl.RabbitMQ
{

    public class BaseEventConsumer<T> : BackgroundService
    where T : class
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
    public class RabbitMQEventConsumer<T> : BaseEventConsumer<T>
    where T : class
    {
        private readonly ILogger _logger;
        private IConnection _connection;
        private IModel _channel;
        private readonly RabbitOptions _options;
        public RabbitMQEventConsumer(IOptions<RabbitOptions> optionsAccs, ILogger<RabbitMQEventConsumer<T>> logger)
        {
            this._options = optionsAccs.Value;
            this._logger = logger;
            InitRabbitMQ();
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
        private void InitRabbitMQ()
        {
            // create connection  
            _connection = GetConnection();

            // create channel  
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("demo.exchange", ExchangeType.Direct);
            _channel.QueueDeclare("xxxxxx", false, false, false, null);
            _channel.QueueBind("xxxxxx", "demo.exchange", "demo.exchange", null);
            _channel.BasicQos(0, 1, false);

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                
                // received message  
                var content = System.Text.Encoding.UTF8.GetString(ea.Body);

                // handle the received message  
                HandleMessage(content);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;
            
            _channel.BasicConsume("demo.queue.log", false, consumer);
            return Task.CompletedTask;
        }

        private void HandleMessage(string content)
        {
            // we just print this message   
            _logger.LogError($"consumer received {content}");
        }

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }

}