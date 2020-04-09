using System.Threading.Tasks;

namespace YS.EventBus
{
    public interface IEventProducer
    {
        Task Publish(EventItem eventItem);
    }
}
