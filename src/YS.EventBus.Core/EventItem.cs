using System;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public class EventItem<T>
    {
        public string Exchange { get; set; }
        public EventType EventType { get; set; }
        public T Data { get; set; }

    }
}