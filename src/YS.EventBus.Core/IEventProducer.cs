using System;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public interface IEventProducer
    {
        Task Publish<T>(T @event);
        Task Broadcast<T>(T @enent);
    }
}
