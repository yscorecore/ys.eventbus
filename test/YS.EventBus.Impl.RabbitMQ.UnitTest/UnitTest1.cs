using Microsoft.VisualStudio.TestTools.UnitTesting;
using Knife.Hosting;
using System.Collections.Generic;
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
            var producer = this.Get<IEventProducer>();
            Assert.IsNotNull(producer);
            producer.Publish(new EventItem<string>()
            {
                Exchange = "abc",
                EventType = EventType.Queue,
                Data = "Hello"
            });
        }
    }
}
