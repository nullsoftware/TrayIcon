using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ItemCollection = System.Windows.Controls.ItemCollection;
using WinFormsMouseEventArgs = System.Windows.Forms.MouseEventArgs;
using WPFApplication = System.Windows.Application;
using WPFBinding = System.Windows.Data.Binding;
using WPFContextMenu = System.Windows.Controls.ContextMenu;
using WPFMenuItem = System.Windows.Controls.MenuItem;
using WPFMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WPFSeparator = System.Windows.Controls.Separator;

#pragma warning disable CS0612 // Type or member is obsolete

namespace NullSoftware.ToolKit
{
    /// <summary>
    /// Specifies a component that creates an icon in the notification area.
    /// </summary>
    [DefaultEvent(nameof(Click))]
    public class TrayIcon : FrameworkElement, INotificationService, IDisposable
    {
        private const ushort DefaultShowTimeoutMs = 10000;

#if !NETCOREAPP3_1_OR_GREATER
        private readonly Dictionary<WPFMenuItem, MenuItem> _menuItemMapping = new Dictionary<WPFMenuItem, MenuItem>();
        private readonly Dictionary<ItemCollection, NotifyCollectionChangedEventHandler> _menuCollectionHandlers
            = new Dictionary<ItemCollection, NotifyCollectionChangedEventHandler>();
#endif
        private readonly Dictionary<WPFMenuItem, ToolStripMenuItem> _toolStripMenuItemMapping = new Dictionary<WPFMenuItem, ToolStripMenuItem>();
        private readonly Dictionary<ItemCollection, NotifyCollectionChangedEventHandler> _toolStripCollectionHandlers
            = new Dictionary<ItemCollection, NotifyCollectionChangedEventHandler>();

        private ExitEventHandler _applicationExitHandler;
        private Icon _currentIcon;
        private bool _disposed;


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
                new FrameworkPropertyMetadata(DefaultShowTimeoutMs));

        /// <summary>
        /// Gets or sets the time period, in milliseconds, the balloon tip should display.
        /// This parameter is deprecated as of Windows Vista.
        /// Notification display times are now based on system accessibility settings.
        /// </summary>
        [Obsolete("This parameter is deprecated as of Windows Vista. Notification display times are now based on system accessibility settings.")]
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
            var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            _currentIcon = Icon.ExtractAssociatedIcon(entryAssembly.Location);

            NotifyIcon = new NotifyIcon()
            {
                Icon = _currentIcon,
                Visible = Visibility == Visibility.Visible,
                Text = Title
            };

            // event subscription
            NotifyIcon.BalloonTipClicked += OnNotifyIconBalloonTipClicked;
            NotifyIcon.BalloonTipShown += OnNotifyIconBalloonTipShown;
            NotifyIcon.BalloonTipClosed += OnNotifyIconBalloonTipClosed;
            NotifyIcon.MouseClick += OnNotifyIconMouseClick;
            NotifyIcon.MouseDoubleClick += OnNotifyIconMouseDoubleClick;
            NotifyIcon.MouseDown += OnNotifyIconMouseDown;
            NotifyIcon.MouseUp += OnNotifyIconMouseUp;
            NotifyIcon.MouseMove += OnNotifyIconMouseMove;

            _applicationExitHandler = (sender, e) => Dispose();
            WPFApplication.Current.Exit += _applicationExitHandler;
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="TrayIcon"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Unsubscribe from application exit event
                if (WPFApplication.Current != null && _applicationExitHandler != null)
                {
                    WPFApplication.Current.Exit -= _applicationExitHandler;
                }

                // Dispose context menus
#if !NETCOREAPP3_1_OR_GREATER
                DisposeContextMenu();
#endif
                DisposeContextMenuStrip();

