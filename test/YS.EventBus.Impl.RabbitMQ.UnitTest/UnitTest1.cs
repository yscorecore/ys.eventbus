using Microsoft.VisualStudio.TestTools.UnitTesting;
using Knife.Hosting;
using System.Collections.Generic;
using RabbitMQ.Client;
using System.Threading.Tasks;
using System.Threading;
using System;
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
            })
        {
        }

        [TestMethod]
        public void TestMethod1()
        {
            var test = this.Get<TestConsumer>();

            var producer = this.Get<IEventProducer>();
            Assert.IsNotNull(producer);

            Task.WaitAll(
                //Task.Delay(50000),
                Task.Run(() => { test.ReceiveMessagesWithEvents(); }),
                Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        producer.Publish(new EventItem<Data>()
                        {
                            Exchange = "mycompany.queues.accounting",
                            EventType = EventType.Queue ,
                            Data = new Data()
                            {
                                MyProperty = DateTime.Now.Second,
                                MyProperty2 = DateTime.Now.ToShortTimeString()
                            }
                        });
                        Task.Delay(1000).Wait();
                    }
                }),
                Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        producer.Publish(new EventItem<Data>()
                        {
                            Exchange = "mycompany.queues.accounting2",
                            EventType = EventType.Queue ,
                            Data = new Data()
                            {
                                MyProperty =123,
                                MyProperty2 = DateTime.Now.ToString()
                            }
                        });
                        Task.Delay(1500).Wait();
                    }
                })
            );
        }
        public class Data
        {
            public int MyProperty { get; set; }
            public string MyProperty2 { get; set; }
        }
    }
}
