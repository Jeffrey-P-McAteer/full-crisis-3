using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FullCrisis3.Core.Scenes;

public class NewGameScene : SubMenuScene
{
    public NewGameScene(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Action onBack)
        : base(spriteBatch, graphicsDevice, "New Game", onBack)
    {
    }

    protected override void UpdateContent(GameTime gameTime)
    {
        // Add new game specific logic here
        // Example: difficulty selection, character creation, etc.
    }

    protected override void DrawContent(GameTime gameTime)
    {
        if (Font == null) return;
        
        // Placeholder content for new game
        var lines = new[]
        {
            "Start a new adventure!",
            "",
            "Choose your options:",
            "• Difficulty: Normal",
            "• Character: Player",
            "• World: Default",
            "",
            "Press Enter to start..."
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