                // Dispose NotifyIcon
                if (NotifyIcon != null)
                {
                    NotifyIcon.BalloonTipClicked -= OnNotifyIconBalloonTipClicked;
                    NotifyIcon.BalloonTipShown -= OnNotifyIconBalloonTipShown;
                    NotifyIcon.BalloonTipClosed -= OnNotifyIconBalloonTipClosed;
                    NotifyIcon.MouseClick -= OnNotifyIconMouseClick;
                    NotifyIcon.MouseDoubleClick -= OnNotifyIconMouseDoubleClick;
                    NotifyIcon.MouseDown -= OnNotifyIconMouseDown;
                    NotifyIcon.MouseUp -= OnNotifyIconMouseUp;
                    NotifyIcon.MouseMove -= OnNotifyIconMouseMove;
                    NotifyIcon.Dispose();
                }

                // Dispose current icon
                _currentIcon?.Dispose();
            }

            _disposed = true;
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

            if (trayIcon.IsInitialized)
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
                Icon newIcon = null;

                switch (trayIcon.IconSource)
                {
                    case BitmapFrame frame:
                        newIcon = new Icon(WPFApplication.GetResourceStream(new Uri(frame.Decoder.ToString())).Stream, 16, 16);
                        break;
                    case BitmapImage bitmapImg:
                        newIcon = new Icon(bitmapImg.StreamSource, 16, 16);
                        break;
                    default:
                        throw new NotSupportedException("Icon Source supports only BitmapFrame or BitmapImage.");
                }

                // Dispose old icon before replacing
                var oldIcon = trayIcon._currentIcon;
                trayIcon._currentIcon = newIcon;
                trayIcon.NotifyIcon.Icon = newIcon;
                oldIcon?.Dispose();
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
        /// Converts <see cref="WPFContextMenu"/> to Windows Forms variation.
        /// </summary>
        protected virtual void GenerateContextMenu()
        {
            switch (ContextMenuVariation)
            {
                case ContextMenuVariation.ContextMenu:
#if !NETCOREAPP3_1_OR_GREATER
                    DisposeContextMenuStrip();
                    NotifyIcon.ContextMenu = GenerateContextMenu(ContextMenu);
                    return;
#else
                    throw new NotSupportedException("'System.Windows.Forms.ContextMenu' is not supported in current .NET version. Use 'ContextMenuVariation.ContextMenuStrip' instead.");
#endif
                default:
#if !NETCOREAPP3_1_OR_GREATER
                    DisposeContextMenu();
#endif
                    NotifyIcon.ContextMenuStrip = GenerateContextMenuStrip(ContextMenu);
                    return;
            }  
        }

        #region Context Menu

