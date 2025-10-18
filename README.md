# Full Crisis 3

A cross-platform desktop application built with .NET 9 and Avalonia UI, supporting Linux x86_64 and Windows x86_64 platforms.

## Features

- **Cross-Platform Desktop**: Runs natively on Linux and Windows using Avalonia UI
- **Self-Contained**: Desktop executables include all necessary dependencies
- **Modern UI**: Built with XAML and MVVM architecture using Avalonia
- **Main Menu**: New Game, Load Game, Settings, and Quit options with smooth navigation
- **Responsive UI**: Scales and adapts to different screen sizes and DPI settings
- **Dark Theme**: Modern dark theme with cross-platform fonts and styling
- **Quit Confirmation**: Modal dialog with escape key navigation support

## Building

### Prerequisites

- .NET 9 SDK
- Python 3 (for build script)

### Quick Build

Run the build script to create release packages for both platforms:

```bash
python3 build.py
```

This creates:
- `./release/FullCrisis3.linux.x64` - Self-contained Linux executable
- `./release/FullCrisis3.win.x64.exe` - Self-contained Windows executable

### Manual Build

**Desktop (Linux/Windows):**
```bash
cd FullCrisis3.Desktop
dotnet build
dotnet run
```

## Running

### Linux
```bash
cd release
./FullCrisis3.linux.x64
```

### Windows
```batch
cd release
FullCrisis3.win.x64.exe
```

## Architecture

```
FullCrisis3/
+-- FullCrisis3.Core/          # Shared game logic and systems
+-- FullCrisis3.Desktop/       # Desktop platform (Linux/Windows)
+-- build.py                   # Cross-platform build script
```

### Key Components

- **MainWindow**: Main application window built with Avalonia
- **MainWindowViewModel**: Root view model managing navigation and dialogs
- **MainMenuView**: XAML-based main menu with styled buttons
- **SubMenuView**: Reusable XAML template for sub-menus
- **MVVM Architecture**: Clean separation using ReactiveUI for data binding

## Controls

- **Navigation**: Mouse click, Tab/Shift+Tab for keyboard navigation
- **Select**: Mouse click, Enter, or Space
- **Back/Cancel**: Escape key
- **Quit**: Escape (from main menu shows confirmation dialog)

## Development

The application uses a modern MVVM architecture with:
- **Views**: XAML-based UI definitions with styling and layout
- **ViewModels**: Business logic and data binding using ReactiveUI
- **Models**: Data structures and core application logic
- **Platform-specific**: Desktop implementations for Linux and Windows
- **Cross-platform**: Avalonia provides consistent UI across platforms
- **Font Support**: Cross-platform font fallbacks (Times/Liberation Serif for titles, Courier/Liberation Mono for UI)

Ready for adding game features, animations, and additional UI screens!

## Font Configuration

The application uses cross-platform font stacks for maximum compatibility:
- **Title Font**: `Times New Roman,Liberation Serif,Times,serif`
- **UI/Button Font**: `Courier New,Liberation Mono,DejaVu Sans Mono,Consolas,monospace`

This ensures the application looks consistent across Windows, Linux, and macOS without requiring custom font embedding.