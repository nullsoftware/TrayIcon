using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NullSoftware.ToolKit
{
    public class TrayIconHandlers : FreezableCollection<TrayIcon>
    {
        private static readonly DependencyProperty TrayIconsProperty = DependencyProperty.RegisterAttached(
            "TrayIconsPrivate",
            typeof(TrayIconHandlers),
            typeof(TrayIconHandlers),
            new PropertyMetadata(default(TrayIconHandlers)));

        public static TrayIconHandlers GetTrayIcons(FrameworkElement element)
        {
            TrayIconHandlers handlers = (TrayIconHandlers)element.GetValue(TrayIconsProperty);
            if (handlers == null)
            {
                handlers = new TrayIconHandlers(element);
                element.SetValue(TrayIconsProperty, handlers);
            }

            return handlers;
        }

        private readonly FrameworkElement _owner;

        public TrayIconHandlers(FrameworkElement owner)
        {
            _owner = owner;

            var self = (INotifyCollectionChanged)this;
            self.CollectionChanged += (sender, args) =>
            {
                foreach (TrayIcon trayIcon in args.NewItems)
                {
                    if (DesignerProperties.GetIsInDesignMode(trayIcon))
                        trayIcon.Dispose();
                }
            };
        }

        /// <inheritdoc />
        protected override Freezable CreateInstanceCore()
        {
            return new TrayIconHandlers(_owner);
        }
    }
}
