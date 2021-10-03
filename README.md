# TrayIcon
Library that allows use Tray Icon in WPF Application. 
Ported from Windows Forms. Has wrapper for WPF ContexMenu (which converts it to Windows Forms ContextMenu). This is needed for good performance, and compatibility.
This library targets all MVVM requirements:
- it has bindable properties
- it has interface with notify methods

## Getting started.

First you need to include namespace to your code or markup.

For XAML it can look like:
```XAML
<Window xmlns:icon="clr-namespace:NullSoftware.ToolKit;assembly=TrayIcon" />
```

And for C#:
```C#
using NullSoftware.ToolKit;
```

Then you can place tray icon inside your window, or keep it in variable/property.

Full Example:
```XAML
<Window x:Class="TrayIcon.Test.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:TrayIcon.Test"
        xmlns:vm="clr-namespace:TrayIcon.Test.ViewModels"
        xmlns:icon="clr-namespace:NullSoftware.ToolKit;assembly=TrayIcon"
        mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="800"
        TextOptions.TextFormattingMode="Display"
        SnapsToDevicePixels="True"
        UseLayoutRounding="True">
    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>
    <icon:TrayIconHandlers.TrayIcons>
        <icon:TrayIcon Title="My Application">
            <!--This context menu will be converted to System.Windows.Forms.ContextMenu-->
            <icon:TrayIcon.ContextMenu>
                <ContextMenu>
                    <MenuItem Header="_Silent Mode" IsCheckable="True" IsChecked="{Binding IsSilentModeEnabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
                    <Separator/>
                    <MenuItem Header="E_xit" Command="{Binding CloseCommand}"/>
                </ContextMenu>
            </icon:TrayIcon.ContextMenu>
        </icon:TrayIcon>
    </icon:TrayIconHandlers.TrayIcons>
    
    <Grid>
        <CheckBox VerticalAlignment="Top"
                  HorizontalAlignment="Left"
                  Margin="10, 20"
                  IsChecked="{Binding IsSilentModeEnabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                  Content="Silent Mode"/>
    </Grid>
</Window>
```
-----------------------------

***In Develop***
