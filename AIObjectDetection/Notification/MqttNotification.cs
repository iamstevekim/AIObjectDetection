using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AICore.Notification
{
    class MqttNotification : INotification
    {
        public async Task SendNotification(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
