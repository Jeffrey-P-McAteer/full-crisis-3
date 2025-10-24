using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace FullCrisis3;

/// <summary>
/// A control that renders an animated dashed border around focused buttons
/// </summary>
public class AnimatedDashedBorder : Control
{
    private double _animationOffset = 0;
    private readonly Avalonia.Threading.DispatcherTimer _animationTimer;
    
    // Colors for the dashed pattern
    private readonly Color _lightGrey = Color.FromRgb(156, 163, 175); // #FF9CA3AF
    private readonly Color _darkGrey = Color.FromRgb(107, 114, 128);  // #FF6B7280
    
    public static readonly StyledProperty<double> DashLengthProperty =
        AvaloniaProperty.Register<AnimatedDashedBorder, double>(nameof(DashLength), 8.0);
    
    public static readonly StyledProperty<double> AnimationSpeedProperty =
        AvaloniaProperty.Register<AnimatedDashedBorder, double>(nameof(AnimationSpeed), 20.0);
    
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<AnimatedDashedBorder, double>(nameof(StrokeThickness), 2.0);
    
    public double DashLength
    {
        get => GetValue(DashLengthProperty);
        set => SetValue(DashLengthProperty, value);
    }
    
    public double AnimationSpeed
    {
        get => GetValue(AnimationSpeedProperty);
        set => SetValue(AnimationSpeedProperty, value);
    }
    
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }
    
    static AnimatedDashedBorder()
    {
        AffectsRender<AnimatedDashedBorder>(DashLengthProperty, AnimationSpeedProperty, StrokeThicknessProperty);
    }
    
    public AnimatedDashedBorder()
    {
        _animationTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _animationTimer.Tick += (s, e) => AnimationTick();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _animationTimer.Start();
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animationTimer.Stop();
    }
    
    private void AnimationTick()
    {
        _animationOffset += AnimationSpeed * 0.016; // Delta time approximation
        if (_animationOffset >= DashLength * 2)
        {
            _animationOffset = 0;
        }
        InvalidateVisual();
    }
    
    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        
        var rect = new Rect(StrokeThickness / 2, StrokeThickness / 2, 
                           Bounds.Width - StrokeThickness, Bounds.Height - StrokeThickness);
        
        var geometry = new RectangleGeometry(rect);
        
        // Create animated dashed pattern
        var dashStyle = new DashStyle(new double[] { DashLength / StrokeThickness, DashLength / StrokeThickness }, _animationOffset / StrokeThickness);
        var lightPen = new Pen(new SolidColorBrush(_lightGrey), StrokeThickness) { DashStyle = dashStyle };
        
        var darkDashStyle = new DashStyle(new double[] { DashLength / StrokeThickness, DashLength / StrokeThickness }, (_animationOffset + DashLength) / StrokeThickness);
        var darkPen = new Pen(new SolidColorBrush(_darkGrey), StrokeThickness) { DashStyle = darkDashStyle };
        
        // Draw the dashed border with alternating colors
        context.DrawGeometry(null, lightPen, geometry);
        context.DrawGeometry(null, darkPen, geometry);
    }
}