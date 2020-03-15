using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public abstract class BaseEventConsumer<T> : IEventConsumer
    {
        public BaseEventConsumer() : this(typeof(T).FullName)
        {

        }
        public BaseEventConsumer(string exchange)
        {
            this.Exchange = exchange;
        }
        public string Exchange { get; protected set; }

        public async Task<bool> HandlerData(byte[] bytes)
        {
            try
            {
                T data = this.OnDeserialize(bytes);
                return await Handler(data);
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        protected virtual T OnDeserialize(byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes);
        }
        protected abstract Task<bool> Handler(T data);

    }
}
