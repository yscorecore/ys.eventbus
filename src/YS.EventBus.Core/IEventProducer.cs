using System;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public interface IEventProducer
    {
        Task Publish<T>(EventItem<T> eventItem);
    }
}
