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
```XAML:TrayIcon/TrayIcon.Test/MainWindow.xaml

```
-----------------------------

***In Develop***
