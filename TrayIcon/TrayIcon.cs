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
using WPFBinding = System.Windows.Data.Binding;
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

        public static readonly DependencyProperty BalloonTipClickCommandProperty =
            DependencyProperty.Register(
                nameof(BalloonTipClickCommand),
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

        /// <summary>
        /// Gets or sets the time period, in milliseconds, the balloon tip should display.
        /// This parameter is deprecated as of Windows Vista.
        /// Notification display times are now based on system accessibility settings.
        /// </summary>
        public ushort ShowTimeout
        {
            get { return (ushort)GetValue(ShowTimeoutProperty); }
            set { SetValue(ShowTimeoutProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ToolTip text displayed
        /// when the mouse pointer rests on a notification area icon.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// ToolTip text is more than 63 characters long.
        /// </exception>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the current icon.
        /// </summary>
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

        public ICommand BalloonTipClickCommand
        {
            get { return (ICommand)GetValue(BalloonTipClickCommandProperty); }
            set { SetValue(BalloonTipClickCommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets path to <see cref="INotificationService"/> property
        /// to bind there current instance.
        /// </summary>
        /// <remarks>
        /// Using current property it is possible to inject
        /// <see cref="INotificationService"/> to view model.
        /// </remarks>
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
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location),
                Visible = Visibility == Visibility.Visible,
                Text = Title
            };

            // event subscription
            NotifyIcon.Disposed += (sender, e) => NotifyIcon = null;
            NotifyIcon.Click += (sender, e) => ClickCommand?.Execute(null);
            NotifyIcon.DoubleClick += (sender, e) => DoubleClickCommand?.Execute(null);
            NotifyIcon.BalloonTipClicked += (sender, e) => BalloonTipClickCommand?.Execute(null);
            WPFApplication.Current.Exit += (sender, e) => NotifyIcon?.Dispose();
        }

        ~TrayIcon()
        {
            Dispose();
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public void Notify(string title, string text)
        {
            NotifyIcon.ShowBalloonTip(ShowTimeout, title, text, ToolTipIcon.None);
        }

        /// <inheritdoc/>
        public void Notify(string title, string text, NotificationType notificationType)
        {
            NotifyIcon.ShowBalloonTip(ShowTimeout, title, text, (ToolTipIcon)notificationType);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            NotifyIcon?.Dispose();
        }

        protected void InjectServiceToSource()
        {
            DataContext.GetType()
                .GetProperty(NotificationServiceMemberPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(DataContext, this);
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
                    Debug.WriteLine($"Failed to bind service to member '{NotificationServiceMemberPath}' (DataContext is empty). Exception: {ex}");

                return false;
            }
        }

        private ContextMenu GenerateContextMenu(WPFContextMenu original)
        {
            if (original == null || original.Items.Count == 0)
                return null;

            original.SetBinding(
                FrameworkElement.DataContextProperty,
                new WPFBinding(nameof(DataContext)) { Source = this });

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
            DispatcherPriority.DataBind);
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
