using System.Threading.Tasks;

namespace YS.EventBus
{
    public interface IEventConsumer
    {
        string Exchange { get; }
        EventType EventType { get; }
        Task<bool> HandlerData(byte[] bytes);
    }
}
