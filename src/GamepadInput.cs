using Microsoft.Xna.Framework.Input;
using System;
using System.Threading;

namespace FullCrisis3;

public class GamepadInput : IDisposable
{
    private readonly Timer _timer;
    private readonly Action<string> _onInput;
    private GamePadState _previousState;

    public GamepadInput(Action<string> onInput)
    {
        Logger.LogMethod();
        _onInput = onInput;
        _previousState = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
        _timer = new Timer(Poll, null, 0, 16);
    }

    private void Poll(object? state)
    {
        try
        {
            var currentState = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
            if (!currentState.IsConnected) return;

            if (IsPressed(Buttons.A)) { Logger.Debug("Gamepad A pressed"); _onInput("Confirm"); }
            if (IsPressed(Buttons.B)) { Logger.Debug("Gamepad B pressed"); _onInput("Cancel"); }
            if (IsPressed(Buttons.DPadUp)) { Logger.Debug("Gamepad Up pressed"); _onInput("Up"); }
            if (IsPressed(Buttons.DPadDown)) { Logger.Debug("Gamepad Down pressed"); _onInput("Down"); }

            _previousState = currentState;

            bool IsPressed(Buttons button) => currentState.IsButtonDown(button) && !_previousState.IsButtonDown(button);
        }
        catch { }
    }

    public void Dispose() => _timer?.Dispose();
}