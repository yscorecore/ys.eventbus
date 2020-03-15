using Microsoft.VisualStudio.TestTools.UnitTesting;
using Knife.Hosting;
using System.Collections.Generic;
using RabbitMQ.Client;
using System.Threading.Tasks;
using System.Threading;
using System;
using YS.EventBus;
using Microsoft.Extensions.DependencyInjection;
namespace YS.EventBus.Impl.RabbitMQ.UnitTest
{
    [TestClass]
    public class UnitTest1 : KnifeHost
    {
        public UnitTest1() : base(
            new Dictionary<string, object>
            {
                ["Rabbit:UserName"] = "rabbitmq",
                ["Rabbit:Password"] = "rabbitmq",
            }, (builder, sc) =>
            {
                sc.AddSingleton<IEventConsumer, DataConsumer>();

            })
        {
        }

        [TestMethod]
        public void TestMethod1()
        {
            var test = this.Get<RabbitMQEventConsumer>();

            var producer = this.Get<IEventProducer>();
            Assert.IsNotNull(producer);

            Task.WaitAll(
                Task.Run(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        producer.Enqueue(
                           new Data()
                           {
                               MyProperty = DateTime.Now.Second,
                               MyProperty2 = DateTime.Now.ToShortTimeString()
                           }
                        ).Wait();
                        Task.Delay(100).Wait();
                    }
                }),
                Task.Run(() => { test.StartAsync(default); })
            );
        }
        public class Data
        {
            public int MyProperty { get; set; }
            public string MyProperty2 { get; set; }
        }
        public class DataConsumer : BaseEventConsumer<Data>
        {
            public DataConsumer()
            {

            }
            protected override Task<bool> Handler(Data data)
            {
                Console.WriteLine($"{data.MyProperty}_{data.MyProperty2}");
                return Task.FromResult(true);
            }
        }
    }
}
