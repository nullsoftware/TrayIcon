using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NullSoftware.ToolKit.Extensions
{
    public static class MenuItemExtensions
    {
        public static readonly DependencyProperty IsDefaultProperty
            = DependencyProperty.RegisterAttached(
                "IsDefault", 
                typeof(bool), 
                typeof(MenuItemExtensions),
                new FrameworkPropertyMetadata(false));

        public static void SetIsDefault(DependencyObject element, bool value)
        {
            element.SetValue(IsDefaultProperty, value);
        }

        public static bool GetIsDefault(DependencyObject element)
        {
            return (bool)element.GetValue(IsDefaultProperty);
        }
    }
}
