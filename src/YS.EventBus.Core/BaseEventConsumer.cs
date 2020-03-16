using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public abstract class BaseEventConsumer<T> : IEventConsumer
    {
        public BaseEventConsumer(EventType eventType) : this(typeof(T).FullName, eventType)
        {
        }
        public BaseEventConsumer(string exchange, EventType eventType)
        {
            this.Exchange = exchange;
            this.EventType = eventType;
        }
        public virtual string Exchange { get; protected set; }

        public virtual EventType EventType { get; protected set; }
        public async Task<bool> HandlerData(byte[] bytes)
        {
            try
            {
                T data = this.OnDeserialize(bytes);
                return await Handler(data);
            }
#pragma warning disable CA1031 // 不捕获常规异常类型
            catch (Exception ex)
#pragma warning restore CA1031 // 不捕获常规异常类型
            {
                this.OnHanderException(ex);
                return false;
            }
        }

        protected virtual T OnDeserialize(byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes);
        }
        protected abstract Task<bool> Handler(T data);
        protected abstract void OnHanderException(Exception exception);

    }
}
