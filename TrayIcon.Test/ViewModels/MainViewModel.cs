using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace TrayIcon.Test.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public bool IsSilentModeEnabled { get; set; }

        [DoNotNotify]
        public IRefreshableCommand CloseCommand { get; }

        public MainViewModel()
        {
            CloseCommand = new RelayCommand(App.Current.MainWindow.Close);
        }
    }
}
