using Microsoft.Xna.Framework;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.Assets;

namespace FullCrisis3.Core.Scenes;

public class SceneManager
{
    private IScene? _currentScene;
    private readonly InputManager _inputManager;
    private readonly AssetManager _assetManager;

    public SceneManager(InputManager inputManager, AssetManager assetManager)
    {
        _inputManager = inputManager;
        _assetManager = assetManager;
    }

    public void SetScene(IScene scene)
    {
        _currentScene?.Unload();
        _currentScene = scene;
        _currentScene.Load(_inputManager, _assetManager);
    }

    public void Update(GameTime gameTime)
    {
        _currentScene?.Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        _currentScene?.Draw(gameTime);
    }
}