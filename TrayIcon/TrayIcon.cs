using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Threading;
using ItemCollection = System.Windows.Controls.ItemCollection;
using WPFApplication = System.Windows.Application;
using WPFContextMenu = System.Windows.Controls.ContextMenu;
using WPFMenuItem = System.Windows.Controls.MenuItem;
using WPFSeparator = System.Windows.Controls.Separator;

namespace NullSoftware.ToolKit
{
    public class TrayIcon : FrameworkElement, INotificationService, IDisposable
    {
        #region Dependency Properties Registration

        public static readonly DependencyProperty ShowTimeoutProperty =
            DependencyProperty.Register(
                nameof(ShowTimeout),
                typeof(ushort),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata((ushort)10000));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title), 
                typeof(string),
                typeof(TrayIcon), 
                new FrameworkPropertyMetadata(OnTitleChanged));

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                nameof(IconSource), 
                typeof(Stream), 
                typeof(TrayIcon),
                new FrameworkPropertyMetadata(OnIconSourceChanged));

        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.Register(
                nameof(ClickCommand), 
                typeof(ICommand), 
                typeof(TrayIcon),
                new FrameworkPropertyMetadata());

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.Register(
                nameof(DoubleClickCommand),
                typeof(ICommand),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata());

        public static readonly DependencyProperty NotificationServiceMemberPathProperty =
           DependencyProperty.Register(
               nameof(NotificationServiceMemberPath),
               typeof(string),
               typeof(TrayIcon),
               new FrameworkPropertyMetadata(OnNotificationServiceMemberPathChanged));

        #endregion

        #region Properties

        public ushort ShowTimeout
        {
            get { return (ushort)GetValue(ShowTimeoutProperty); }
            set { SetValue(ShowTimeoutProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public Stream IconSource
        {
            get { return (Stream)GetValue(IconSourceProperty); }
            set { SetValue(IconSourceProperty, value); }
        }

        public ICommand ClickCommand
        {
            get { return (ICommand)GetValue(ClickCommandProperty); }
            set { SetValue(ClickCommandProperty, value); }
        }

        public ICommand DoubleClickCommand
        {
            get { return (ICommand)GetValue(DoubleClickCommandProperty); }
            set { SetValue(DoubleClickCommandProperty, value); }
        }

        public string NotificationServiceMemberPath
        {
            get { return (string)GetValue(NotificationServiceMemberPathProperty); }
            set { SetValue(NotificationServiceMemberPathProperty, value); }
        }

        protected NotifyIcon NotifyIcon { get; private set; }

        #endregion

        #region Constructors

        static TrayIcon()
        {
            VisibilityProperty.OverrideMetadata(typeof(TrayIcon), new FrameworkPropertyMetadata(OnVisibilityChanged));
            ContextMenuProperty.OverrideMetadata(typeof(TrayIcon), new FrameworkPropertyMetadata(OnContextMenuChanged));
        }

        public TrayIcon()
        {
            // properties initialization
            NotifyIcon = new NotifyIcon()
            {
                Icon = Icon.ExtractAssociatedIcon(AppDomain.CurrentDomain.FriendlyName),
                Visible = Visibility == Visibility.Visible,
                Text = Title
            };

            // event subscription
            NotifyIcon.Disposed += (sender, e) => NotifyIcon = null;
            NotifyIcon.Click += (sender, e) => ClickCommand?.Execute(null);
            NotifyIcon.DoubleClick += (sender, e) => DoubleClickCommand?.Execute(null);
            WPFApplication.Current.Exit += (sender, e) => NotifyIcon?.Dispose();
        }

        #endregion

        #region Methods

        public void Notify(string title, string text)
        {
            NotifyIcon.ShowBalloonTip(ShowTimeout, title, text, ToolTipIcon.None);
        }

        public void Notify(string title, string text, NotificationType notificationType)
        {
            NotifyIcon.ShowBalloonTip(ShowTimeout, title, text, notificationType.ToToolTipIcon());
        }

        public void Dispose()
        {
            NotifyIcon?.Dispose();
        }

        protected void InjectServiceToSource()
        {
            DataContext.GetType().GetProperty(NotificationServiceMemberPath).SetValue(DataContext, this);
        }

        protected bool TryInjectServiceToSource()
        {
            try
            {
                InjectServiceToSource();

                return true;
            }
            catch (Exception ex)
            {
                if (DataContext != null)
                    Debug.WriteLine($"Failed to bind service to member '{NotificationServiceMemberPath}' in '{DataContext.GetType()}'. Exception: {ex}");
                else
                    Debug.WriteLine($"Failed to bind service to member '{NotificationServiceMemberPath}'. Exception: {ex}");

                return false;
            }
        }

        private ContextMenu GenerateContextMenu(WPFContextMenu original)
        {
            if (original == null || original.Items.Count == 0)
                return null;

            if (original.DataContext == null)
                original.DataContext = DataContext;

            return new ContextMenu(GenerateMenuItems(original.Items));
        }

        private MenuItem[] GenerateMenuItems(ItemCollection original)
        {
            List<MenuItem> result = new List<MenuItem>();

            foreach (FrameworkElement item in original)
            {
                switch (item)
                {
                    case WPFMenuItem menuItem:
                        result.Add(LinkMenuItem(menuItem));
                        break;
                    case WPFSeparator separator:
                        result.Add(new MenuItem("-"));
                        break;
                    default:
                        throw new NotSupportedException($"Type '{item.GetType()}' not supported.");
                }
            }

            return result.ToArray();
        }

        private MenuItem LinkMenuItem(WPFMenuItem item)
        {
            MenuItem result = new MenuItem(GetHeader(item));

            // needed to change menu item header dynamically
            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.HeaderProperty, 
                typeof(WPFMenuItem)).AddValueChanged(item, new EventHandler((sender, e) => result.Text = GetHeader(item)));

            if (item.Items.Count != 0)
            {
                result.MenuItems.AddRange(GenerateMenuItems(item.Items));

                return result;
            }

            if (item.IsCheckable)
            {
                item.AddHandler(WPFMenuItem.CheckedEvent, new RoutedEventHandler((sender, e) => result.Checked = true));
                item.AddHandler(WPFMenuItem.UncheckedEvent, new RoutedEventHandler((sender, e) => result.Checked = false));

                result.Checked = item.IsChecked;
            }

            MenuItemAutomationPeer peer = new MenuItemAutomationPeer(item);
            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;

            result.Click += (sender, e) => invokeProv.Invoke();

            return result;
        }

        private string GetHeader(WPFMenuItem item)
        {
            return item.Header?.ToString()?.Replace("_", "&");
        }

        private static void OnTitleChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrayIcon trayIcon = (TrayIcon)d;

            if (trayIcon.NotifyIcon == null)
                return;

            trayIcon.NotifyIcon.Text = (string)e.NewValue;
        }

        private static void OnIconSourceChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrayIcon trayIcon = (TrayIcon)d;

            if (trayIcon.NotifyIcon == null)
                return;

            if (trayIcon.IconSource != null)
                trayIcon.NotifyIcon.Icon = new Icon(trayIcon.IconSource, 16, 16);
        }

        private static void OnContextMenuChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrayIcon trayIcon = (TrayIcon)d;

            if (trayIcon.NotifyIcon == null)
                return;

            trayIcon.Dispatcher.BeginInvoke(new Action(() =>
            {
                trayIcon.NotifyIcon.ContextMenu = trayIcon.GenerateContextMenu((WPFContextMenu)e.NewValue);
            }),
            DispatcherPriority.Background);
        }

        private static void OnVisibilityChanged(
           DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrayIcon trayIcon = (TrayIcon)d;

            if (trayIcon.NotifyIcon == null)
                return;

            trayIcon.NotifyIcon.Visible = (Visibility)e.NewValue == Visibility.Visible;
        }

        private static void OnNotificationServiceMemberPathChanged(
           DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrayIcon trayIcon = (TrayIcon)d;

            string memberPath = (string)e.NewValue;

            if (!string.IsNullOrEmpty(memberPath))
            {
                trayIcon.Dispatcher.BeginInvoke(
                    new Func<bool>(trayIcon.TryInjectServiceToSource),
                    DispatcherPriority.DataBind);
            }
        }

        #endregion
    }
}
