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
using WPFMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WinFormsMouseEventArgs = System.Windows.Forms.MouseEventArgs;

#pragma warning disable CS0612 // Type or member is obsolete

namespace NullSoftware.ToolKit
{
    /// <summary>
    /// Specifies a component that creates an icon in the notification area.
    /// </summary>
    [DefaultEvent(nameof(Click))]
    public class TrayIcon : FrameworkElement, INotificationService, IDisposable
    {
        //
        // [Dependency properties]
        //

        #region ShowTimeout

        /// <summary>
        /// Identifies the <see cref="ShowTimeout"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTimeoutProperty =
            DependencyProperty.Register(
                nameof(ShowTimeout),
                typeof(ushort),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata((ushort)10000));

        /// <summary>
        /// Gets or sets the time period, in milliseconds, the balloon tip should display.
        /// This parameter is deprecated as of Windows Vista.
        /// Notification display times are now based on system accessibility settings.
        /// </summary>
        [Obsolete]
        [Description("Gets or sets the time period, in milliseconds, the balloon tip should display. This parameter is deprecated as of Windows Vista.")]
        public ushort ShowTimeout
        {
            get { return (ushort)GetValue(ShowTimeoutProperty); }
            set { SetValue(ShowTimeoutProperty, value); }
        }

        #endregion

        #region ContextMenuVariation

#if !NETCOREAPP3_1_OR_GREATER
        private const ContextMenuVariation DefaultVariation = ContextMenuVariation.ContextMenu;
#else
        private const ContextMenuVariation DefaultVariation = ContextMenuVariation.ContextMenuStrip;
#endif

        /// <summary>
        /// Identifies the <see cref="ContextMenuVariation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContextMenuVariationProperty =
            DependencyProperty.Register(
                nameof(ContextMenuVariation),
                typeof(ContextMenuVariation),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata(DefaultVariation, OnContextMenuVariationChanged));

        /// <summary>
        /// Gets or sets context menu variation. 
        /// This property affects context menu generation.
        /// </summary>
        [Category("Common")]
        [Description("Gets or sets context menu variation.")]
        public ContextMenuVariation ContextMenuVariation
        {
            get { return (ContextMenuVariation)GetValue(ContextMenuVariationProperty); }
            set { SetValue(ContextMenuVariationProperty, value); }
        }

        #endregion

