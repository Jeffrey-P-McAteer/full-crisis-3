using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FullCrisis3;

/// <summary>
/// Configuration for background animation themes
/// </summary>
public class BackgroundThemeConfig
{
    public string Name { get; set; } = "";
    public Color SkyColor { get; set; } = Colors.Black;
    public Color HillColor { get; set; } = Color.FromRgb(20, 20, 30);
    public Color BuildingColor { get; set; } = Color.FromRgb(10, 10, 15);
    public Color LightOnColor { get; set; } = Color.FromRgb(255, 255, 200);
    public Color LightOffColor { get; set; } = Color.FromRgb(40, 40, 50);
    public double StarDensity { get; set; } = 0.02; // Percentage of sky filled with stars
    public double LightBlinkRate { get; set; } = 2.0; // Seconds between light changes
    public double[] ParallaxSpeeds { get; set; } = { 1.0, 2.0, 4.0 }; // Pixels per second for each layer
}

/// <summary>
/// Represents an animated element in the background
/// </summary>
public abstract class BackgroundElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Speed { get; set; } // Pixels per second
    public abstract void Update(double deltaTime, double canvasWidth, double canvasHeight);
    public abstract void Render(DrawingContext context, BackgroundThemeConfig theme);
}

/// <summary>
/// A scrolling building silhouette
/// </summary>
public class BuildingElement : BackgroundElement
{
    public List<WindowLight> Windows { get; set; } = new();
    public double LastLightUpdate { get; set; }
    
    public class WindowLight
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 3;
        public double Height { get; set; } = 3;
        public bool IsOn { get; set; }
        public double NextBlinkTime { get; set; }
    }
    
    public override void Update(double deltaTime, double canvasWidth, double canvasHeight)
    {
        // Move building left
        X -= Speed * deltaTime;
        
        // Reset position when off-screen
        if (X + Width < 0)
        {
            X = canvasWidth + (Width * 0.5);
        }
        
        // Update window lights
        LastLightUpdate += deltaTime;
        foreach (var window in Windows)
        {
            if (LastLightUpdate >= window.NextBlinkTime)
            {
                window.IsOn = Random.Shared.NextDouble() > 0.7; // 30% chance to be on
                window.NextBlinkTime = LastLightUpdate + Random.Shared.NextDouble() * 3.0 + 1.0; // 1-4 seconds
            }
        }
    }
    
    public override void Render(DrawingContext context, BackgroundThemeConfig theme)
    {
        // Draw building silhouette
        var buildingBrush = new SolidColorBrush(theme.BuildingColor);
        var rect = new Rect(X, Y, Width, Height);
        context.FillRectangle(buildingBrush, rect);
        
        // Draw windows
        foreach (var window in Windows)
        {
            var windowBrush = new SolidColorBrush(window.IsOn ? theme.LightOnColor : theme.LightOffColor);
            var windowRect = new Rect(X + window.X, Y + window.Y, window.Width, window.Height);
            context.FillRectangle(windowBrush, windowRect);
        }
    }
}

/// <summary>
/// A scrolling hill silhouette
/// </summary>
public class HillElement : BackgroundElement
{
    public List<Point> HillShape { get; set; } = new();
    
    public override void Update(double deltaTime, double canvasWidth, double canvasHeight)
    {
        X -= Speed * deltaTime;
        
        if (X + Width < 0)
        {
            X = canvasWidth;
        }
    }
    
    public override void Render(DrawingContext context, BackgroundThemeConfig theme)
    {
        if (HillShape.Count < 3) return;
        
        var brush = new SolidColorBrush(theme.HillColor);
        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(X, Y + Height) };
        
        // Add hill curve points
        foreach (var point in HillShape)
        {
            figure.Segments.Add(new LineSegment { Point = new Point(X + point.X, Y + point.Y) });
        }
        
        // Close the shape at bottom
        figure.Segments.Add(new LineSegment { Point = new Point(X + Width, Y + Height) });
        figure.IsClosed = true;
        
