namespace YS.EventBus
{
    public class EventItem
    {
        public string Exchange { get; set; }
        public EventType EventType { get; set; }
        public byte[] Data { get; set; }

    }
}
