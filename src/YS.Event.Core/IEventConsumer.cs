using System;
using System.Threading.Tasks;

namespace YS.Event.Core
{
    public interface IEventConsumer
    {
        Task Subscribe<T>();

    }
}
