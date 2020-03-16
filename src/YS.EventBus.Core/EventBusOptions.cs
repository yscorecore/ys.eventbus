using System;

namespace YS.EventBus
{
    [OptionsClass]
    public class EventBusOptions
    {
        public ushort MaxConsumerCount { get; set; } = 1;
    }

}
