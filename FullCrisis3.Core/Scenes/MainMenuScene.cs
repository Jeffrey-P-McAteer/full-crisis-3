using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.Assets;
using FullCrisis3.Core.UI;
using FullCrisis3.Core.Graphics;
using System;

namespace FullCrisis3.Core.Scenes;

public class MainMenuScene : IScene
{
    private readonly SpriteBatch _spriteBatch;
    private readonly GraphicsDevice _graphicsDevice;
    private InputManager? _inputManager;
    private AssetManager? _assetManager;
    private Menu _menu;
    private BitmapFont? _font;
    private Vector2 _titlePosition;
    private Vector2 _screenCenter;

    public MainMenuScene(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        _spriteBatch = spriteBatch;
        _graphicsDevice = graphicsDevice;
        _menu = new Menu();
    }

    public void Load(InputManager inputManager, AssetManager assetManager)
    {
        _inputManager = inputManager;
        _assetManager = assetManager;
        _font = _assetManager.GetFont("DefaultFont");

        var viewport = _graphicsDevice.Viewport;
        _screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

        var titleSize = _font.MeasureString("Full Crisis 3");
        _titlePosition = new Vector2(_screenCenter.X - titleSize.X / 2, _screenCenter.Y - 200);

        var menuStartY = _screenCenter.Y - 50;
        var menuSpacing = 50;

        _menu.AddItem("New Game", new Vector2(_screenCenter.X - _font.MeasureString("New Game").X / 2, menuStartY));
        _menu.AddItem("Load Game", new Vector2(_screenCenter.X - _font.MeasureString("Load Game").X / 2, menuStartY + menuSpacing));
        _menu.AddItem("Settings", new Vector2(_screenCenter.X - _font.MeasureString("Settings").X / 2, menuStartY + menuSpacing * 2));
        _menu.AddItem("Quit", new Vector2(_screenCenter.X - _font.MeasureString("Quit").X / 2, menuStartY + menuSpacing * 3));

        _menu.UpdateItemBounds(_font);
    }

    public void Unload()
    {
    }

    public void Update(GameTime gameTime)
    {
        if (_inputManager == null) return;

        _menu.Update(_inputManager);

        if (_menu.IsItemSelected(_inputManager))
        {
            var selectedItem = _menu.GetSelectedItemText();
            switch (selectedItem)
            {
                case "New Game":
                    break;
                case "Load Game":
                    break;
                case "Settings":
                    break;
                case "Quit":
                    Environment.Exit(0);
                    break;
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (_font == null) return;

        _spriteBatch.Begin();

        _font.DrawString(_spriteBatch, "Full Crisis 3", _titlePosition, Color.White, 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0f);

        _menu.Draw(_spriteBatch, _font);

        _spriteBatch.End();
    }
}