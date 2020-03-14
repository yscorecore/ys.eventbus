using System;
using System.Threading.Tasks;

namespace YS.EventBus.Impl.RabbitMQ
{

    [ServiceClass]
    public class RabbitMQEventProducer : IEventProducer
    {
        public Task Broadcast<T>(T enentData)
        {
            throw new NotImplementedException();
        }

        public Task Publish<T>(T eventData)
        {
            throw new NotImplementedException();
        }
    }
}
