using Microsoft.Xna.Framework;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.Assets;
using System.Collections.Generic;

namespace FullCrisis3.Core.Scenes;

public class SceneManager
{
    private IScene? _currentScene;
    private readonly Stack<IScene> _sceneStack;
    private readonly InputManager _inputManager;
    private readonly AssetManager _assetManager;

    public SceneManager(InputManager inputManager, AssetManager assetManager)
    {
        _inputManager = inputManager;
        _assetManager = assetManager;
        _sceneStack = new Stack<IScene>();
    }

    public void SetScene(IScene scene)
    {
        _currentScene?.Unload();
        _currentScene = scene;
        _currentScene.Load(_inputManager, _assetManager);
        
        // If it's the main menu, provide a reference to this scene manager
        if (_currentScene is MainMenuScene mainMenu)
        {
            mainMenu.SetSceneManager(this);
        }
    }

    public void PushScene(IScene scene)
    {
        if (_currentScene != null)
        {
            _sceneStack.Push(_currentScene);
        }
        SetScene(scene);
    }

    public void PopScene()
    {
        if (_sceneStack.Count > 0)
        {
            var previousScene = _sceneStack.Pop();
            SetScene(previousScene);
        }
    }

    public bool CanGoBack()
    {
        return _sceneStack.Count > 0;
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