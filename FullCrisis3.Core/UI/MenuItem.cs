using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FullCrisis3.Core.Graphics;

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

    public void Draw(SpriteBatch spriteBatch, BitmapFont font)
    {
        var color = IsSelected ? SelectedColor : NormalColor;
        font.DrawString(spriteBatch, Text, Position, color);
    }

    public bool Contains(Vector2 point)
    {
        return Bounds.Contains(point);
    }
}