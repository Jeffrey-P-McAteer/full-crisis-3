using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FullCrisis3.Core.Input;

public class InputManager
{
    private KeyboardState _currentKeyboardState;
    private KeyboardState _previousKeyboardState;
    private MouseState _currentMouseState;
    private MouseState _previousMouseState;
    private GamePadState _currentGamePadState;
    private GamePadState _previousGamePadState;

    public void Update()
    {
        _previousKeyboardState = _currentKeyboardState;
        _previousMouseState = _currentMouseState;
        _previousGamePadState = _currentGamePadState;

        _currentKeyboardState = Keyboard.GetState();
        _currentMouseState = Mouse.GetState();
        _currentGamePadState = GamePad.GetState(PlayerIndex.One);
    }

    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    public bool IsKeyDown(Keys key)
    {
        return _currentKeyboardState.IsKeyDown(key);
    }

    public bool IsMouseButtonPressed(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => _currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released,
            MouseButton.Right => _currentMouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released,
            MouseButton.Middle => _currentMouseState.MiddleButton == ButtonState.Pressed && _previousMouseState.MiddleButton == ButtonState.Released,
            _ => false
        };
    }

    public bool IsGamePadButtonPressed(Buttons button)
    {
        return _currentGamePadState.IsButtonDown(button) && !_previousGamePadState.IsButtonDown(button);
    }

    public Vector2 MousePosition => new(_currentMouseState.X, _currentMouseState.Y);

    public Vector2 GamePadLeftStick => _currentGamePadState.ThumbSticks.Left;

    public bool IsNavigateUp()
    {
        return IsKeyPressed(Keys.Up) || IsKeyPressed(Keys.W) ||
               IsGamePadButtonPressed(Buttons.DPadUp) ||
               (_currentGamePadState.ThumbSticks.Left.Y > 0.5f && _previousGamePadState.ThumbSticks.Left.Y <= 0.5f);
    }

    public bool IsNavigateDown()
    {
        return IsKeyPressed(Keys.Down) || IsKeyPressed(Keys.S) ||
               IsGamePadButtonPressed(Buttons.DPadDown) ||
               (_currentGamePadState.ThumbSticks.Left.Y < -0.5f && _previousGamePadState.ThumbSticks.Left.Y >= -0.5f);
    }

    public bool IsConfirm()
    {
        return IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space) ||
               IsGamePadButtonPressed(Buttons.A) ||
               IsMouseButtonPressed(MouseButton.Left);
    }

    public bool IsCancel()
    {
        return IsKeyPressed(Keys.Escape) ||
               IsGamePadButtonPressed(Buttons.B);
    }
}

public enum MouseButton
{
    Left,
    Right,
    Middle
}