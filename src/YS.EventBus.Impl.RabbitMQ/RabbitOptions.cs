using System;
using System.Threading.Tasks;
using YS.Knife;

namespace YS.EventBus.Impl.RabbitMQ
{
    [OptionsClass]
    public class RabbitOptions
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string HostName { get; set; } = "localhost";

        public int Port { get; set; } = 5672;

        public string VHost { get; set; } = "/";
    }
}
