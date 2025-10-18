# Tiling Window Manager Configuration

Full Crisis 3 is designed to work well with tiling window managers like i3, sway, and others. The application includes several hints to help window managers understand it should float rather than tile.

## Built-in Window Manager Hints

The application sets the following properties to encourage floating behavior:
- **Fixed size**: 1280x720 pixels, non-resizable
- **Center positioning**: Opens in the center of the screen
- **Standard decorations**: Keeps title bar for better WM integration

## Manual Configuration

If you prefer explicit control, add these rules to your window manager config:

### i3 Window Manager (~/.config/i3/config)
```
# Float Full Crisis 3 by window title
for_window [title="Full Crisis 3"] floating enable

# Alternative: Float by application class (once Avalonia WM_CLASS is fixed)
# for_window [class="FullCrisis3"] floating enable

# Optional: Center the window
for_window [title="Full Crisis 3"] move position center
```

### Sway Window Manager (~/.config/sway/config)
```
# Float Full Crisis 3 by window title
for_window [title="Full Crisis 3"] floating enable

# Alternative: Float by application class (once Avalonia WM_CLASS is fixed)
# for_window [app_id="FullCrisis3"] floating enable

# Optional: Center the window  
for_window [title="Full Crisis 3"] move position center
```

### Hyprland (~/.config/hypr/hyprland.conf)
```
# Float Full Crisis 3
windowrule = float, ^(Full Crisis 3)$
windowrule = center, ^(Full Crisis 3)$

# Alternative using windowrulev2
windowrulev2 = float, title:^(Full Crisis 3)$
windowrulev2 = center, title:^(Full Crisis 3)$
```

### dwm
For dwm users, you can modify your config.h rules array:
```c
static const Rule rules[] = {
    /* class      instance    title           tags mask     isfloating   monitor */
    { NULL,       NULL,       "Full Crisis 3", 0,           1,           -1 },
};
```

## Testing Window Manager Integration

To test if the configuration is working:
1. Launch Full Crisis 3
2. The window should appear floating and centered
3. Try resizing - it should maintain fixed dimensions
4. The title bar should show "Full Crisis 3"

## Cross-Platform Compatibility

These window manager hints are designed to:
- Work correctly on Windows and macOS (standard windowing behavior)
- Provide helpful hints to Linux tiling window managers
- Not interfere with other desktop environments like GNOME or KDE

## Notes

- The application uses Avalonia UI which runs on X11/XWayland
- Native Wayland support may change these behaviors in future Avalonia versions
- WM_CLASS setting in Avalonia is still being improved - manual rules using window title are currently most reliable