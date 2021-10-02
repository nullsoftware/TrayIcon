using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NullSoftware.ToolKit
{
    internal static class Extensions
    {
        public static ToolTipIcon ToToolTipIcon(this NotificationType notificationType)
        {
            switch(notificationType)
            {
                case NotificationType.None:
                    return ToolTipIcon.None;
                case NotificationType.Information:
                    return ToolTipIcon.Info;
                case NotificationType.Warning:
                    return ToolTipIcon.Warning;
                case NotificationType.Error:
                    return ToolTipIcon.Error;
                default:
                    return ToolTipIcon.None;
            }
        }
    }
}
