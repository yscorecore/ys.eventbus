using System;
using System.Threading.Tasks;

namespace YS.EventBus
{
    public enum EventType
    {
        // 单点事件
        Queue,
        // 广播事件
        Broadcast,
    }
}