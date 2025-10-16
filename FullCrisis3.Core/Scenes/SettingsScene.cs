using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FullCrisis3.Core.Scenes;

public class SettingsScene : SubMenuScene
{
    public SettingsScene(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Action onBack)
        : base(spriteBatch, graphicsDevice, "Settings", onBack)
    {
    }

    protected override void UpdateContent(GameTime gameTime)
    {
        // Add settings specific logic here
        // Example: volume sliders, graphics options, controls configuration
    }

    protected override void DrawContent(GameTime gameTime)
    {
        if (Font == null) return;
        
        // Placeholder content for settings
        var lines = new[]
        {
            "Game Settings:",
            "",
            "Audio:",
            "• Master Volume: 100%",
            "• Music Volume: 80%",
            "• SFX Volume: 100%",
            "",
            "Graphics:",
            "• Resolution: 1280x720",
            "• Fullscreen: Off",
            "",
            "Controls:",
            "• Keyboard/Mouse: Enabled",
            "• Gamepad: Enabled"
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