# Iciclecreek.Avalonia.WindowManager
This library implements a window manager for Avalonia with windows defined using Avalonia instead of native windows.
This gives you the ability to create MDI style user interfaces in Avalonia, even in environments which don't support windowing like Android and iOS.

![windows](https://raw.githubusercontent.com/tomlm/Iciclecreek.Avalonia.WindowManager/refs/heads/main/windows.gif)

# Installation
To install you need to add a reference to the nuget package **Iciclecreek.Avalonia.WindowManager**

```dotnet add package Iciclecreek.Avalonia.WindowManager```

The window manager comes with a Light and Dark theme which you need to install into your app.xaml.
To do that you need to:
* Add **Iciclecreek.Avalonia.WindowManager** namespace  ```xmlns:wm="using:Iciclecreek.Avalonia.WindowManager"```
* Add **WindowManagerTheme** 

App.axaml
```
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:wm="using:Iciclecreek.Avalonia.WindowManager"
             x:Class="Demo.App"
             RequestedThemeVariant="Default">
  <Application.Styles>
    <FluentTheme />
    <wm:WindowManagerTheme/>
  </Application.Styles>
</Application>
```

# Usage
This library defines 2 classes:
* **WindowsPanel** - panel which hosts any content you want and also manages overlapping ManagedWindow instances 
* **ManagedWindow** - a Window implementation which isn't native but instead 100% avalonia 
 
## WindowsPanel control
The **WindowsPanel** is a **Canvas** control which manages overlapping windows.

It has the following properties:
* **Content** - which the content of the background of the windows panel
* **Windows** - which is enumeration of all of the windows the WindowsPanel owns.

And the following window oriented methods:
* **AddWindow(window)** - Adds a window to the panel
* **ShowAllWindows()** - restores all windows
* **MinimizeAllWindows()** - Minimizes all windows.

## ManagedWindow control
The **ManagedWindow** control is a clone of the **Window** control. It has standard Window properties like **Title**, **WindowState**, **WindowStartupLocation**, **Position**, etc.
Instead of being hosted using Native windows, a ManagedWindow control is hosted by the **WindowsPanel**

### Showing a window
To show a window you need to get an instance of the WindowsPanel and call **ShowWindow()**.

For example:
```xaml
  <wm:WindowsPanel Name="WindowManager"/>
```
And code behind
```cs
   var window = new ManagedWindow()
   {
       Title = "My window",
       WindowStartupLocation=WindowStartupLocation.CenterScreen,
       Width=300, Height=300
   };
    WindowManager.ShowWindow(window);
```

To close a window you simple call **Close()**.

### Showing a Dialog
To show a dialog is exactly the same as Avalonia, you instantiate a ManagedWindow and call **.ShowDialog() **passing in the parent window.
```cs
   var dialogWindow = new ManagedWindow()
   {
       Title = "My window",
       WindowStartupLocation=WindowStartupLocation.CenterScreen,
       Width=300, Height=300
   };
    var result = await dialogWindow.ShowDialog<string>(this);
```

To close a dialog you call **Close(result)**;


