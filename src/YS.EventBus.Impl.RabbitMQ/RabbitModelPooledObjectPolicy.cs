using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using YS.Knife;

namespace YS.EventBus.Impl.RabbitMQ
{
    [ServiceClass(Lifetime = ServiceLifetime.Singleton)]
    public class RabbitModelPooledObjectPolicy : IPooledObjectPolicy<IModel>
    {
        private readonly RabbitOptions rabbitSettings;

        private readonly IConnection connection;

        public RabbitModelPooledObjectPolicy(IOptions<RabbitOptions> rabbitOptions)
        {
            _ = rabbitOptions ?? throw new ArgumentNullException(nameof(rabbitOptions));
            rabbitSettings = rabbitOptions.Value;
            connection = GetConnection();
        }

        private IConnection GetConnection()
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

        public IModel Create()
        {
            return connection.CreateModel();
        }

        public bool Return(IModel obj)
        {
            if (obj != null && obj.IsOpen)
            {
                return true;
            }
            else
            {
                obj?.Dispose();
                return false;
            }
        }
    }
}
