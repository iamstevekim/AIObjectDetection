using System.Threading.Tasks;

namespace AICore.Notification
{
    interface INotification
    {
        Task SendNotification(string msg);
    }
}
