using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Knife.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace YS.EventBus.Impl.RabbitMQ.UnitTest
{
    [TestClass]
    public class TopicEventTest
    {

        [TestMethod]
        public void ShouldGetExpectedMessageWhenBroadcastTopicGivenOneConsume()
        {
            var consume = new DataConsumer();
            using (var host = new KnifeHost(
                new Dictionary<string, object>
                {
                    ["Rabbit:UserName"] = "rabbitmq",
                    ["Rabbit:Password"] = "rabbitmq",
                }, (build, sc) =>
                {
                    sc.AddSingleton<IEventConsumer>(consume);
                }))
            {
                var appLiftTime = host.Get<IHostApplicationLifetime>();
                var producer = host.Get<IEventProducer>();
                BroadcastTopicData(producer, 1);// this dada will lost
                Task.Delay(500).Wait();
                Task.WaitAll(
                     Task.Run(host.Run),
                     Task.Run(() => { Task.Delay(500); BroadcastTopicData(producer, 1000); }),
                     Task.Run(async () => { await Task.Delay(5000); appLiftTime.StopApplication(); })
                );
            }
            Assert.AreEqual(100, consume.Received.Count);
            var countByKey = consume.Received.Select(p => p.IntProp).Distinct().Count();
            Assert.AreEqual(100, countByKey);
        }


        [TestMethod]
        public void ShouldGetZeroMessageWhenBroadcastTopicBeforeConsume()
        {
            var consume = new DataConsumer();
            using (var host = new KnifeHost(
                new Dictionary<string, object>
                {
                    ["Rabbit:UserName"] = "rabbitmq",
                    ["Rabbit:Password"] = "rabbitmq",
                }, (build, sc) =>
                {
                    sc.AddSingleton<IEventConsumer>(consume);
                }))
            {
                var appLiftTime = host.Get<IHostApplicationLifetime>();
                var producer = host.Get<IEventProducer>();
                BroadcastTopicData(producer, 100);
                Task.WaitAll(
                     Task.Run(host.Run),
                     Task.Run(async () => { await Task.Delay(1000); appLiftTime.StopApplication(); })
                );
            }
            Assert.AreEqual(0, consume.Received.Count);
        }
        [TestMethod]
        public void ShouldGetExpectedMessageWhenBroadcastTopicGivenMutilConsume()
        {
            var consume1 = new DataConsumer();
            var consume2 = new DataConsumer();
            var consume3 = new DataConsumer();
            using (var host = new KnifeHost(
                new Dictionary<string, object>
                {
                    ["Rabbit:UserName"] = "rabbitmq",
                    ["Rabbit:Password"] = "rabbitmq",
                }, (build, sc) =>
                {
                    sc.AddSingleton<IEventConsumer>(consume1);
                    sc.AddSingleton<IEventConsumer>(consume2);
                    sc.AddSingleton<IEventConsumer>(consume3);
                }))
            {
                var appLiftTime = host.Get<IHostApplicationLifetime>();

                var producer = host.Get<IEventProducer>();
                Task.WaitAll(
                     Task.Run(host.Run),
                     Task.Run(() => { Task.Delay(2000); BroadcastTopicData(producer, 100); }),
                     Task.Run(async () => { await Task.Delay(5000); appLiftTime.StopApplication(); })
                );
            }
            Assert.AreEqual(100, consume1.Received.Count);
            Assert.AreEqual(100, consume2.Received.Count);
            Assert.AreEqual(100, consume3.Received.Count);
          
        }

        private void BroadcastTopicData(IEventProducer producer, int count)
        {
            Enumerable.Range(0, count).ForEach(async (i) =>
            {
                await producer.BroadcastTopic(new Data { IntProp = i, StrProp = DateTime.Now.ToLongTimeString() });
            });
        }
        public class Data
        {
            public int IntProp { get; set; }
            public string StrProp { get; set; }
        }
        public class DataConsumer : BaseEventConsumer<Data>
        {
            public DataConsumer():base(EventType.Topic)
            {

            }
            public ConcurrentBag<Data> Received { get; private set; } = new ConcurrentBag<Data>();
            public int MyProperty { get; set; }
            protected override Task<bool> Handler(Data data)
            {
                Received.Add(data);
                Console.WriteLine($"{data.IntProp}_{data.StrProp}");
                return Task.FromResult(true);
            }

            protected override void OnHanderException(Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }
    }
}
