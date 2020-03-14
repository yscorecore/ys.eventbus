using System;
using System.Threading.Tasks;

namespace YS.EventBus.Impl.RabbitMQ
{

    [ServiceClass]
    public class RabbitMQEventProducer : IEventProducer
    {
        public Task Publish<T>(EventItem<T> eventItem)
        {
            throw new NotImplementedException();
        }
    }
}
