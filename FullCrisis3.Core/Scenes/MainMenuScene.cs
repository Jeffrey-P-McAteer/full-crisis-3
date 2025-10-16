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
    private BackgroundAnimation _backgroundAnimation;
    private SceneManager? _sceneManager;

    public MainMenuScene(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        _spriteBatch = spriteBatch;
        _graphicsDevice = graphicsDevice;
        _menu = new Menu();
        _backgroundAnimation = new BackgroundAnimation(graphicsDevice);
    }
    
    public void SetSceneManager(SceneManager sceneManager)
    {
        _sceneManager = sceneManager;
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
        
        // Load background animation (placeholder for video)
        _backgroundAnimation.LoadVideo("MenuBackground");
        _backgroundAnimation.Play();
    }

    public void Unload()
    {
    }

    public void Update(GameTime gameTime)
    {
        if (_inputManager == null) return;

        _backgroundAnimation.Update(gameTime);
        _menu.Update(_inputManager);

        if (_menu.IsItemSelected(_inputManager))
        {
            var selectedItem = _menu.GetSelectedItemText();
            switch (selectedItem)
            {
                case "New Game":
                    NavigateToSubMenu("New Game");
                    break;
                case "Load Game":
                    NavigateToSubMenu("Load Game");
                    break;
                case "Settings":
                    NavigateToSubMenu("Settings");
                    break;
                case "Quit":
                    Environment.Exit(0);
                    break;
            }
        }
    }
    
    private void NavigateToSubMenu(string menuType)
    {
        if (_sceneManager == null) return;
        
        IScene subScene = menuType switch
        {
            "New Game" => new NewGameScene(_spriteBatch, _graphicsDevice, () => _sceneManager.SetScene(this)),
            "Load Game" => new LoadGameScene(_spriteBatch, _graphicsDevice, () => _sceneManager.SetScene(this)),
            "Settings" => new SettingsScene(_spriteBatch, _graphicsDevice, () => _sceneManager.SetScene(this)),
            _ => this
        };
        
        _sceneManager.SetScene(subScene);
    }

    public void Draw(GameTime gameTime)
    {
        if (_font == null || _assetManager == null) return;

        _spriteBatch.Begin();

        // Draw background animation
        _backgroundAnimation.Draw(_spriteBatch, _assetManager.GetTexture("Pixel"));

        // Draw title with drop shadow
        var shadowOffset = new Vector2(3, 3);
        _font.DrawString(_spriteBatch, "Full Crisis 3", _titlePosition + shadowOffset, Color.Black * 0.5f, 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0f);
        _font.DrawString(_spriteBatch, "Full Crisis 3", _titlePosition, Color.White, 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0f);

        // Draw menu with outlines
        _menu.Draw(_spriteBatch, _font, _assetManager);

        _spriteBatch.End();
    }
}