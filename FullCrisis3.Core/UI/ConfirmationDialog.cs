using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.Graphics;
using FullCrisis3.Core.Assets;
using System;

namespace FullCrisis3.Core.UI;

public class ConfirmationDialog
{
    private readonly string _title;
    private readonly string _confirmText;
    private readonly string _cancelText;
    private readonly Action _onConfirm;
    private readonly Action _onCancel;
    private int _selectedIndex;
    private Vector2 _position;
    private Vector2 _titlePosition;
    private Vector2 _confirmPosition;
    private Vector2 _cancelPosition;
    private Rectangle _dialogBounds;
    private bool _isInitialized;

    public ConfirmationDialog(string title, string confirmText, string cancelText, Action onConfirm, Action onCancel)
    {
        _title = title;
        _confirmText = confirmText;
        _cancelText = cancelText;
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _selectedIndex = 1; // Default to cancel for safety
    }

    public void Initialize(GraphicsDevice graphicsDevice, BitmapFont font)
    {
        if (_isInitialized) return;

        var viewport = graphicsDevice.Viewport;
        var screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

        // Measure text sizes
        var titleSize = font.MeasureString(_title);
        var confirmSize = font.MeasureString(_confirmText);
        var cancelSize = font.MeasureString(_cancelText);

        // Calculate dialog dimensions
        var maxWidth = Math.Max(titleSize.X, Math.Max(confirmSize.X, cancelSize.X)) + 60;
        var dialogHeight = titleSize.Y + confirmSize.Y + cancelSize.Y + 80;

        // Calculate positions
        _position = new Vector2(screenCenter.X - maxWidth / 2, screenCenter.Y - dialogHeight / 2);
        _dialogBounds = new Rectangle((int)_position.X, (int)_position.Y, (int)maxWidth, (int)dialogHeight);

        _titlePosition = new Vector2(screenCenter.X - titleSize.X / 2, _position.Y + 20);
        _confirmPosition = new Vector2(screenCenter.X - confirmSize.X / 2, _titlePosition.Y + titleSize.Y + 30);
        _cancelPosition = new Vector2(screenCenter.X - cancelSize.X / 2, _confirmPosition.Y + confirmSize.Y + 10);

        _isInitialized = true;
    }

    public void Update(InputManager inputManager)
    {
        if (inputManager.IsNavigateUp())
        {
            _selectedIndex = (_selectedIndex - 1 + 2) % 2;
        }
        else if (inputManager.IsNavigateDown())
        {
            _selectedIndex = (_selectedIndex + 1) % 2;
        }

        if (inputManager.IsConfirm())
        {
            if (_selectedIndex == 0)
                _onConfirm();
            else
                _onCancel();
        }
        else if (inputManager.IsCancel())
        {
            _onCancel();
        }
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font, AssetManager assetManager)
    {
        if (!_isInitialized) return;

        var pixelTexture = assetManager.GetTexture("Pixel");

        // Draw semi-transparent background overlay
        spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.Black * 0.5f);

        // Draw dialog background
        spriteBatch.Draw(pixelTexture, _dialogBounds, Color.DarkSlateGray);

        // Draw dialog border
        var borderThickness = 2;
        spriteBatch.Draw(pixelTexture, new Rectangle(_dialogBounds.X, _dialogBounds.Y, _dialogBounds.Width, borderThickness), Color.White);
        spriteBatch.Draw(pixelTexture, new Rectangle(_dialogBounds.X, _dialogBounds.Bottom - borderThickness, _dialogBounds.Width, borderThickness), Color.White);
        spriteBatch.Draw(pixelTexture, new Rectangle(_dialogBounds.X, _dialogBounds.Y, borderThickness, _dialogBounds.Height), Color.White);
        spriteBatch.Draw(pixelTexture, new Rectangle(_dialogBounds.Right - borderThickness, _dialogBounds.Y, borderThickness, _dialogBounds.Height), Color.White);

        // Draw title
        font.DrawString(spriteBatch, _title, _titlePosition, Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

        // Draw confirm option
        var confirmColor = _selectedIndex == 0 ? Color.Yellow : Color.White;
        if (_selectedIndex == 0)
        {
            var confirmBounds = new Rectangle((int)_confirmPosition.X - 10, (int)_confirmPosition.Y - 5, 
                (int)font.MeasureString(_confirmText).X + 20, (int)font.MeasureString(_confirmText).Y + 10);
            spriteBatch.Draw(pixelTexture, confirmBounds, Color.DarkBlue * 0.3f);
        }
        font.DrawString(spriteBatch, _confirmText, _confirmPosition, confirmColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

        // Draw cancel option
        var cancelColor = _selectedIndex == 1 ? Color.Yellow : Color.White;
        if (_selectedIndex == 1)
        {
            var cancelBounds = new Rectangle((int)_cancelPosition.X - 10, (int)_cancelPosition.Y - 5,
                (int)font.MeasureString(_cancelText).X + 20, (int)font.MeasureString(_cancelText).Y + 10);
            spriteBatch.Draw(pixelTexture, cancelBounds, Color.DarkBlue * 0.3f);
        }
        font.DrawString(spriteBatch, _cancelText, _cancelPosition, cancelColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
    }
}