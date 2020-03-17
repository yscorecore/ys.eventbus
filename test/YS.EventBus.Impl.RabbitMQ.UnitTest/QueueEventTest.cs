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
namespace YS.EventBus.Impl.RabbitMQ
{
    [TestClass]
    public class QueueEventTest
    {

        [TestMethod]
        public void ShouldGetExpectedMessageWhenEnqueueGivenOneConsume()
        {
            var consume = new DataConsumer();
            using (var host = new KnifeHost(new string[0], (build, sc) =>
                {
                    sc.AddSingleton<IEventConsumer>(consume);
                }))
            {
                var appLiftTime = host.Get<IHostApplicationLifetime>();
                var producer = host.Get<IEventProducer>();
                EnqueueData(producer, 100);
                Task.WaitAll(
                     Task.Run(host.Run),
                     // wait 200 ms to consume message
                     Task.Delay(200).ContinueWith(_ => appLiftTime.StopApplication())
                );
            }
            Assert.AreEqual(100, consume.Received.Count);
            var countByKey = consume.Received.Select(p => p.IntProp).Distinct().Count();
            Assert.AreEqual(100, countByKey);
        }

        [TestMethod]
        public void ShouldGetExpectedMessageWhenEnqueueGivenMutilConsume()
        {
            var consume1 = new DataConsumer();
            var consume2 = new DataConsumer();
            var consume3 = new DataConsumer();
            using (var host = new KnifeHost(new string[0], (build, sc) =>
                {
                    sc.AddSingleton<IEventConsumer>(consume1);
                    sc.AddSingleton<IEventConsumer>(consume2);
                    sc.AddSingleton<IEventConsumer>(consume3);
                }))
            {
                var appLiftTime = host.Get<IHostApplicationLifetime>();
                var producer = host.Get<IEventProducer>();
                EnqueueData(producer, 100);
                Task.WaitAll(
                     Task.Run(host.Run),
                     // wait 200 ms to consume message
                     Task.Delay(200).ContinueWith(_ => appLiftTime.StopApplication())
                );
            }
            Assert.IsTrue(consume1.Received.Count > 0);
            Assert.IsTrue(consume2.Received.Count > 0);
            Assert.IsTrue(consume3.Received.Count > 0);
            var consumeCount = consume1.Received.Count + consume2.Received.Count + consume3.Received.Count;
            Assert.AreEqual(100, consumeCount);
            var allReceived = consume1.Received.Concat(consume2.Received).Concat(consume3.Received);
            var countByKey = allReceived.Select(p => p.IntProp).Distinct().Count();
            Assert.AreEqual(100, countByKey);
        }

        private void EnqueueData(IEventProducer producer, int count)
        {
            Enumerable.Range(0, count).ForEach(async (i) =>
            {
                await producer.Enqueue(new Data { IntProp = i, StrProp = DateTime.Now.ToLongTimeString() });
            });
        }
        public class Data
        {
            public int IntProp { get; set; }
            public string StrProp { get; set; }
        }
        public class DataConsumer : BaseEventConsumer<Data>
        {
            public DataConsumer() : base(EventType.Queue)
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