        #region Title

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title), 
                typeof(string),
                typeof(TrayIcon), 
                new FrameworkPropertyMetadata(OnTitleChanged));

        /// <summary>
        /// Gets or sets the ToolTip text displayed
        /// when the mouse pointer rests on a notification area icon.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// ToolTip text is more than 63 characters long.
        /// </exception>
        [Category("Common")]
        [Description("Gets or sets the ToolTip text displayed when the mouse pointer rests on a notification area icon.")]
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        #endregion

        #region IconSource

        /// <summary>
        /// Identifies the <see cref="IconSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                nameof(IconSource), 
                typeof(ImageSource), 
                typeof(TrayIcon),
                new FrameworkPropertyMetadata(OnIconSourceChanged));

        /// <summary>
        /// Gets or sets the icon displayed in tray.
        /// </summary>
        [Category("Common")]
        [Description("Gets or sets the icon displayed in tray.")]
        public ImageSource IconSource
        {
            get { return (ImageSource)GetValue(IconSourceProperty); }
            set { SetValue(IconSourceProperty, value); }
        }

        #endregion

        #region ClickCommand

        /// <summary>
        /// Identifies the <see cref="ClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.Register(
                nameof(ClickCommand), 
                typeof(ICommand), 
                typeof(TrayIcon),
                new FrameworkPropertyMetadata());

        /// <summary>
        /// Gets or sets the command to invoke when this Tray Icon is pressed.
        /// </summary>
        [Bindable(true)]
        [Category("Action")]
        public ICommand ClickCommand
        {
            get { return (ICommand)GetValue(ClickCommandProperty); }
            set { SetValue(ClickCommandProperty, value); }
        }

        #endregion

        #region ClickCommandParameter

        /// <summary>
        /// Identifies the <see cref="ClickCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClickCommandParameterProperty =
            DependencyProperty.Register(
                nameof(ClickCommandParameter),
                typeof(object),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata());

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="ClickCommand"/> property.
        /// </summary>
        [Bindable(true)]
        [Category("Action")]
        public object ClickCommandParameter
        {
            get { return GetValue(ClickCommandParameterProperty); }
            set { SetValue(ClickCommandParameterProperty, value); }
        }

        #endregion

        #region DoubleClickCommand

        /// <summary>
        /// Identifies the <see cref="DoubleClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.Register(
                nameof(DoubleClickCommand),
                typeof(ICommand),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata());

        /// <summary>
        /// Gets or sets the command to invoke when this Tray Icon is clicked two or more times.
        /// </summary>
        [Bindable(true)]
        [Category("Action")]
        public ICommand DoubleClickCommand
        {
            get { return (ICommand)GetValue(DoubleClickCommandProperty); }
            set { SetValue(DoubleClickCommandProperty, value); }
        }

        #endregion

        #region DoubleClickCommandParameter

        /// <summary>
        /// Identifies the <see cref="DoubleClickCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandParameterProperty =
            DependencyProperty.Register(
                nameof(DoubleClickCommandParameter),
                typeof(object),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata());

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="DoubleClickCommand"/> property.
        /// </summary>
        [Bindable(true)]
        [Category("Action")]
        public object DoubleClickCommandParameter
        {
            get { return GetValue(DoubleClickCommandParameterProperty); }
            set { SetValue(DoubleClickCommandParameterProperty, value); }
        }

        #endregion

        #region BalloonTipClickCommand

        /// <summary>
        /// Identifies the <see cref="BalloonTipClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BalloonTipClickCommandProperty =
            DependencyProperty.Register(
                nameof(BalloonTipClickCommand),
                typeof(ICommand),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata());

        /// <summary>
        /// Gets or sets the command to invoke when balloon tip is pressed.
        /// </summary>
        [Bindable(true)]
        [Category("Action")]
        public ICommand BalloonTipClickCommand
        {
            get { return (ICommand)GetValue(BalloonTipClickCommandProperty); }
            set { SetValue(BalloonTipClickCommandProperty, value); }
        }

        #endregion

        #region BalloonTipClickCommandParameter

        /// <summary>
        /// Identifies the <see cref="BalloonTipClickCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BalloonTipClickCommandParameterProperty =
            DependencyProperty.Register(
                nameof(BalloonTipClickCommandParameter),
                typeof(object),
                typeof(TrayIcon),
                new FrameworkPropertyMetadata());

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="BalloonTipClickCommand"/> property.
        /// </summary>
        [Bindable(true)]
        [Category("Action")]
        public object BalloonTipClickCommandParameter
        {
            get { return GetValue(BalloonTipClickCommandParameterProperty); }
            set { SetValue(BalloonTipClickCommandParameterProperty, value); }
        }

        #endregion

        #region NotificationServiceMemberPath

        /// <summary>
        /// Identifies the <see cref="NotificationServiceMemberPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NotificationServiceMemberPathProperty =
           DependencyProperty.Register(
               nameof(NotificationServiceMemberPath),
               typeof(string),
               typeof(TrayIcon),
               new FrameworkPropertyMetadata(OnNotificationServiceMemberPathChanged));

        /// <summary>
        /// Gets or sets path to <see cref="INotificationService"/> property
        /// to bind there current instance.
        /// </summary>
        /// <remarks>
        /// Using current property it is possible to inject
        /// <see cref="INotificationService"/> to view model.
        /// </remarks>
        [Category("Common")]
        [Description("Gets or sets path to INotificationService property to bind there current instance.")]
        public string NotificationServiceMemberPath
        {
            get { return (string)GetValue(NotificationServiceMemberPathProperty); }
            set { SetValue(NotificationServiceMemberPathProperty, value); }
        }

        #endregion

        #region IsDefault attached property

        /// <summary>
        /// Identifies the NullSoftware.ToolKit.TrayIcon.IsDefault attached property.
        /// </summary>
        public static readonly DependencyProperty IsDefaultProperty =
            DependencyProperty.RegisterAttached(
               "IsDefault",
               typeof(bool),
               typeof(TrayIcon),
               new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets the value of the NullSoftware.ToolKit.TrayIcon.IsDefault 
        /// attached property from a given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>
        /// The value of the NullSoftware.ToolKit.TrayIcon.IsDefault attached property.
        /// </returns>
        public static bool GetIsDefault(DependencyObject element)
        {
            return (bool)element.GetValue(IsDefaultProperty);
        }

        /// <summary>
        /// Sets the value of the NullSoftware.ToolKit.TrayIcon.IsDefault 
        /// attached property to a given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="element">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetIsDefault(DependencyObject element, bool value)
        {
            element.SetValue(IsDefaultProperty, value);
        }

        #endregion


        //
        // [Routed Events]
        //

        #region Click

        /// <summary>
        /// Identifies the <see cref="Click"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ClickEvent = 
            EventManager.RegisterRoutedEvent(
                nameof(Click),
                RoutingStrategy.Bubble,
                typeof(MouseButtonEventHandler),
                typeof(TrayIcon));

        /// <summary>
        /// Occurs when a <see cref="TrayIcon"/> is clicked.
        /// </summary>
        [Category("Behavior")]
        public event MouseButtonEventHandler Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        #endregion

        #region MouseDoubleClick

        /// <summary>
        /// Identifies the <see cref="MouseDoubleClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent MouseDoubleClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(MouseDoubleClick),
                RoutingStrategy.Bubble,
                typeof(MouseButtonEventHandler),
                typeof(TrayIcon));

        /// <summary>
        /// Occurs when a <see cref="TrayIcon"/> is double-clicked.
        /// </summary>
        [Category("Behavior")]
        public event MouseButtonEventHandler MouseDoubleClick
        {
            add { AddHandler(MouseDoubleClickEvent, value); }
            remove { RemoveHandler(MouseDoubleClickEvent, value); }
        }

        #endregion

        #region BalloonTipClick

        /// <summary>
        /// Identifies the <see cref="BalloonTipClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent BalloonTipClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(BalloonTipClick),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(TrayIcon));

        /// <summary>
        /// Occurs when the balloon tip is clicked.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler BalloonTipClick
        {
            add { AddHandler(BalloonTipClickEvent, value); }
            remove { RemoveHandler(BalloonTipClickEvent, value); }
        }

        #endregion

        #region BalloonTipShown

        /// <summary>
        /// Identifies the <see cref="BalloonTipShown"/> routed event.
        /// </summary>
        public static readonly RoutedEvent BalloonTipShownEvent =
           EventManager.RegisterRoutedEvent(
               nameof(BalloonTipShown),
               RoutingStrategy.Bubble,
               typeof(RoutedEventHandler),
               typeof(TrayIcon));

        /// <summary>
        /// Occurs when the balloon tip is displayed on the screen.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler BalloonTipShown
        {
            add { AddHandler(BalloonTipShownEvent, value); }
            remove { RemoveHandler(BalloonTipShownEvent, value); }
        }

        #endregion

        #region BalloonTipClosed

        /// <summary>
        /// Identifies the <see cref="BalloonTipClosed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent BalloonTipClosedEvent =
           EventManager.RegisterRoutedEvent(
               nameof(BalloonTipClosed),
               RoutingStrategy.Bubble,
               typeof(RoutedEventHandler),
               typeof(TrayIcon));

        /// <summary>
        /// Occurs when the balloon tip is closed by the user.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler BalloonTipClosed
        {
            add { AddHandler(BalloonTipClosedEvent, value); }
            remove { RemoveHandler(BalloonTipClosedEvent, value); }
        }

        #endregion

        

        #region Properties

        /// <summary>
        /// Wrapped NotifyIcon.
        /// </summary>
        protected NotifyIcon NotifyIcon { get; private set; }

        #endregion

        #region Constructors

        static TrayIcon()
        {
            VisibilityProperty.OverrideMetadata(typeof(TrayIcon), new FrameworkPropertyMetadata(OnVisibilityChanged));
            ContextMenuProperty.OverrideMetadata(typeof(TrayIcon), new FrameworkPropertyMetadata(OnContextMenuChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayIcon"/> class.
        /// </summary>
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
            NotifyIcon.BalloonTipClicked += OnNotifyIconBalloonTipClicked;
            NotifyIcon.BalloonTipShown += OnNotifyIconBalloonTipShown;
            NotifyIcon.BalloonTipClosed += OnNotifyIconBalloonTipClosed;
            NotifyIcon.MouseClick += OnNotifyIconMouseClick;
            NotifyIcon.MouseDoubleClick += OnNotifyIconMouseDoubleClick;
            NotifyIcon.MouseDown += OnNotifyIconMouseDown;
            NotifyIcon.MouseUp += OnNotifyIconMouseUp;
            NotifyIcon.MouseMove += OnNotifyIconMouseMove;
            WPFApplication.Current.Exit += (sender, e) => NotifyIcon?.Dispose();
        }

        /// <summary>
        /// Disposes inner NotifyIcon.
        /// </summary>
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

        /// <summary>
        /// Injects <see cref="INotificationService"/> to DataContext by <see cref="NotificationServiceMemberPath"/>.
        /// </summary>
        protected void InjectServiceToSource()
        {
            DataContext.GetType()
                .GetProperty(NotificationServiceMemberPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(DataContext, this);
        }

        /// <summary>
        /// Tries to inject <see cref="INotificationService"/> to DataContext by <see cref="NotificationServiceMemberPath"/>.
        /// </summary>
        /// <returns>true if injection was successful; otherwise, false.</returns>
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

        private static void OnContextMenuVariationChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrayIcon trayIcon = (TrayIcon)d;

            if (trayIcon.IsInitialized && trayIcon.IsLoaded)
                trayIcon.GenerateContextMenu();
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
            {
                switch (trayIcon.IconSource)
                {
                    case BitmapFrame frame:
                        trayIcon.NotifyIcon.Icon = new Icon(WPFApplication.GetResourceStream(new Uri(frame.Decoder.ToString())).Stream, 16, 16);
                        break;
                    case BitmapImage bitmapImg:
                        trayIcon.NotifyIcon.Icon = new Icon(bitmapImg.StreamSource, 16, 16);
                        break;
                    default:
                        throw new NotSupportedException("Icon Source supports only BitmapFrame or BitmapImage.");
                }
            }
        }

        private static void OnContextMenuChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrayIcon trayIcon = (TrayIcon)d;

            if (trayIcon.NotifyIcon == null)
                return;

            trayIcon.Dispatcher.BeginInvoke(new Action(trayIcon.GenerateContextMenu), DispatcherPriority.DataBind);
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

        private static string GetHeader(WPFMenuItem item)
        {
            return item.Header?.ToString()?.Replace("_", "&");
        }

        private static MouseButton ToMouseButton(MouseButtons btn)
        {
            switch (btn)
            {
                case MouseButtons.Left: return MouseButton.Left;
                case MouseButtons.Right: return MouseButton.Right;
                case MouseButtons.Middle: return MouseButton.Middle;
                case MouseButtons.XButton1: return MouseButton.XButton1;
                case MouseButtons.XButton2: return MouseButton.XButton2;
                default:
                    throw new NotSupportedException($"Can not convert System.Windows.Forms.MouseButtons.{btn} to System.Windows.Input.MouseButton.");
            }
        }

        private static MouseButtonEventArgs ToMouseButtonEventArgs(WinFormsMouseEventArgs e, RoutedEvent routedEvent)
        {
            return new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, ToMouseButton(e.Button))
            {
                RoutedEvent = routedEvent
            };
        }

        /// <summary>
        /// Converts <see cref="WPFContextMenu"/> to Winndows Forms variation.
        /// </summary>
        protected virtual void GenerateContextMenu()
        {
            switch (ContextMenuVariation)
            {
                case ContextMenuVariation.ContextMenu:
#if !NETCOREAPP3_1_OR_GREATER
                    NotifyIcon.ContextMenu = GenerateContextMenu(ContextMenu);
#else
                    throw new NotSupportedException("ContextMenu is not supported in current .NET version.");
#endif
                    return;
                default:
                    NotifyIcon.ContextMenuStrip = GenerateContextMenuStrip(ContextMenu);
                    return;
            }  
        }

        #region Context Menu

#if !NETCOREAPP3_1_OR_GREATER

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

            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.VisibilityProperty,
                typeof(WPFMenuItem)).AddValueChanged(item, new EventHandler((sender, e) => result.Visible = item.Visibility == Visibility.Visible));

            result.Visible = item.Visibility == Visibility.Visible;
            result.Enabled = item.IsEnabled;
            item.IsEnabledChanged += (sender, e) => result.Enabled = (bool)e.NewValue;

            result.DefaultItem = GetIsDefault(item);

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

