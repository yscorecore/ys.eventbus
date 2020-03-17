using Knife.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YS.Knife.Test;

namespace YS.EventBus.Impl.RabbitMQ
{
    [TestClass]
    public static class TestEnvironment
    {
        [AssemblyInitialize()]
        public static void Setup(TestContext testContext)
        {
            var availablePort = Utility.GetAvailableTcpPort(5672);
            var availableManagePort = Utility.GetAvailableTcpPort(15672);
            var password = Utility.NewPassword();
            StartContainer(availablePort, availableManagePort, password);
            SetRabbitInfo(availablePort, availableManagePort, password);
        }

        [AssemblyCleanup()]
        public static void TearDown()
        {
            DockerCompose.Down();
        }
        private static void StartContainer(uint port,uint managePort,string password)
        {
            DockerCompose.Up(new Dictionary<string, object>
            {
                ["RABBIT_PORT"] = port,
                ["RABBIT_MAN_PORT"] = managePort,
                ["RABBIT_PASS"] = password
            });
            // delay 30s ,wait for rabbit container ready
            Task.Delay(30000).Wait();
        }
        private static void SetRabbitInfo(uint port, uint managePort, string password)
        {
            Environment.SetEnvironmentVariable("Rabbit__UserName", "rabbitmq");
            Environment.SetEnvironmentVariable("Rabbit__Port", port.ToString());
            Environment.SetEnvironmentVariable("Rabbit__Password", password);
        }
    }
}
