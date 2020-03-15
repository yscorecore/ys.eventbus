using System;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public class EventBusOptions
    {
        public int MaxConsumerCount {get;set;}=16;
    }
    
}