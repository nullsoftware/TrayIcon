using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using NullSoftware.ToolKit;
using PropertyChanged;

namespace Example.ViewModels
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

        [DoNotNotify]
        public IRefreshableCommand AddRecentFileCommand { get; }

        [DoNotNotify]
        public IRefreshableCommand ClearRecentFilesCommand { get; }

        [DoNotNotify]
        public ObservableCollection<MenuItem> RecentFiles { get; } = new ObservableCollection<MenuItem>();

        public MainViewModel()
        {
            MinimazeCommand = new RelayCommand(() => App.Current.MainWindow.WindowState = System.Windows.WindowState.Minimized);
            SayHelloCommand = new RelayCommand(() => NotificationService.Notify("Greetings", "Hello World!"));
            CloseCommand = new RelayCommand(App.Current.MainWindow.Close);
            AddRecentFileCommand = new RelayCommand(AddRecentFile);
            ClearRecentFilesCommand = new RelayCommand(() => RecentFiles.Clear());
        }

        private void AddRecentFile()
        {
            string path = $"C:\\Files\\file_{DateTime.Now:HH-mm-ss-fff}.txt";

            ICommand openCommand = new RelayCommand(
                () => NotificationService.Notify("Open file", path));

            RecentFiles.Add(new MenuItem
            {
                Header = path,
                Command = openCommand,
            });
        }
    }
}
