using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using YS.Knife.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace YS.EventBus.Impl.RabbitMQ
{
    [TestClass]
    public class TopicEventTest
    {

        [TestMethod]
        public void ShouldGetExpectedMessageWhenBroadcastTopicGivenOneConsume()
        {
            var consume = new DataConsumer();
            using (var host = new KnifeHost(new string[0], (build, sc) =>
                {
                    sc.AddSingleton<IEventConsumer>(consume);
                }))
            {
                var appLiftTime = host.GetService<IHostApplicationLifetime>();
                var producer = host.GetService<IEventProducer>();
                Task.WaitAll(
                    Task.Run(host.Run),
                    // wait 2000ms for the consumes ready.
                    Task.Delay(2000).ContinueWith(_ => BroadcastTopicData(producer, 100)),
                    Task.Delay(5000).ContinueWith(_ => appLiftTime.StopApplication())
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
            using (var host = new KnifeHost(new string[0], (build, sc) =>
                {
                    sc.AddSingleton<IEventConsumer>(consume);
                }))
            {
                var appLiftTime = host.GetService<IHostApplicationLifetime>();
                var producer = host.GetService<IEventProducer>();
                BroadcastTopicData(producer, 100);
                Task.WaitAll(
                     Task.Run(host.Run),
                     Task.Delay(2000).ContinueWith(_ => appLiftTime.StopApplication())
                );
            }
            Assert.AreEqual(0, consume.Received.Count);
        }

        [TestMethod]
        public void ShouldGetExpectedMessageWhenBroadcastTopicGivenMutilConsume()
        {
            var consume = new DataConsumer();
            var consume2 = new DataConsumer();
            var consume3 = new DataConsumer();
            using (var host = new KnifeHost(new string[0], (build, sc) =>
                {
                    sc.AddSingleton<IEventConsumer>(consume);
                    sc.AddSingleton<IEventConsumer>(consume2);
                    sc.AddSingleton<IEventConsumer>(consume3);
                }))
            {
                var appLiftTime = host.GetService<IHostApplicationLifetime>();
                var producer = host.GetService<IEventProducer>();
                Task.WaitAll(
                    Task.Run(host.Run),
                    // wait 800ms for the consumes ready.
                    Task.Delay(800).ContinueWith(_ => BroadcastTopicData(producer, 100)),
                    Task.Delay(2000).ContinueWith(_ => appLiftTime.StopApplication())
                );
            }
            Assert.AreEqual(100, consume.Received.Count);
            Assert.AreEqual(100, consume2.Received.Count);
            Assert.AreEqual(100, consume3.Received.Count);

        }

        private void BroadcastTopicData(IEventProducer producer, int count)
        {
            Enumerable.Range(0, count).ToList().ForEach(async (i) =>
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
            public DataConsumer() : base(EventType.Topic)
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
