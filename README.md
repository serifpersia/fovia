# fovia
**fovia** is a fluid magnification tool for Windows, designed to provide smooth, fluid zooming and panning across your entire desktop. Unlike the default Windows Magnifier, fovia offers customizable tracking modes, independent zoom hotkey bindings, and a modern interface.

## Features
- **Fluid Zooming:** Hardware-accelerated magnification.
- **Dynamic Tracking Modes:** 
  - *Push (Edge Pan):* Pushes the screen only when the cursor reaches the edges.
  - *Centered:* Keeps the cursor strictly in the center of the zoom area.
  - *Proportional:* Maps your cursor's screen position to the magnified view.
- **Customizable Hotkeys:** Independently bind your "Zoom In" and "Zoom Out" actions to any key combination or mouse scroll direction.
- **Admin-Ready:** Easily zoom in on elevated applications (like Task Manager) by running the app as Administrator.

## Getting Started
### Installation
1. Download it here [Releases Page](https://github.com/serifpersia/fovia/releases).
3. Extract the contents of the zip and run `setup.exe` to install fovia on your system.

### Usage
- Click the **Hotkey** fields in the settings menu to rebind your zoom controls to your preference.
- Use the **Tracking Mode** dropdown to select how you want the screen to follow your mouse.

## Building from Source
To build fovia yourself, ensure you have the following installed:
- **Visual Studio 2022** (or newer).
- **.NET 9.0 SDK** (or newer).
- **Microsoft Visual Studio Installer Projects** extension installed in Visual Studio.

### Steps:
1. Clone the repository.
2. Open `fovia.sln` in Visual Studio.
5. Build the `fovia` project, then build the `fovia_setup` project to generate the installer.

## Requirements
- Windows 10 or Windows 11 (x64 architecture).
- .NET 9.0 Runtime (included automatically if using the installer).

## License
This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.
