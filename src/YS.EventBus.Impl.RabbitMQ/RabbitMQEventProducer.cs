using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
namespace YS.EventBus.Impl.RabbitMQ
{

    [ServiceClass(Lifetime = ServiceLifetime.Singleton)]
    public class RabbitMQEventProducer : IEventProducer
    {
        private readonly DefaultObjectPool<IModel> _objectPool;
        public RabbitMQEventProducer(IPooledObjectPolicy<IModel> objectPolicy)
        {
            _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
        }
        public Task Publish<T>(EventItem<T> eventItem)
        {
            if (eventItem == null)
            {
                throw new ArgumentNullException(nameof(eventItem));
            }
            if (eventItem.Data == null)
            {
                return Task.CompletedTask;
            }
            var channel = _objectPool.Get();
            try
            {
                var exchangeType = eventItem.EventType == EventType.Broadcast ? ExchangeType.Fanout : ExchangeType.Direct;
                channel.ExchangeDeclare(eventItem.Exchange, exchangeType, true, false, null);
                var sendBytes = JsonSerializer.SerializeToUtf8Bytes(eventItem.Data);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(eventItem.Exchange, eventItem.Exchange, properties, sendBytes);
                return Task.CompletedTask;
            }
            finally
            {
                _objectPool.Return(channel);
            }
        }
    }
}
