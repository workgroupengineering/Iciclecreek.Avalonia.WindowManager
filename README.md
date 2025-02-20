# WindowManager for Avalonia
This library implements a window manager for Avalonia with 100% Avalonia defined windows allowing you to create MDI style user interfaces in Avalonia.

![windows](https://github.com/user-attachments/assets/3d38f431-5451-4d34-b88d-9656ba5ab5ab)

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
* **WindowManagerPanel** - panel which hosts any content you want and also manages overlapping ManagedWindow instances 
* **ManagedWindow** - a Window implementation which isn't native but instead 100% avalonia 
 
## WindowManagerPanel control
The **WindowManagerPanel** is a **Canvas** control which manages overlapping windows.

It has the following properties:
* **Children** - which is any children you want to have in the background of the window.
* **Windows** - which is enumeration of all of the windows the WindowManagerPanel owns.

And the following window oriented methods:
* **ShowWindow(window)** - Adds a window to the panel
* **ShowAllWindows()** - restores all windows
* **MinimizeAllWindows()** - Minimizes all windows.

## ManagedWindow control
The **ManagedWindow** control is a clone of the **Window** control. It has standard Window properties like **Title**, **WindowState**, **WindowStartupLocation**, **Position**, etc.
Instead of being hosted using Native windows, a ManagedWindow control is hosted by the **WindowManagerPanel**

### Showing a window
To show a window you need to get an instance of the WindowManagerPanel and call **ShowWindow()**.

For example:
```xaml
  <wm:WindowMangerPanel Name="WindowManager"/>
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
To show a dialog is exactly the same as Avalonia, you instantiate a ManagedWindow and call ShowDialog on it.
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


