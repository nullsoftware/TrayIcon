[![](https://img.shields.io/nuget/vpre/TrayIcon)](https://www.nuget.org/packages/TrayIcon/)
[![](https://img.shields.io/nuget/dt/TrayIcon)](https://www.nuget.org/packages/TrayIcon/)

# Tray Icon
Library that allows use Tray Icon in WPF Application. 
Ported from Windows Forms. Has wrapper for WPF ContexMenu (which converts it to Windows Forms ContextMenu). This is needed for good performance, and compatibility.
This library targets all MVVM requirements:
- it has bindable properties
- it has interface with notify methods

## Getting started.
Use one of the follwing methods to install and use this library:

- **Package Manager:**

    ```batch
    PM> Install-Package TrayIcon
    ```

- **.NET CLI:**

    ```batch
    > dotnet add package TrayIcon
    ```
----
First you need to include namespace to your code or markup.

For XAML it can look like:
```XAML
<Window xmlns:icon="https://github.com/nullsoftware/TrayIcon" />
```

And for C#:
```C#
using NullSoftware.ToolKit;
```
----
Then you can place tray icon inside your window, or keep it in variable/property.
For XAML:
```XAML
<icon:TrayIconHandlers.TrayIcons>
    <icon:TrayIcon Title="My Application"
                   IconSource="MainIcon.ico"
                   ClickCommand="{Binding ExampleCommand}"
                   NotificationServiceMemberPath="NotificationService"/>
</icon:TrayIconHandlers.TrayIcons>
```

For C#:
```C#
TrayIcon myTrayIcon = new TrayIcon() 
{ 
    Title = "My Application",
    IconSource = new BitmapImage(new Uri("pack://application:,,,/MainIcon.ico")),
    ClickCommand = new RelayCommand(ExampleAction)
};
```
----
To show balloon you need to call `Notify` method:
```C#
INotificationService notifyService = myTrayIcon;
notifyService.Notify("Greetings", "Hello World!", NotificationType.Information);
```
**Note:** `INotificationService` can be obtained from XAML by using `NotificationServiceMemberPath`.
It injects `INotificationService` to specified DataContext property.

----
Full Example:
```XAML
<Window x:Class="TrayIcon.Example.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:TrayIcon.Example"
        xmlns:vm="clr-namespace:TrayIcon.Example.ViewModels"
        xmlns:icon="https://github.com/nullsoftware/TrayIcon"
        mc:Ignorable="d"
        Title="MainWindow" 
        Height="450" Width="800"
        Icon="MainIcon.ico">
    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>

    <!--Here you can place your icons-->
    <icon:TrayIconHandlers.TrayIcons>
        <icon:TrayIcon Title="My Application"
                       IconSource="MainIcon.ico"
                       DoubleClickCommand="{Binding MinimazeCommand}"
                       NotificationServiceMemberPath="NotificationService">
            <icon:TrayIcon.ContextMenu>
                <!--This context menu will be converted to System.Windows.Forms.ContextMenu-->
                <ContextMenu>
                    <MenuItem Header="Notify" Command="{Binding SayHelloCommand}" icon:TrayIcon.IsDefault="True"/>
                    <Separator/>
                    <MenuItem Header="_Silent Mode" IsCheckable="True" IsChecked="{Binding IsSilentModeEnabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
                    <Separator/>
                    <MenuItem Header="E_xit" Command="{Binding CloseCommand}"/>
                </ContextMenu>
            </icon:TrayIcon.ContextMenu>
        </icon:TrayIcon>
    </icon:TrayIconHandlers.TrayIcons>

    <Grid>
        <StackPanel VerticalAlignment="Top"
                    HorizontalAlignment="Left"
                    Margin="10, 20"
                    Orientation="Horizontal">
            <CheckBox VerticalAlignment="Center"
                      IsChecked="{Binding IsSilentModeEnabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                      Content="Silent Mode"/>
            <Button Margin="20, 0, 0, 0"
                    Padding="10, 3"
                    MinWidth="86"
                    Command="{Binding SayHelloCommand}"
                    Content="Notify"/>
        </StackPanel>
    </Grid>
</Window>
```
ViewModel:
```C#
using System;
using System.Windows.Input;
using NullSoftware.ToolKit;
using PropertyChanged;

public class MainViewModel : ObservableObject
{
    [DoNotNotify]
    private INotificationService NotificationService { get; set; }

    public bool IsSilentModeEnabled { get; set; }

    [DoNotNotify]
    public ICommand MinimazeCommand { get; }

    [DoNotNotify]
    public ICommand SayHelloCommand { get; }

    [DoNotNotify]
    public ICommand CloseCommand { get; }

    public MainViewModel()
    {
        MinimazeCommand = new RelayCommand(() => App.Current.MainWindow.WindowState = System.Windows.WindowState.Minimized);
        SayHelloCommand = new RelayCommand(() => NotificationService.Notify("Greetings", "Hello World!"));
        CloseCommand = new RelayCommand(App.Current.MainWindow.Close);
    }
}
```
**Note:** in ViewModel was used [PropertyChanged.Fody](https://github.com/Fody/PropertyChanged) plugin, to simplify usage of `INotifyPropertyChanged`.
