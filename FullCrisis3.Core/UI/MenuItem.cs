using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FullCrisis3.Core.Graphics;
using FullCrisis3.Core.Assets;

namespace FullCrisis3.Core.UI;

public class MenuItem
{
    public string Text { get; set; }
    public Vector2 Position { get; set; }
    public bool IsSelected { get; set; }
    public Color NormalColor { get; set; } = Color.White;
    public Color SelectedColor { get; set; } = Color.Yellow;
    public Rectangle Bounds { get; private set; }

    public MenuItem(string text, Vector2 position)
    {
        Text = text;
        Position = position;
    }

    public void UpdateBounds(BitmapFont font)
    {
        var size = font.MeasureString(Text);
        Bounds = new Rectangle((int)Position.X, (int)Position.Y, (int)size.X, (int)size.Y);
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font, AssetManager? assetManager = null)
    {
        var color = IsSelected ? SelectedColor : NormalColor;
        
        // Draw outline for selected item
        if (IsSelected && assetManager != null)
        {
            var padding = 10;
            var outlineRect = new Rectangle(
                Bounds.X - padding,
                Bounds.Y - padding,
                Bounds.Width + padding * 2,
                Bounds.Height + padding * 2
            );
            DrawRectangleOutline(spriteBatch, assetManager, outlineRect, Color.White, 2);
        }
        
        font.DrawString(spriteBatch, Text, Position, color);
    }
    
    private void DrawRectangleOutline(SpriteBatch spriteBatch, AssetManager assetManager, Rectangle rectangle, Color color, int thickness)
    {
        var pixel = assetManager.GetTexture("Pixel");
        
        // Draw the four lines of the outline
        // Top
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + rectangle.Height - thickness, rectangle.Width, thickness), color);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + rectangle.Width - thickness, rectangle.Y, thickness, rectangle.Height), color);
    }

    public bool Contains(Vector2 point)
    {
        return Bounds.Contains(point);
    }
}