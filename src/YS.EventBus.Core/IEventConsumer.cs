using System;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public interface IEventConsumer
    {
        string Exchange { get; }
        Task<bool> HandlerData(byte[] bytes);
    }
}
