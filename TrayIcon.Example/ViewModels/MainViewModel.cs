using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NullSoftware.ToolKit;
using PropertyChanged;

namespace TrayIcon.Example.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        [DoNotNotify]
        private INotificationService NotificationService { get; set; }

        public bool IsSilentModeEnabled { get; set; }

        [DoNotNotify]
        public IRefreshableCommand MinimazeCommand { get; }

        [DoNotNotify]
        public IRefreshableCommand SayHelloCommand { get; }

        [DoNotNotify]
        public IRefreshableCommand CloseCommand { get; }

        public MainViewModel()
        {
            MinimazeCommand = new RelayCommand(() => App.Current.MainWindow.WindowState = System.Windows.WindowState.Minimized);
            SayHelloCommand = new RelayCommand(() => NotificationService.Notify("Greetings", "Hello World!"));
            CloseCommand = new RelayCommand(App.Current.MainWindow.Close);
        }
    }
}