        geometry.Figures.Add(figure);
        context.DrawGeometry(brush, null, geometry);
    }
}

/// <summary>
/// A twinkling star
/// </summary>
public class StarElement : BackgroundElement
{
    public double Brightness { get; set; } = 1.0;
    public double BrightnessDirection { get; set; } = 1.0;
    public double TwinkleSpeed { get; set; } = 1.0;
    
    public override void Update(double deltaTime, double canvasWidth, double canvasHeight)
    {
        // Gentle twinkling effect
        Brightness += BrightnessDirection * TwinkleSpeed * deltaTime;
        
        if (Brightness >= 1.0)
        {
            Brightness = 1.0;
            BrightnessDirection = -1.0;
        }
        else if (Brightness <= 0.3)
        {
            Brightness = 0.3;
            BrightnessDirection = 1.0;
        }
    }
    
    public override void Render(DrawingContext context, BackgroundThemeConfig theme)
    {
        var starColor = Color.FromArgb((byte)(255 * Brightness), 255, 255, 255);
        var brush = new SolidColorBrush(starColor);
        var rect = new Rect(X, Y, 2, 2);
        context.FillRectangle(brush, rect);
    }
}

/// <summary>
/// Main background animation controller
/// </summary>
public class AnimatedBackground : Control
{
    private readonly List<BackgroundElement> _elements = new();
    private readonly List<BackgroundElement>[] _layers = new List<BackgroundElement>[3];
    private BackgroundThemeConfig _theme = new();
    private DateTime _lastUpdate = DateTime.UtcNow;
    
    public static readonly StyledProperty<BackgroundThemeConfig> ThemeProperty =
        AvaloniaProperty.Register<AnimatedBackground, BackgroundThemeConfig>(nameof(Theme));
    
    public BackgroundThemeConfig Theme
    {
        get => GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }
    
    static AnimatedBackground()
    {
        AffectsRender<AnimatedBackground>(ThemeProperty);
    }
    
    public AnimatedBackground()
    {
        // Initialize layers
        for (int i = 0; i < _layers.Length; i++)
        {
            _layers[i] = new List<BackgroundElement>();
        }
        
        SetupCityscapeTheme();
        CreateCityscapeElements();
        
        // Start animation loop
        var timer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        timer.Tick += (s, e) => UpdateAndRender();
        timer.Start();
    }
    
    private void SetupCityscapeTheme()
    {
        _theme = new BackgroundThemeConfig
        {
            Name = "Cityscape",
            SkyColor = Color.FromRgb(5, 5, 15),
            HillColor = Color.FromRgb(15, 15, 25),
            BuildingColor = Color.FromRgb(8, 8, 12),
            LightOnColor = Color.FromRgb(255, 255, 180),
            LightOffColor = Color.FromRgb(25, 25, 35),
            StarDensity = 0.015,
            LightBlinkRate = 2.5,
            ParallaxSpeeds = new double[] { 5.0, 15.0, 30.0 } // Background, midground, foreground
        };
        Theme = _theme;
    }
    
    private void CreateCityscapeElements()
    {
        var random = Random.Shared;
        
        // Layer 0: Stars and hills (background)
        CreateStars(_layers[0], 50);
        CreateHills(_layers[0], 3);
        
        // Layer 1: Mid-distance buildings
        CreateBuildings(_layers[1], 8, 60, 120, _theme.ParallaxSpeeds[1]);
        
        // Layer 2: Foreground buildings
        CreateBuildings(_layers[2], 6, 100, 200, _theme.ParallaxSpeeds[2]);
    }
    
    private void CreateStars(List<BackgroundElement> layer, int count)
    {
        var random = Random.Shared;
        for (int i = 0; i < count; i++)
        {
            layer.Add(new StarElement
            {
                X = random.NextDouble() * 1280,
                Y = random.NextDouble() * 300, // Upper portion of screen
                Width = 2,
                Height = 2,
                Speed = 0, // Stars don't move
                TwinkleSpeed = random.NextDouble() * 0.5 + 0.3
            });
        }
    }
    
