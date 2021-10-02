using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NullSoftware.ToolKit
{
    public interface INotificationService
    {
        void Notify(string title, string text);

        void Notify(string title, string text, NotificationType notificationType);
    }
}
