using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public static class EventProducerExtentions
    {
        public static Task Broadcast<T>(this IEventProducer producer, T data, string exchange = null)
        {
            if (producer == null)
            {
                throw new ArgumentNullException(nameof(producer));
            }
            return producer.Publish(new EventItem
            {
                EventType = EventType.Broadcast,
                Data = ObjectToBytes(data),
                Exchange = string.IsNullOrEmpty(exchange) ? typeof(T).FullName : exchange
            });
        }
        public static Task Enqueue<T>(this IEventProducer producer, T data, string exchange = null)
        {
            if (producer == null)
            {
                throw new ArgumentNullException(nameof(producer));
            }
            return producer.Publish(new EventItem
            {
                EventType = EventType.Queue,
                Data = ObjectToBytes(data),
                Exchange = string.IsNullOrEmpty(exchange) ? typeof(T).FullName : exchange
            });
        }
        private static byte[] ObjectToBytes<T>(T data)
        {
            return JsonSerializer.SerializeToUtf8Bytes(data);
        }
    }
}
