using RabbitMQ.Client;
namespace YS.EventBus.Impl.RabbitMQ
{
    public static class RabbitMQExtentions
    {
        public static void ExchangeDeclare(this IModel model, string exchange, EventType eventType)
        {
            var exchangeType = eventType == EventType.Topic ? ExchangeType.Fanout : ExchangeType.Direct;
            model.ExchangeDeclare(exchange, exchangeType, true, false, null);
        }
        public static string QueueDeclareAndBind(this IModel model, string exchange)
        {
            var queueInfo = model.QueueDeclare(exchange, true, false, false, null);
            model.QueueBind(queueInfo.QueueName, exchange, exchange, null);
            return queueInfo.QueueName;
        }
        public static string TemporaryQueueDeclareAndBind(this IModel model, string exchange)
        {
           var queueInfo= model.QueueDeclare("", true, false, true, null);
            model.QueueBind(queueInfo.QueueName, exchange, exchange, null);
            return queueInfo.QueueName;
        }
    }
}
