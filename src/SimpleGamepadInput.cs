using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace FullCrisis3;

[AutoLog]
public class SimpleGamepadInput : IDisposable
{
    private readonly Action<string> _onInput;
    private readonly Action<bool> _onConnectionChanged;
    private readonly Timer? _pollTimer;
    private bool _wasConnected;
    private bool _disposed;

    // Button state tracking
    private readonly Dictionary<string, bool> _buttonStates = new();

    // Linux joystick file handle
    private FileStream? _jsStream;

    // Linux joystick event structure
    [StructLayout(LayoutKind.Sequential)]
    private struct JsEvent
    {
        public uint time;     // timestamp in milliseconds
        public short value;   // value
        public byte type;     // event type
        public byte number;   // axis/button number
    }

    // Linux joystick constants
    private const byte JS_EVENT_BUTTON = 0x01;
    private const byte JS_EVENT_AXIS = 0x02;
    private const byte JS_EVENT_INIT = 0x80;

    // Windows XInput structures
    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    // XInput constants
    private const uint XINPUT_GAMEPAD_DPAD_UP = 0x0001;
    private const uint XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
    private const uint XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
    private const uint XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
    private const uint XINPUT_GAMEPAD_A = 0x1000;
    private const uint XINPUT_GAMEPAD_B = 0x2000;

    // Windows XInput API (try multiple DLL names for compatibility)
    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState_1_4(uint dwUserIndex, ref XInputState pState);

