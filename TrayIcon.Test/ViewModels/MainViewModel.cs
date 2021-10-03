﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NullSoftware.ToolKit;
using PropertyChanged;

namespace TrayIcon.Test.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public INotificationService NotificationService { get; private set; }

        public bool IsSilentModeEnabled { get; set; }

        [DoNotNotify]
        public IRefreshableCommand SayHelloCommand { get; }

        [DoNotNotify]
        public IRefreshableCommand CloseCommand { get; }

        public MainViewModel()
        {
            SayHelloCommand = new RelayCommand(() => NotificationService.Notify("Simple Title", "Simple Text..."));
            CloseCommand = new RelayCommand(App.Current.MainWindow.Close);
        }
    }
}
