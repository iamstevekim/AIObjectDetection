using System;
using System.Collections.Generic;
using System.Text;

namespace AICore.Notification
{
    class NotificationFactory
    {
        public static INotification CreateNotifier()
        {
            return new HttpNotification();
            // return new MqttNotification();
        }
    }
}