#if !NETCOREAPP3_1_OR_GREATER

        private void DisposeContextMenu()
        {
            if (NotifyIcon.ContextMenu == null)
                return;

            var contextMenu = NotifyIcon.ContextMenu;
            NotifyIcon.ContextMenu = null;

            foreach (var pair in _menuCollectionHandlers.ToList())
            {
                ((INotifyCollectionChanged)pair.Key).CollectionChanged -= pair.Value;
            }
            _menuCollectionHandlers.Clear();

            var keys = _menuItemMapping.Keys.ToList();
            foreach (var item in keys)
            {
                UnlinkMenuItem(item);
            }

            contextMenu.Dispose();
        }

        private ContextMenu GenerateContextMenu(WPFContextMenu original)
        {
            if (original == null)
                return null;

            original.SetBinding(
                FrameworkElement.DataContextProperty,
                new WPFBinding(nameof(DataContext)) { Source = this });

            var result = new ContextMenu(GenerateMenuItems(original.Items));
            SubscribeMenuItemsChanges(original.Items, result.MenuItems);

            return result;
        }

        private MenuItem[] GenerateMenuItems(ItemCollection original)
        {
            List<MenuItem> result = new List<MenuItem>();

            foreach (FrameworkElement item in original)
            {
                result.Add(CreateMenuItem(item));
            }

            return result.ToArray();
        }

        private MenuItem CreateMenuItem(FrameworkElement item)
        {
            switch (item)
            {
                case WPFMenuItem menuItem:
                    return LinkMenuItem(menuItem);
                case WPFSeparator separator:
                    return new MenuItem("-");
                default:
                    throw new NotSupportedException(
                        $"Type '{item.GetType()}' is not supported as a context menu item. " +
                        $"Use '{typeof(WPFMenuItem)}' or '{typeof(WPFSeparator)}'. " +
                        "When binding an ObservableCollection via ItemsSource, the items must already be MenuItem instances.");
            }
        }

        private MenuItem LinkMenuItem(WPFMenuItem item)
        {
            // if exists return item from cache
            if (_menuItemMapping.TryGetValue(item, out var existing))
                return existing;


            MenuItem result = new MenuItem(GetHeader(item));

            // needed to change menu item header dynamically
            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.HeaderProperty,
                typeof(WPFMenuItem)).AddValueChanged(item, OnWPFMenuItemHeaderChanged);

            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.VisibilityProperty,
                typeof(WPFMenuItem)).AddValueChanged(item, OnWPFMenuItemVisibilityChanged);

            result.Visible = item.Visibility == Visibility.Visible;
            result.Enabled = item.IsEnabled;
            item.IsEnabledChanged += OnWPFMenuItemIsEnabledChanged;

            result.DefaultItem = GetIsDefault(item);

            // Cache the mapping eagerly so dynamic-children handlers can resolve this menu item
            _menuItemMapping.Add(item, result);

            if (item.Items.Count != 0)
            {
                result.MenuItems.AddRange(GenerateMenuItems(item.Items));
            }
            else
            {
                if (item.IsCheckable)
                {
                    item.Checked += OnWPFMenuItemChecked;
                    item.Unchecked += OnWPFMenuItemUnchecked;

                    result.Checked = item.IsChecked;
                }

                MenuItemAutomationPeer peer = new MenuItemAutomationPeer(item);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;

                result.Tag = invokeProv;
                result.Click += OnMenuItemClick;
            }

            // Subscribe to allow dynamic items (e.g. ObservableCollection bound via ItemsSource)
            SubscribeMenuItemsChanges(item.Items, result.MenuItems);

            return result;
        }

        private void UnlinkMenuItem(WPFMenuItem item)
        {
            if (!_menuItemMapping.TryGetValue(item, out var cachedItem))
                return;

            // Recursively unlink any descendants we have linked
            foreach (var child in item.Items.OfType<WPFMenuItem>().ToList())
            {
                UnlinkMenuItem(child);
            }

            UnsubscribeMenuItemsChanges(item.Items);

            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.HeaderProperty,
                typeof(WPFMenuItem)).RemoveValueChanged(item, OnWPFMenuItemHeaderChanged);
            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.VisibilityProperty,
                typeof(WPFMenuItem)).RemoveValueChanged(item, OnWPFMenuItemVisibilityChanged);

            item.IsEnabledChanged -= OnWPFMenuItemIsEnabledChanged;

            // Click + checkable handlers were only set up for items that initially had no children
            if (cachedItem.Tag is IInvokeProvider)
            {
                if (item.IsCheckable)
                {
                    item.Checked -= OnWPFMenuItemChecked;
                    item.Unchecked -= OnWPFMenuItemUnchecked;
                }

                cachedItem.Click -= OnMenuItemClick;
            }

            cachedItem.Tag = null;
            cachedItem.Dispose();

            _menuItemMapping.Remove(item);
        }

        private void SubscribeMenuItemsChanges(ItemCollection wpfItems, Menu.MenuItemCollection menuItems)
        {
            if (wpfItems == null || _menuCollectionHandlers.ContainsKey(wpfItems))
                return;

            NotifyCollectionChangedEventHandler handler = (sender, e) =>
                OnWpfMenuItemsChanged(wpfItems, menuItems, e);

            ((INotifyCollectionChanged)wpfItems).CollectionChanged += handler;
            _menuCollectionHandlers[wpfItems] = handler;
        }

        private void UnsubscribeMenuItemsChanges(ItemCollection wpfItems)
        {
            if (wpfItems == null) return;

            if (!_menuCollectionHandlers.TryGetValue(wpfItems, out var handler))
                return;

            ((INotifyCollectionChanged)wpfItems).CollectionChanged -= handler;
            _menuCollectionHandlers.Remove(wpfItems);
        }

        private void OnWpfMenuItemsChanged(
            ItemCollection wpfItems,
            Menu.MenuItemCollection menuItems,
            NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertMenuItems(menuItems, e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveMenuItems(menuItems, e.OldStartingIndex, e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveMenuItems(menuItems, e.OldStartingIndex, e.OldItems);
                    InsertMenuItems(menuItems, e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveMenuItems(menuItems, e.OldStartingIndex, e.OldItems);
                    InsertMenuItems(menuItems, e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ResetMenuItems(wpfItems, menuItems);
                    break;
            }
        }

        private void InsertMenuItems(Menu.MenuItemCollection menuItems, int startIndex, IList newItems)
        {
            if (newItems == null) return;

            if (startIndex < 0)
                startIndex = menuItems.Count;

            for (int i = 0; i < newItems.Count; i++)
            {
                MenuItem mi = CreateMenuItem((FrameworkElement)newItems[i]);
                menuItems.Add(startIndex + i, mi);
            }
        }

        private void RemoveMenuItems(Menu.MenuItemCollection menuItems, int startIndex, IList oldItems)
        {
            if (oldItems == null) return;

            for (int i = oldItems.Count - 1; i >= 0; i--)
            {
                int index = startIndex + i;
                if (index < 0 || index >= menuItems.Count)
                    continue;

                MenuItem mi = menuItems[index];
                menuItems.RemoveAt(index);

                if (oldItems[i] is WPFMenuItem oldMenuItem)
                {
                    UnlinkMenuItem(oldMenuItem);
                }
                else
                {
                    mi.Dispose();
                }
            }
        }

        private void ResetMenuItems(ItemCollection wpfItems, Menu.MenuItemCollection menuItems)
        {
            var snapshot = new List<MenuItem>(menuItems.Count);
            foreach (MenuItem mi in menuItems)
                snapshot.Add(mi);

            menuItems.Clear();

            foreach (var mi in snapshot)
            {
                WPFMenuItem matched = null;
                foreach (var pair in _menuItemMapping)
                {
                    if (pair.Value == mi)
                    {
                        matched = pair.Key;
                        break;
                    }
                }

                if (matched != null)
                {
                    UnlinkMenuItem(matched);
                    continue;
                }

                mi.Dispose();
            }

            foreach (FrameworkElement wpfItem in wpfItems)
            {
                menuItems.Add(CreateMenuItem(wpfItem));
            }
        }

        private void OnWPFMenuItemHeaderChanged(object sender, EventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _menuItemMapping[item].Text = GetHeader(item);
        }

        private void OnWPFMenuItemVisibilityChanged(object sender, EventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _menuItemMapping[item].Visible = item.Visibility == Visibility.Visible;
        }

        private void OnWPFMenuItemIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _menuItemMapping[item].Enabled = (bool)e.NewValue;
        }

        private void OnWPFMenuItemChecked(object sender, RoutedEventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _menuItemMapping[item].Checked = true;
        }

        private void OnWPFMenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _menuItemMapping[item].Checked = false;
        }

        private void OnMenuItemClick(object sender, EventArgs e)
        {
            var item = (MenuItem)sender;
            IInvokeProvider invokeProv = (IInvokeProvider)item.Tag;
            invokeProv.Invoke();
        }

#endif

        #endregion

        #region Context Menu Strip

        private void DisposeContextMenuStrip()
        {
            if (NotifyIcon.ContextMenuStrip == null)
                return;

            var contextMenuStrip = NotifyIcon.ContextMenuStrip;
            NotifyIcon.ContextMenuStrip = null;

            foreach (var pair in _toolStripCollectionHandlers.ToList())
            {
                ((INotifyCollectionChanged)pair.Key).CollectionChanged -= pair.Value;
            }
            _toolStripCollectionHandlers.Clear();

            var keys = _toolStripMenuItemMapping.Keys.ToList();
            foreach (var item in keys)
            {
                UnlinkStripMenuItem(item);
            }

            contextMenuStrip.Dispose();
        }

        private ContextMenuStrip GenerateContextMenuStrip(WPFContextMenu original)
        {
            if (original == null)
                return null;

            original.SetBinding(
                FrameworkElement.DataContextProperty,
                new WPFBinding(nameof(DataContext)) { Source = this });

            var result = new ContextMenuStrip();

            result.Items.AddRange(GenerateStripMenuItems(original.Items));
            SubscribeStripItemsChanges(original.Items, result.Items);
            //result.Renderer = new CustomRender.ModernRenderer();

            return result;
        }

        private ToolStripItem[] GenerateStripMenuItems(ItemCollection original)
        {
            List<ToolStripItem> result = new List<ToolStripItem>();

            foreach (FrameworkElement item in original)
            {
                result.Add(CreateStripItem(item));
            }

            return result.ToArray();
        }

        private ToolStripItem CreateStripItem(FrameworkElement item)
        {
            switch (item)
            {
                case WPFMenuItem menuItem:
                    return LinkStripMenuItem(menuItem);
                case WPFSeparator separator:
                    return new ToolStripSeparator();
                default:
                    throw new NotSupportedException(
                        $"Type '{item.GetType()}' is not supported as a context menu item. " +
                        $"Use '{typeof(WPFMenuItem)}' or '{typeof(WPFSeparator)}'. " +
                        "When binding an ObservableCollection via ItemsSource, the items must already be MenuItem instances.");
            }
        }

        private ToolStripMenuItem LinkStripMenuItem(WPFMenuItem item)
        {
            // if exists return item from cache
            if (_toolStripMenuItemMapping.TryGetValue(item, out var existing))
                return existing;


            ToolStripMenuItem result = new ToolStripMenuItem(GetHeader(item));

            // needed to change menu item header dynamically
            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.HeaderProperty,
                typeof(WPFMenuItem)).AddValueChanged(item, OnStripWPFMenuItemHeaderChanged);

            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.VisibilityProperty,
                typeof(WPFMenuItem)).AddValueChanged(item, OnStripWPFMenuItemVisibilityChanged);

            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.IconProperty,
                typeof(WPFMenuItem)).AddValueChanged(item, OnStripWPFMenuItemIconChanged);

            result.Visible = item.Visibility == Visibility.Visible;
            result.Enabled = item.IsEnabled;
            result.Image = ConvertWpfIconToImage(item.Icon);
            item.IsEnabledChanged += OnStripWPFMenuItemIsEnabledChanged;

            // Cache the mapping eagerly so dynamic-children handlers can resolve this strip item
            _toolStripMenuItemMapping.Add(item, result);

            if (item.Items.Count != 0)
            {
                result.DropDownItems.AddRange(GenerateStripMenuItems(item.Items));
            }
            else
            {
                if (item.IsCheckable)
                {
                    item.Checked += OnStripWPFMenuItemChecked;
                    item.Unchecked += OnStripWPFMenuItemUnchecked;

                    result.Checked = item.IsChecked;
                }

                MenuItemAutomationPeer peer = new MenuItemAutomationPeer(item);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;

                result.Tag = invokeProv;
                result.Click += OnToolStripMenuItemClick;
            }

            // Subscribe to allow dynamic items (e.g. ObservableCollection bound via ItemsSource)
            SubscribeStripItemsChanges(item.Items, result.DropDownItems);

            return result;
        }

        private void UnlinkStripMenuItem(WPFMenuItem item)
        {
            if (!_toolStripMenuItemMapping.TryGetValue(item, out var cachedItem))
                return;

            // Recursively unlink any descendants we have linked
            foreach (var child in item.Items.OfType<WPFMenuItem>().ToList())
            {
                UnlinkStripMenuItem(child);
            }

            UnsubscribeStripItemsChanges(item.Items);

            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.HeaderProperty,
                typeof(WPFMenuItem)).RemoveValueChanged(item, OnStripWPFMenuItemHeaderChanged);
            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.VisibilityProperty,
                typeof(WPFMenuItem)).RemoveValueChanged(item, OnStripWPFMenuItemVisibilityChanged);
            DependencyPropertyDescriptor.FromProperty(
                WPFMenuItem.IconProperty,
                typeof(WPFMenuItem)).RemoveValueChanged(item, OnStripWPFMenuItemIconChanged);

            item.IsEnabledChanged -= OnStripWPFMenuItemIsEnabledChanged;

            // Click + checkable handlers were only set up for items that initially had no children
            if (cachedItem.Tag is IInvokeProvider)
            {
                if (item.IsCheckable)
                {
                    item.Checked -= OnStripWPFMenuItemChecked;
                    item.Unchecked -= OnStripWPFMenuItemUnchecked;
                }

                cachedItem.Click -= OnToolStripMenuItemClick;
            }

            var existingImage = cachedItem.Image;
            cachedItem.Image = null;
            existingImage?.Dispose();

            cachedItem.Tag = null;
            cachedItem.Dispose();

            _toolStripMenuItemMapping.Remove(item);
        }

        private void SubscribeStripItemsChanges(ItemCollection wpfItems, ToolStripItemCollection stripItems)
        {
            if (wpfItems == null || _toolStripCollectionHandlers.ContainsKey(wpfItems))
                return;

            NotifyCollectionChangedEventHandler handler = (sender, e) =>
                OnStripWpfItemsChanged(wpfItems, stripItems, e);

            ((INotifyCollectionChanged)wpfItems).CollectionChanged += handler;
            _toolStripCollectionHandlers[wpfItems] = handler;
        }

        private void UnsubscribeStripItemsChanges(ItemCollection wpfItems)
        {
            if (wpfItems == null) return;

            if (!_toolStripCollectionHandlers.TryGetValue(wpfItems, out var handler))
                return;

            ((INotifyCollectionChanged)wpfItems).CollectionChanged -= handler;
            _toolStripCollectionHandlers.Remove(wpfItems);
        }

        private void OnStripWpfItemsChanged(
            ItemCollection wpfItems,
            ToolStripItemCollection stripItems,
            NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertStripItems(stripItems, e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveStripItems(stripItems, e.OldStartingIndex, e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveStripItems(stripItems, e.OldStartingIndex, e.OldItems);
                    InsertStripItems(stripItems, e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Move:
                    // ToolStripItemCollection has no Move; emulate as remove + insert
                    RemoveStripItems(stripItems, e.OldStartingIndex, e.OldItems);
                    InsertStripItems(stripItems, e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ResetStripItems(wpfItems, stripItems);
                    break;
            }
        }

        private void InsertStripItems(ToolStripItemCollection stripItems, int startIndex, IList newItems)
        {
            if (newItems == null) return;

            if (startIndex < 0)
                startIndex = stripItems.Count;

            for (int i = 0; i < newItems.Count; i++)
            {
                ToolStripItem stripItem = CreateStripItem((FrameworkElement)newItems[i]);
                stripItems.Insert(startIndex + i, stripItem);
            }
        }

        private void RemoveStripItems(ToolStripItemCollection stripItems, int startIndex, IList oldItems)
        {
            if (oldItems == null) return;

            for (int i = oldItems.Count - 1; i >= 0; i--)
            {
                int stripIndex = startIndex + i;
                if (stripIndex < 0 || stripIndex >= stripItems.Count)
                    continue;

                ToolStripItem stripItem = stripItems[stripIndex];
                stripItems.RemoveAt(stripIndex);

                if (oldItems[i] is WPFMenuItem oldMenuItem)
                {
                    UnlinkStripMenuItem(oldMenuItem);
                }
                else
                {
                    stripItem.Dispose();
                }
            }
        }

        private void ResetStripItems(ItemCollection wpfItems, ToolStripItemCollection stripItems)
        {
            var snapshot = new List<ToolStripItem>(stripItems.Count);
            foreach (ToolStripItem ti in stripItems)
                snapshot.Add(ti);

            stripItems.Clear();

            foreach (var ti in snapshot)
            {
                if (ti is ToolStripMenuItem tsmi)
                {
                    WPFMenuItem matched = null;
                    foreach (var pair in _toolStripMenuItemMapping)
                    {
                        if (pair.Value == tsmi)
                        {
                            matched = pair.Key;
                            break;
                        }
                    }

                    if (matched != null)
                    {
                        UnlinkStripMenuItem(matched);
                        continue;
                    }
                }

                ti.Dispose();
            }

            foreach (FrameworkElement wpfItem in wpfItems)
            {
                stripItems.Add(CreateStripItem(wpfItem));
            }
        }

        private void OnStripWPFMenuItemHeaderChanged(object sender, EventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _toolStripMenuItemMapping[item].Text = GetHeader(item);
        }

        private void OnStripWPFMenuItemVisibilityChanged(object sender, EventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _toolStripMenuItemMapping[item].Visible = item.Visibility == Visibility.Visible;
        }

        private void OnStripWPFMenuItemIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _toolStripMenuItemMapping[item].Enabled = (bool)e.NewValue;
        }

        private void OnStripWPFMenuItemIconChanged(object sender, EventArgs e)
        {
            var item = (WPFMenuItem)sender;
            var stripItem = _toolStripMenuItemMapping[item];
            var oldImage = stripItem.Image;
            stripItem.Image = ConvertWpfIconToImage(item.Icon);
            oldImage?.Dispose();
        }

        private void OnStripWPFMenuItemChecked(object sender, RoutedEventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _toolStripMenuItemMapping[item].Checked = true;
        }

        private void OnStripWPFMenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            var item = (WPFMenuItem)sender;
            _toolStripMenuItemMapping[item].Checked = false;
        }

        private void OnToolStripMenuItemClick(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            IInvokeProvider invokeProv = (IInvokeProvider)item.Tag;
            invokeProv.Invoke();
        }

        private static System.Drawing.Image ConvertWpfIconToImage(object icon)
        {
            if (icon == null)
                return null;

            BitmapSource source = null;

            switch (icon)
            {
                case BitmapSource bs:
                    source = bs;
                    break;
                case System.Windows.Controls.Image wpfImage when wpfImage.Source is BitmapSource imgSrc:
                    source = imgSrc;
                    break;
                case System.Windows.Controls.Image wpfImage when wpfImage.Source != null:
                    source = RenderImageSourceToBitmapSource(wpfImage.Source);
                    break;
                case ImageSource imgSource:
                    source = RenderImageSourceToBitmapSource(imgSource);
                    break;
            }

            if (source == null)
                return null;

            try
            {
                using (var ms = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(source));
                    encoder.Save(ms);
                    ms.Position = 0;

                    // Copy into an independent Bitmap so the MemoryStream can be released safely.
                    using (var loaded = new System.Drawing.Bitmap(ms))
                    {
                        return new System.Drawing.Bitmap(loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to convert WPF MenuItem.Icon to a WinForms image. Exception: {ex}");
                return null;
            }
        }

        private static BitmapSource RenderImageSourceToBitmapSource(ImageSource imgSource)
        {
            const int size = 16;

            var wrapper = new System.Windows.Controls.Image
            {
                Source = imgSource,
                Stretch = System.Windows.Media.Stretch.Uniform,
            };

            wrapper.Measure(new System.Windows.Size(size, size));
            wrapper.Arrange(new System.Windows.Rect(0, 0, size, size));

            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(wrapper);
            return rtb;
        }


        #endregion


        private void OnNotifyIconBalloonTipClicked(object sender, EventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(BalloonTipClickEvent));

            if (BalloonTipClickCommand?.CanExecute(BalloonTipClickCommandParameter) == true)
            {
                BalloonTipClickCommand.Execute(BalloonTipClickCommandParameter);
            }
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
                if (ClickCommand?.CanExecute(ClickCommandParameter) == true)
                {
                    ClickCommand.Execute(ClickCommandParameter);
                }
            }
        }

        private void OnNotifyIconMouseDoubleClick(object sender, WinFormsMouseEventArgs e)
        {
            RaiseEvent(ToMouseButtonEventArgs(e, MouseDoubleClickEvent));

            if (e.Button == MouseButtons.Left)
            {
                if (DoubleClickCommand?.CanExecute(DoubleClickCommandParameter) == true)
                {
                    DoubleClickCommand.Execute(DoubleClickCommandParameter);
                }
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
