using Microsoft.VisualStudio.TestTools.UnitTesting;
using Knife.Hosting;
namespace YS.EventBus.Core.UnitTest
{
    [TestClass]
    public class UnitTest : KnifeHost
    {
        [TestMethod]
        public void TestMethod1()
        {
            var producer = this.Get<IEventProducer>();
            var consumer = this.Get<IEventConsumer>();
            //producer.Publish("Hello,World");
            //consumer.Subscribe<Message>();

        }

        public class Message
        {
            public int Version { get; set; }
            public string Name { get; set; }
        }
    }
}