    private void CreateHills(List<BackgroundElement> layer, int count)
    {
        var random = Random.Shared;
        double spacing = 1280.0 / count;
        
        for (int i = 0; i < count; i++)
        {
            var hill = new HillElement
            {
                X = i * spacing + random.NextDouble() * spacing * 0.5,
                Y = 400 + random.NextDouble() * 100,
                Width = spacing + random.NextDouble() * 200,
                Height = 200 + random.NextDouble() * 100,
                Speed = _theme.ParallaxSpeeds[0]
            };
            
            // Generate hill shape
            int points = 8 + random.Next(4);
            for (int j = 0; j <= points; j++)
            {
                double x = (j / (double)points) * hill.Width;
                double y = Math.Sin((j / (double)points) * Math.PI) * (hill.Height * 0.6) + 
                          random.NextDouble() * (hill.Height * 0.3);
                hill.HillShape.Add(new Point(x, y));
            }
            
            layer.Add(hill);
        }
    }
    
    private void CreateBuildings(List<BackgroundElement> layer, int count, double minHeight, double maxHeight, double speed)
    {
        var random = Random.Shared;
        double spacing = 1280.0 / count;
        
        for (int i = 0; i < count; i++)
        {
            double width = 40 + random.NextDouble() * 80;
            double height = minHeight + random.NextDouble() * (maxHeight - minHeight);
            
            var building = new BuildingElement
            {
                X = i * spacing + random.NextDouble() * spacing * 0.3,
                Y = 720 - height,
                Width = width,
                Height = height,
                Speed = speed
            };
            
            // Add windows
            int windowRows = (int)(height / 25);
            int windowCols = (int)(width / 15);
            
            for (int row = 1; row < windowRows - 1; row++)
            {
                for (int col = 1; col < windowCols; col++)
                {
                    if (random.NextDouble() > 0.3) // 70% chance of window
                    {
                        building.Windows.Add(new BuildingElement.WindowLight
                        {
                            X = col * 15 + 3,
                            Y = row * 25 + 3,
                            IsOn = random.NextDouble() > 0.6,
                            NextBlinkTime = random.NextDouble() * _theme.LightBlinkRate
                        });
                    }
                }
            }
            
            layer.Add(building);
        }
    }
    
    private void UpdateAndRender()
    {
        var now = DateTime.UtcNow;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;
        
        // Update all elements in all layers
        foreach (var layer in _layers)
        {
            foreach (var element in layer)
            {
                element.Update(deltaTime, Bounds.Width, Bounds.Height);
            }
        }
        
        InvalidateVisual();
    }
    
    public override void Render(DrawingContext context)
    {
        // Fill background
        context.FillRectangle(new SolidColorBrush(_theme.SkyColor), Bounds);
        
        // Render layers back to front
        foreach (var layer in _layers)
        {
            foreach (var element in layer)
            {
                element.Render(context, _theme);
            }
        }
    }
    
    /// <summary>
    /// Switch to a different theme (extensibility point)
    /// </summary>
    public void SetTheme(BackgroundThemeConfig newTheme)
    {
        _theme = newTheme;
        Theme = newTheme;
        
        // Update element speeds
        for (int i = 0; i < _layers.Length && i < newTheme.ParallaxSpeeds.Length; i++)
        {
            foreach (var element in _layers[i])
            {
                element.Speed = newTheme.ParallaxSpeeds[i];
            }
        }
        
        InvalidateVisual();
    }
    
    /// <summary>
    /// Add custom elements (extensibility point)
    /// </summary>
    public void AddElement(BackgroundElement element, int layer = 1)
    {
        if (layer >= 0 && layer < _layers.Length)
        {
            _layers[layer].Add(element);
        }
    }
    
    /// <summary>
    /// Clear all elements (for scene transitions)
    /// </summary>
    public void ClearElements()
    {
        foreach (var layer in _layers)
        {
            layer.Clear();
        }
    }
}