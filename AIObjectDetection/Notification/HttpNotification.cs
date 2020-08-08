using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AICore.Notification
{
    class HttpNotification : INotification
    {

        public HttpNotification()
        {

        }

        public async Task SendNotification(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
