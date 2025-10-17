# Full Crisis 3

A cross-platform 2D videogame built with .NET 9 and MonoGame, supporting Linux x86_64 and Windows x86_64 platforms.

## Features

- **Cross-Platform Desktop**: Runs natively on Linux and Windows
- **Self-Contained**: Desktop executables require no external files or installation
- **Multi-Input Support**: Mouse, keyboard (Arrow keys/WASD), and game controller navigation
- **Main Menu**: New Game, Load Game, Settings, and Quit options
- **Responsive UI**: Scales and adapts to different screen sizes
- **Built-in Font**: Custom bitmap font system eliminates external dependencies

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

- **FullCrisisGame**: Main game class with MonoGame lifecycle
- **SceneManager**: Manages different game states (menu, gameplay, etc.)
- **InputManager**: Unified input handling for keyboard, mouse, and gamepad
- **AssetManager**: Asset loading and management
- **UI System**: Accessible menu system with visual feedback

## Controls

- **Navigation**: Arrow Keys, WASD, Mouse, or Gamepad D-Pad/Left Stick
- **Select**: Enter, Space, Mouse Click, or Gamepad A button  
- **Back/Cancel**: Escape or Gamepad B button
- **Quit**: Escape (from main menu)

## Development

The game uses a modular architecture that separates:
- Core game logic (shared across platforms)
- Platform-specific implementations (desktop only)
- Asset management optimized for desktop performance
- Scene-based state management for easy extension

Ready for adding gameplay features, animations, and additional game assets!