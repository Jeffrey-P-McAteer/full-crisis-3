using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.Assets;
using FullCrisis3.Core.UI;
using FullCrisis3.Core.Graphics;
using System;

namespace FullCrisis3.Core.Scenes;

public class SubMenuScene : IScene
{
    protected readonly SpriteBatch _spriteBatch;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _title;
    private readonly Action _onBack;
    
    private InputManager? _inputManager;
    private AssetManager? _assetManager;
    private BitmapFont? _font;
    private MenuItem _backButton;
    private Vector2 _titlePosition;
    private Rectangle _contentArea;

    public SubMenuScene(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, string title, Action onBack)
    {
        _spriteBatch = spriteBatch;
        _graphicsDevice = graphicsDevice;
        _title = title;
        _onBack = onBack;
        _backButton = new MenuItem("Back", Vector2.Zero);
    }

    public void Load(InputManager inputManager, AssetManager assetManager)
    {
        _inputManager = inputManager;
        _assetManager = assetManager;
        _font = _assetManager.GetFont("DefaultFont");

        var viewport = _graphicsDevice.Viewport;
        
        // Title in top-left
        _titlePosition = new Vector2(50, 50);
        
        // Back button in lower-left
        var backPosition = new Vector2(50, viewport.Height - 100);
        _backButton = new MenuItem("Back", backPosition);
        _backButton.UpdateBounds(_font);
        
        // Content area in center-right
        _contentArea = new Rectangle(
            300, // Leave space for title and back button
            100,
            viewport.Width - 350, // Right margin
            viewport.Height - 200 // Top and bottom margins
        );
    }

    public void Unload()
    {
    }

    public void Update(GameTime gameTime)
    {
        if (_inputManager == null) return;

        // Handle back button
        var mousePos = _inputManager.MousePosition;
        _backButton.IsSelected = _backButton.Contains(mousePos);
        
        if (_inputManager.IsCancel() || 
            (_backButton.IsSelected && _inputManager.IsConfirm()))
        {
            _onBack();
        }
        
        // Update content area - override this in derived classes
        UpdateContent(gameTime);
    }

    protected virtual void UpdateContent(GameTime gameTime)
    {
        // Override in derived classes for specific sub-menu content
    }

    public void Draw(GameTime gameTime)
    {
        if (_font == null || _assetManager == null) return;

        _spriteBatch.Begin();

        // Draw title
        _font.DrawString(_spriteBatch, _title, _titlePosition, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

        // Draw back button
        _backButton.Draw(_spriteBatch, _font, _assetManager);

        // Draw content area border (for development)
        var pixel = _assetManager.GetTexture("Pixel");
        var borderColor = Color.Gray;
        var thickness = 2;
        
        // Content area outline
        _spriteBatch.Draw(pixel, new Rectangle(_contentArea.X, _contentArea.Y, _contentArea.Width, thickness), borderColor);
        _spriteBatch.Draw(pixel, new Rectangle(_contentArea.X, _contentArea.Y + _contentArea.Height - thickness, _contentArea.Width, thickness), borderColor);
        _spriteBatch.Draw(pixel, new Rectangle(_contentArea.X, _contentArea.Y, thickness, _contentArea.Height), borderColor);
        _spriteBatch.Draw(pixel, new Rectangle(_contentArea.X + _contentArea.Width - thickness, _contentArea.Y, thickness, _contentArea.Height), borderColor);

        // Draw content - override this in derived classes
        DrawContent(gameTime);

        _spriteBatch.End();
    }

    protected virtual void DrawContent(GameTime gameTime)
    {
        // Override in derived classes for specific sub-menu content
        if (_font == null) return;
        
        // Placeholder text
        var placeholderText = "Content area for " + _title;
        var textSize = _font.MeasureString(placeholderText);
        var textPosition = new Vector2(
            _contentArea.X + (_contentArea.Width - textSize.X) / 2,
            _contentArea.Y + (_contentArea.Height - textSize.Y) / 2
        );
        
        _font.DrawString(_spriteBatch, placeholderText, textPosition, Color.LightGray);
    }

    protected Rectangle ContentArea => _contentArea;
    protected BitmapFont? Font => _font;
    protected AssetManager? AssetManager => _assetManager;
    protected InputManager? InputManager => _inputManager;
}