    [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState_1_3(uint dwUserIndex, ref XInputState pState);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState_9_1_0(uint dwUserIndex, ref XInputState pState);

    public static string GetControllerName()
    {
        try
        {
            Logger.LogMethod("GetControllerName", "Checking for gamepad...");
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Logger.LogMethod("GetControllerName", "Windows detected - trying XInput");
                if (TryGetXInputState(out _))
                {
                    return "Xbox Controller (XInput)";
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Logger.LogMethod("GetControllerName", "Linux detected - checking /dev/input");
                var jsDevices = Directory.GetFiles("/dev/input", "js*");
                var eventDevices = Directory.GetFiles("/dev/input", "event*");
                
                Logger.LogMethod("GetControllerName", $"Found {jsDevices.Length} joystick devices, {eventDevices.Length} event devices");
                
                if (jsDevices.Length > 0)
                {
                    return $"Gamepad (Linux joystick)";
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("GetControllerName", $"Error: {ex.Message}");
        }
        return "No controller detected";
    }

    private static bool TryGetXInputState(out XInputState state)
    {
        state = new XInputState();
        
        // Try different XInput DLL versions
        try
        {
            return XInputGetState_1_4(0, ref state) == 0;
        }
        catch
        {
            try
            {
                return XInputGetState_1_3(0, ref state) == 0;
            }
            catch
            {
                try
                {
                    return XInputGetState_9_1_0(0, ref state) == 0;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    public bool IsConnected
    {
        get
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return TryGetXInputState(out _);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return Directory.GetFiles("/dev/input", "js*").Length > 0;
                }
            }
            catch
            {
                // Ignore errors
            }
            return false;
        }
    }

    public SimpleGamepadInput(Action<string> onInput, Action<bool>? onConnectionChanged = null)
    {

        _onInput = onInput;
        _onConnectionChanged = onConnectionChanged ?? (_ => { });
        
        try
        {
            Logger.LogMethod("SimpleGamepadInput Constructor", "Starting simple gamepad polling...");
            
            // Check initial state
            _wasConnected = IsConnected;
            Logger.LogMethod("SimpleGamepadInput Constructor", $"Initial connection state: {_wasConnected}");
            
            // Poll for gamepad state changes every 16ms (60 FPS)
            _pollTimer = new Timer(Poll, null, 100, 16);
            Logger.LogMethod("SimpleGamepadInput Constructor", "Gamepad polling started");
        }
        catch (Exception ex)
        {
            Logger.LogMethod("SimpleGamepadInput Constructor", $"Error: {ex.Message}");
        }
    }

    private void Poll(object? state)
    {
        if (_disposed) return;

        try
        {
            bool isConnected = IsConnected;
            
            // Check for connection changes
            if (isConnected != _wasConnected)
            {
                _wasConnected = isConnected;
                _onConnectionChanged(_wasConnected);
                Logger.LogMethod("Poll", $"Connection state changed: {_wasConnected}");
                
                if (!isConnected)
                {
                    // Clear button states when disconnected
                    _buttonStates.Clear();
                }
            }
            
            if (!isConnected) return;

            // Read input based on platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                PollXInput();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                PollLinuxJoystick();
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("Poll", $"Error: {ex.Message}");
        }
    }

    private void PollXInput()
    {
        try
        {
            if (!TryGetXInputState(out XInputState state)) return;
            
            var buttons = state.Gamepad.wButtons;
            
            // Check for button presses (rising edge detection)
            CheckButtonPress("A", (buttons & XINPUT_GAMEPAD_A) != 0, "Confirm");
            CheckButtonPress("B", (buttons & XINPUT_GAMEPAD_B) != 0, "Cancel");
            CheckButtonPress("DPadUp", (buttons & XINPUT_GAMEPAD_DPAD_UP) != 0, "Up");
            CheckButtonPress("DPadDown", (buttons & XINPUT_GAMEPAD_DPAD_DOWN) != 0, "Down");
            CheckButtonPress("DPadLeft", (buttons & XINPUT_GAMEPAD_DPAD_LEFT) != 0, "Left");
            CheckButtonPress("DPadRight", (buttons & XINPUT_GAMEPAD_DPAD_RIGHT) != 0, "Right");
        }
        catch (Exception ex)
        {
            Logger.LogMethod("PollXInput", $"Error: {ex.Message}");
        }
    }

    private void CheckButtonPress(string buttonName, bool isPressed, string inputName)
    {
        try
        {
            bool wasPressed = _buttonStates.GetValueOrDefault(buttonName, false);
            
            // Rising edge detection: button is pressed now but wasn't before
            if (isPressed && !wasPressed)
            {
                Logger.LogMethod("CheckButtonPress", $"Button {buttonName} pressed -> {inputName}");
                _onInput(inputName);
            }
            
            _buttonStates[buttonName] = isPressed;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("CheckButtonPress", $"Error checking button {buttonName}: {ex.Message}");
        }
    }

    private void PollLinuxJoystick()
    {
        try
        {
            // Open joystick device if not already open
            if (_jsStream == null)
            {
                var jsDevices = Directory.GetFiles("/dev/input", "js*");
                if (jsDevices.Length > 0)
                {
                    try
                    {
                        _jsStream = new FileStream(jsDevices[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        // Set to non-blocking mode
                        _jsStream.ReadTimeout = 1;
                        Logger.LogMethod("PollLinuxJoystick", $"Opened joystick device: {jsDevices[0]}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogMethod("PollLinuxJoystick", $"Failed to open joystick device: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    return; // No joystick devices found
                }
            }

            // Read joystick events
            byte[] buffer = new byte[8]; // JsEvent is 8 bytes
            
            while (_jsStream != null && _jsStream.CanRead)
            {
                try
                {
                    int bytesRead = _jsStream.Read(buffer, 0, 8);
                    if (bytesRead != 8) break; // Need exactly 8 bytes for a complete event

                    // Parse the joystick event
                    var jsEvent = new JsEvent
                    {
                        time = BitConverter.ToUInt32(buffer, 0),
                        value = BitConverter.ToInt16(buffer, 4),
                        type = buffer[6],
                        number = buffer[7]
                    };

                    // Skip init events
                    if ((jsEvent.type & JS_EVENT_INIT) != 0) continue;

                    // Process button events
                    if ((jsEvent.type & JS_EVENT_BUTTON) != 0)
                    {
                        ProcessLinuxButton(jsEvent.number, jsEvent.value != 0);
                    }
                    // Process axis events for D-pad simulation
                    else if ((jsEvent.type & JS_EVENT_AXIS) != 0)
                    {
                        ProcessLinuxAxis(jsEvent.number, jsEvent.value);
                    }
                }
                catch (TimeoutException)
                {
                    // No more data available, this is normal
                    break;
                }
                catch (IOException)
                {
                    // Device likely disconnected
                    _jsStream?.Dispose();
                    _jsStream = null;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("PollLinuxJoystick", $"Error: {ex.Message}");
        }
    }

    private void ProcessLinuxButton(byte buttonNumber, bool isPressed)
    {
        try
        {
            // Map Xbox controller button numbers to input names
            string? inputName = buttonNumber switch
            {
                0 => "Confirm",  // A button (button 0 on Xbox controllers)
                1 => "Cancel",   // B button (button 1 on Xbox controllers) 
                2 => null,       // X button (button 2) - not mapped
                3 => null,       // Y button (button 3) - not mapped
                _ => null
            };

            if (inputName != null)
            {
                CheckButtonPress($"Button{buttonNumber}", isPressed, inputName);
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("ProcessLinuxButton", $"Error processing button {buttonNumber}: {ex.Message}");
        }
    }

    private void ProcessLinuxAxis(byte axisNumber, short value)
    {
        try
        {
            // Map D-pad axes to directional input
            // Xbox controller D-pad is typically on axes 6 and 7
            if (axisNumber == 6) // Horizontal D-pad axis
            {
                if (value < -16384) // Left
                {
                    CheckButtonPress("DPadLeft", true, "Left");
                    CheckButtonPress("DPadRight", false, "Right");
                }
                else if (value > 16384) // Right  
                {
                    CheckButtonPress("DPadRight", true, "Right");
                    CheckButtonPress("DPadLeft", false, "Left");
                }
                else // Center
                {
                    CheckButtonPress("DPadLeft", false, "Left");
                    CheckButtonPress("DPadRight", false, "Right");
                }
            }
            else if (axisNumber == 7) // Vertical D-pad axis
            {
                if (value < -16384) // Up
                {
                    CheckButtonPress("DPadUp", true, "Up");
                    CheckButtonPress("DPadDown", false, "Down");
                }
                else if (value > 16384) // Down
                {
                    CheckButtonPress("DPadDown", true, "Down");
                    CheckButtonPress("DPadUp", false, "Up");
                }
                else // Center
                {
                    CheckButtonPress("DPadUp", false, "Up");
                    CheckButtonPress("DPadDown", false, "Down");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("ProcessLinuxAxis", $"Error processing axis {axisNumber}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        try
        {
            Logger.LogMethod("Dispose", "Disposing simple gamepad input...");
            _pollTimer?.Dispose();
            _jsStream?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogMethod("Dispose", $"Error during disposal: {ex.Message}");
        }
    }
}