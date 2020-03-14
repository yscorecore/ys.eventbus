using System;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public interface IEventConsumer
    {
        Task Subscribe<T>();

    }
}
