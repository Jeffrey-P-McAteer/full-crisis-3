using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FullCrisis3.Core.Scenes;

public class LoadGameScene : SubMenuScene
{
    public LoadGameScene(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Action onBack)
        : base(spriteBatch, graphicsDevice, "Load Game", onBack)
    {
    }

    protected override void UpdateContent(GameTime gameTime)
    {
        // Add load game specific logic here
        // Example: save file selection, preview, etc.
    }

    protected override void DrawContent(GameTime gameTime)
    {
        if (Font == null) return;
        
        // Placeholder content for load game
        var lines = new[]
        {
            "Load a saved game:",
            "",
            "Save Slots:",
            "• Slot 1: Empty",
            "• Slot 2: Empty", 
            "• Slot 3: Empty",
            "",
            "No saved games found."
        };
        
        var lineHeight = 30;
        var startY = ContentArea.Y + 50;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var position = new Vector2(ContentArea.X + 20, startY + i * lineHeight);
            Font.DrawString(_spriteBatch, lines[i], position, Color.White);
        }
    }
}