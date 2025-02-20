# WindowManager for Avalonia
This library implements a window manager for Avalonia with 100% Avalonia defined windows allowing you to create MDI style user interfaces in Avalonia.

![windows](https://github.com/user-attachments/assets/3d38f431-5451-4d34-b88d-9656ba5ab5ab)

# Installation
To install you need to add a reference to the nuget package **Iciclecreek.Avalonia.WindowManager**

```dotnet add package Iciclecreek.Avalonia.WindowManager```

The window manager comes with a Light and Dark theme which you need to install into your app.xaml.
To do that you need to:
* **Add namespace** ```xmlns:wm="using:Iciclecreek.Avalonia.WindowManager"```
* **Add WindowManagerTheme** - 

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