#endif

        #endregion

        #region Context Menu Strip

        private ContextMenuStrip GenerateContextMenuStrip(WPFContextMenu original)
        {
            if (original == null || original.Items.Count == 0)
                return null;

            original.SetBinding(
                FrameworkElement.DataContextProperty,
                new WPFBinding(nameof(DataContext)) { Source = this });

            var result = new ContextMenuStrip();

            result.Items.AddRange(GenerateStripMenuItems(original.Items));
            //result.Renderer = new CustomRender.ModernRenderer();

            return result;
        }

        private ToolStripItem[] GenerateStripMenuItems(ItemCollection original)
        {
            List<ToolStripItem> result = new List<ToolStripItem>();

            foreach (FrameworkElement item in original)
            {
                switch (item)
                {
                    case WPFMenuItem menuItem:
                        result.Add(LinkStripMenuItem(menuItem));
                        break;
                    case WPFSeparator separator:
                        result.Add(new Controls.UToolStripSeparator());
                        break;
                    default:
                        throw new NotSupportedException($"Type '{item.GetType()}' not supported.");
                }
            }

            return result.ToArray();
        }

        private ToolStripMenuItem LinkStripMenuItem(WPFMenuItem item)
        {
            ToolStripMenuItem result = new ToolStripMenuItem(GetHeader(item));

            // needed to change menu item header dynamically
            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.HeaderProperty,
                typeof(WPFMenuItem)).AddValueChanged(item, new EventHandler((sender, e) => result.Text = GetHeader(item)));

            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.VisibilityProperty,
                typeof(WPFMenuItem)).AddValueChanged(item, new EventHandler((sender, e) => result.Visible = item.Visibility == Visibility.Visible));

            result.Visible = item.Visibility == Visibility.Visible;
            result.Enabled = item.IsEnabled;
            item.IsEnabledChanged += (sender, e) => result.Enabled = (bool)e.NewValue;

            if (item.Items.Count != 0)
            {
                result.DropDownItems.AddRange(GenerateStripMenuItems(item.Items));

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

        #endregion


        private void OnNotifyIconBalloonTipClicked(object sender, EventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(BalloonTipClickEvent));

            BalloonTipClickCommand?.Execute(BalloonTipClickCommandParameter);
        }

        private void OnNotifyIconBalloonTipShown(object sender, EventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(BalloonTipShownEvent));
        }

        private void OnNotifyIconBalloonTipClosed(object sender, EventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(BalloonTipClosedEvent));
        }

        private void OnNotifyIconMouseClick(object sender, WinFormsMouseEventArgs e)
        {
            RaiseEvent(ToMouseButtonEventArgs(e, ClickEvent));

            if (e.Button == MouseButtons.Left)
            {
                ClickCommand?.Execute(ClickCommandParameter);
            }
        }

        private void OnNotifyIconMouseDoubleClick(object sender, WinFormsMouseEventArgs e)
        {
            RaiseEvent(ToMouseButtonEventArgs(e, MouseDoubleClickEvent));

            if (e.Button == MouseButtons.Left)
            {
                DoubleClickCommand?.Execute(DoubleClickCommandParameter);
            }
        }

        private void OnNotifyIconMouseDown(object sender, WinFormsMouseEventArgs e)
        {
            RaiseEvent(ToMouseButtonEventArgs(e, MouseDownEvent));
        }

        private void OnNotifyIconMouseUp(object sender, WinFormsMouseEventArgs e)
        {
            RaiseEvent(ToMouseButtonEventArgs(e, MouseUpEvent));
        }

        private void OnNotifyIconMouseMove(object sender, WinFormsMouseEventArgs e)
        {
            var routedEvent = new WPFMouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
            {
                RoutedEvent = MouseMoveEvent
            };

            RaiseEvent(routedEvent);
        }

#endregion
    }
    
#pragma warning restore CS0612 // Type or member is obsolete

}
