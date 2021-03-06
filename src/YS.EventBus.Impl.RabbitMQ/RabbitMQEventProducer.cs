﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using YS.Knife;

namespace YS.EventBus.Impl.RabbitMQ
{

    [ServiceClass(Lifetime = ServiceLifetime.Singleton)]
    public class RabbitMQEventProducer : IEventProducer
    {
        private readonly DefaultObjectPool<IModel> _objectPool;
        public RabbitMQEventProducer(IPooledObjectPolicy<IModel> objectPolicy)
        {
            _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
        }
        public Task Publish(EventItem eventItem)
        {
            _ = eventItem ?? throw new ArgumentNullException(nameof(eventItem));
            if (eventItem.Data == null)
            {
                return Task.CompletedTask;
            }
            var channel = _objectPool.Get();
            try
            {
                channel.ExchangeDeclare(eventItem.Exchange, eventItem.EventType);

                if (eventItem.EventType == EventType.Queue)
                {
                    // 声明queue，防止还没有Consumer的时候消息丢失
                    channel.QueueDeclareAndBind(eventItem.Exchange);
                }


                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(eventItem.Exchange, eventItem.Exchange, properties, eventItem.Data);
                return Task.CompletedTask;
            }
            finally
            {
                _objectPool.Return(channel);
            }
        }
    }
}
