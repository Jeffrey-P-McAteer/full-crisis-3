using Microsoft.Xna.Framework.Input;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FullCrisis3.Core.Input;

public class GamepadInputService : IDisposable
{
    private readonly Subject<GamepadInput> _inputSubject = new();
    private readonly Subject<string> _debugSubject = new();
    private readonly IDisposable _pollTimer;
    private GamePadState _previousState;
    private bool _wasConnected;
    private string _currentGamepadName = "None";

    public GamepadInputService()
    {
        _previousState = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
        _wasConnected = _previousState.IsConnected;
        
        // Poll gamepad state every 16ms (~60fps)
        _pollTimer = Observable.Interval(TimeSpan.FromMilliseconds(16))
            .Subscribe(_ => PollGamepad());

        // Initial connection check
        CheckGamepadConnection();
    }

    public IObservable<GamepadInput> InputObservable => _inputSubject.AsObservable();
    public IObservable<string> DebugObservable => _debugSubject.AsObservable();

    private void PollGamepad()
    {
        var currentState = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
        
        // Check for connection changes
        if (currentState.IsConnected != _wasConnected)
        {
            _wasConnected = currentState.IsConnected;
            CheckGamepadConnection();
        }
        
        if (!currentState.IsConnected)
            return;

        // Check for button presses
        if (IsButtonPressed(Buttons.A, _previousState, currentState))
        {
            _inputSubject.OnNext(GamepadInput.Confirm);
            _debugSubject.OnNext($"Gamepad: A button pressed on {_currentGamepadName}");
        }
        
        if (IsButtonPressed(Buttons.B, _previousState, currentState))
        {
            _inputSubject.OnNext(GamepadInput.Cancel);
            _debugSubject.OnNext($"Gamepad: B button pressed on {_currentGamepadName}");
        }
        
        if (IsButtonPressed(Buttons.DPadUp, _previousState, currentState) ||
            IsThumbstickUp(_previousState, currentState))
        {
            _inputSubject.OnNext(GamepadInput.NavigateUp);
            _debugSubject.OnNext($"Gamepad: Navigate Up on {_currentGamepadName}");
        }
        
        if (IsButtonPressed(Buttons.DPadDown, _previousState, currentState) ||
            IsThumbstickDown(_previousState, currentState))
        {
            _inputSubject.OnNext(GamepadInput.NavigateDown);
            _debugSubject.OnNext($"Gamepad: Navigate Down on {_currentGamepadName}");
        }
        
        if (IsButtonPressed(Buttons.DPadLeft, _previousState, currentState) ||
            IsThumbstickLeft(_previousState, currentState))
        {
            _inputSubject.OnNext(GamepadInput.NavigateLeft);
            _debugSubject.OnNext($"Gamepad: Navigate Left on {_currentGamepadName}");
        }
        
        if (IsButtonPressed(Buttons.DPadRight, _previousState, currentState) ||
            IsThumbstickRight(_previousState, currentState))
        {
            _inputSubject.OnNext(GamepadInput.NavigateRight);
            _debugSubject.OnNext($"Gamepad: Navigate Right on {_currentGamepadName}");
        }

        _previousState = currentState;
    }

    private void CheckGamepadConnection()
    {
        var state = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
        
        if (state.IsConnected)
        {
            // Try to get gamepad capabilities for more info
            var capabilities = GamePad.GetCapabilities(Microsoft.Xna.Framework.PlayerIndex.One);
            var gamepadType = capabilities.GamePadType.ToString();
            var identifier = capabilities.Identifier ?? "Unknown";
            
            _currentGamepadName = $"{gamepadType} ({identifier})";
            _debugSubject.OnNext($"GAMEPAD CONNECTED: {_currentGamepadName}");
            _debugSubject.OnNext($"  - Has A Button: {capabilities.HasAButton}");
            _debugSubject.OnNext($"  - Has D-Pad: {capabilities.HasDPadUpButton}");
            _debugSubject.OnNext($"  - Has Left Stick: {capabilities.HasLeftXThumbStick}");
        }
        else
        {
            _currentGamepadName = "None";
            _debugSubject.OnNext("GAMEPAD DISCONNECTED");
        }
    }

    private static bool IsButtonPressed(Buttons button, GamePadState previous, GamePadState current)
    {
        return current.IsButtonDown(button) && !previous.IsButtonDown(button);
    }

    private static bool IsThumbstickUp(GamePadState previous, GamePadState current)
    {
        return current.ThumbSticks.Left.Y > 0.5f && previous.ThumbSticks.Left.Y <= 0.5f;
    }

    private static bool IsThumbstickDown(GamePadState previous, GamePadState current)
    {
        return current.ThumbSticks.Left.Y < -0.5f && previous.ThumbSticks.Left.Y >= -0.5f;
    }

    private static bool IsThumbstickLeft(GamePadState previous, GamePadState current)
    {
        return current.ThumbSticks.Left.X < -0.5f && previous.ThumbSticks.Left.X >= -0.5f;
    }

    private static bool IsThumbstickRight(GamePadState previous, GamePadState current)
    {
        return current.ThumbSticks.Left.X > 0.5f && previous.ThumbSticks.Left.X <= 0.5f;
    }

    public void Dispose()
    {
        _pollTimer?.Dispose();
        _inputSubject?.Dispose();
        _debugSubject?.Dispose();
    }
}

public enum GamepadInput
{
    Confirm,
    Cancel,
    NavigateUp,
    NavigateDown,
    NavigateLeft,
    NavigateRight
}