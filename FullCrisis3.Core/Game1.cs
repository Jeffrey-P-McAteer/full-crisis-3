using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FullCrisis3.Core.Scenes;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.Assets;

namespace FullCrisis3.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SceneManager _sceneManager = null!;
    private InputManager _inputManager = null!;
    private AssetManager _assetManager = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        _inputManager = new InputManager();
        _assetManager = new AssetManager(GraphicsDevice);
        _sceneManager = new SceneManager(_inputManager, _assetManager);
        
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        _assetManager.LoadFont("DefaultFont");
        
        _sceneManager.SetScene(new MainMenuScene(_spriteBatch, GraphicsDevice));
    }

    protected override void Update(GameTime gameTime)
    {
        _inputManager.Update();
        
        if (_inputManager.IsKeyPressed(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        _sceneManager.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _sceneManager.Draw(gameTime);

        base.Draw(gameTime);
    }
}