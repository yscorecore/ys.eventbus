using System;
using System.Threading.Tasks;

namespace YS.Event.Core
{
    public interface IEventProducer
    {
        Task Trigger<T>(T @event);
        Task Broadcast<T>(T @enent);
    }
}
