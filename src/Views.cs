using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using System;
using System.Reactive;

namespace FullCrisis3;

public interface IGamepadNavigable
{
    bool HandleGamepadInput(string input);
}

[AutoLog]
public partial class MainWindow : Window
{
    private Button[] _quitDialogButtons = Array.Empty<Button>();
    private int _quitDialogSelectedIndex = 0;
    private readonly InputManager _quitDialogInputManager = new();

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        viewModel.QuitDialogNavigation = HandleGamepadQuitDialogNavigation;
        DataContext = viewModel;
        Loaded += (s, e) => SetupQuitDialogButtons();
        Loaded += this.OnWindowLoaded;
        
        // Configure window manager hints for better tiling WM support
        ConfigureWindowManagerHints();
        
        // Watch for quit dialog visibility changes to set initial focus
        viewModel.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsQuitDialogVisible))
            {
                if (viewModel.IsQuitDialogVisible)
                {
                    SetInitialQuitDialogFocus();
                }
                else
                {
                    RestoreFocusAfterQuitDialog();
                }
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.CurrentBackgroundTheme))
            {
                UpdateBackgroundTheme(viewModel.CurrentBackgroundTheme);
            }
        };
    }

    private void ConfigureWindowManagerHints()
    {
        // Use CLI arguments for window configuration
        var args = GlobalArgs.Current;
        
        // Window size from CLI arguments
        Width = args.Width;
        Height = args.Height;
        
        // Window mode based on CLI arguments
        if (args.Fullscreen)
        {
            // Remove size constraints for fullscreen
            ClearValue(WidthProperty);
            ClearValue(HeightProperty);
            ClearValue(MinWidthProperty);
            ClearValue(MinHeightProperty);
            ClearValue(MaxWidthProperty);
            ClearValue(MaxHeightProperty);
            WindowState = Avalonia.Controls.WindowState.FullScreen;
        }
        else /*if (args.Windowed || args.DebugUI)*/
        {
            WindowState = Avalonia.Controls.WindowState.Normal;
            CanResize = true; // Default allows resizing at all times
        }
        
        WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen;
        
        // These properties are already set in XAML but ensuring they're applied:
        // - Fixed dimensions (1280x720)
        // - CanResize=False 
        // - SystemDecorations=Full (shows title bar for better WM integration)
        // - SizeToContent=Manual (prevents auto-sizing)
        
        // Note: Tiling WM users can still override these with window rules like:
        // i3/sway: for_window [title="Full Crisis 3"] floating enable
        // or: for_window [class="FullCrisis3"] floating enable
    }

    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        var args = GlobalArgs.Current;

        if (!args.Fullscreen)
        {
            // Clear constraints
            ClearValue(MinWidthProperty);
            ClearValue(MinHeightProperty);
            ClearValue(MaxWidthProperty);
            ClearValue(MaxHeightProperty);
        }
    }

    private void SetupQuitDialogButtons()
    {
        _quitDialogButtons = new[]
        {
            this.FindControl<Button>("QuitButton")!,
            this.FindControl<Button>("KeepPlayingButton")!
        };

        // Setup InputManager for quit dialog
        _quitDialogInputManager.ClearSelectables();
        
        for (int i = 0; i < _quitDialogButtons.Length; i++)
        {
            var index = i;
            var button = _quitDialogButtons[i];
            
            _quitDialogInputManager.RegisterSelectable(
                button, 
                tabIndex: i,
                onSelected: item => _quitDialogSelectedIndex = index
            );
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            if (vm.IsQuitDialogVisible)
            {
                HandleQuitDialogNavigation(e);
            }
            else if (e.Key == Key.Escape)
            {
                vm.HandleEscapeKey();
                e.Handled = true;
            }
        }
        base.OnKeyDown(e);
    }

    private void HandleQuitDialogNavigation(KeyEventArgs e)
    {
        // Handle escape key specifically
        if (e.Key == Key.Escape)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.CancelQuitCommand.Execute(Unit.Default);
                e.Handled = true;
            }
            return;
        }

        // Let InputManager handle all other navigation
        if (_quitDialogInputManager.HandleKeyInput(e))
        {
            // InputManager handled the key
            return;
        }
    }

    private void SetInitialQuitDialogFocus()
    {
        if (_quitDialogButtons.Length > 0)
        {
            _quitDialogSelectedIndex = 0; // Start with first button (Quit)
            _quitDialogInputManager.SelectItem(0);
        }
    }

    private void RestoreFocusAfterQuitDialog()
    {
        // Try to focus the quit button in the main menu after a short delay
        // This allows the UI to update and the MainMenuView to be available
        if (DataContext is MainWindowViewModel vm && vm.CurrentView is MainMenuViewModel mainMenuVM)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Set focus back to quit button using the callback we added to MainMenuViewModel
                if (mainMenuVM.RestoreFocusToQuit != null)
                {
                    mainMenuVM.RestoreFocusToQuit.Invoke();
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    private void HandleGamepadQuitDialogNavigation(string input)
    {
        _quitDialogInputManager.HandleGamepadInput(input);
    }
    
    private void UpdateBackgroundTheme(BackgroundThemeConfig? newTheme)
    {
        if (newTheme == null) return;
        
        // Find the AnimatedBackground control
        var animatedBackground = this.FindLogicalDescendantOfType<AnimatedBackground>();
        if (animatedBackground != null)
        {
            animatedBackground.SetTheme(newTheme);
        }
    }
}

[AutoLog]
public partial class MainMenuView : UserControl, IGamepadNavigable
{
    public MainMenuView()
    {
        InitializeComponent();
        
        // Wire up the focus restoration callback
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainMenuViewModel vm)
            {
                vm.RestoreFocusToQuit = FocusQuitButton;
            }
        };
    }

    public void FocusQuitButton()
    {
        // Focus the quit button directly using the new Tab navigation system
        var quitButton = this.FindControl<Button>("QuitButton");
        quitButton?.Focus();
    }

    public bool HandleGamepadInput(string input)
    {
        // Return false to let the Tab simulation system handle all gamepad input
        return false;
    }
}

[AutoLog]
public partial class SubMenuView : UserControl
{
    public SubMenuView() 
    { 
        InitializeComponent(); 
    }
}

[AutoLog]
public partial class SettingsView : UserControl, IGamepadNavigable
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public bool HandleGamepadInput(string input)
    {
        // Return false to let the Tab simulation system handle all gamepad input
        return false;
    }
}

[AutoLog]
public partial class LoadGameView : UserControl, IGamepadNavigable
{
    private readonly InputManager _inputManager = new();
    private readonly InputManager _saveGamesInputManager = new();
    private LoadGameViewModel? _viewModel;
    private ListBox? _saveGamesList;
    
    public LoadGameView()
    {
        InitializeComponent();
        Loaded += (s, e) => SetupControls();
        KeyDown += OnKeyDown;
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as LoadGameViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            SetupSaveGamesNavigation();
        }
    }
    
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoadGameViewModel.SavedGames) || 
            e.PropertyName == nameof(LoadGameViewModel.HasSaveGames))
        {
            SetupSaveGamesNavigation();
        }
    }
    
    private void SetupControls()
    {
        _saveGamesList = this.FindControl<ListBox>("SaveGamesList");
        
        _inputManager.ClearSelectables();
        _inputManager.SetGridNavigation(true);
        
        // Register save games list as a navigable area
        if (_saveGamesList != null)
        {
            _inputManager.RegisterSelectable(_saveGamesList, tabIndex: 0, gridRow: 0, gridColumn: 0);
        }
        
        // Register action buttons in a row below
        var deleteButton = this.FindControl<Button>("DeleteButton");
        var refreshButton = this.FindControl<Button>("RefreshButton");
        var backButton = this.FindControl<Button>("BackButton");
        
        if (deleteButton != null)
            _inputManager.RegisterSelectable(deleteButton, tabIndex: 1, gridRow: 1, gridColumn: 0);
        if (refreshButton != null)
            _inputManager.RegisterSelectable(refreshButton, tabIndex: 2, gridRow: 1, gridColumn: 1);
        if (backButton != null)
            _inputManager.RegisterSelectable(backButton, tabIndex: 3, gridRow: 1, gridColumn: 2);
        
        // Start with save games list selected
        _inputManager.SelectItem(0);
        SetupSaveGamesNavigation();
    }
    
    private void SetupSaveGamesNavigation()
    {
        if (_saveGamesList == null || _viewModel?.SavedGames == null) return;
        
        _saveGamesInputManager.ClearSelectables();
        _saveGamesInputManager.SetGridNavigation(true);
        
        // Wait for ListBox to generate its containers
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var containers = new System.Collections.Generic.List<Control>();
            
            for (int i = 0; i < _viewModel.SavedGames.Count; i++)
            {
                var container = _saveGamesList.ContainerFromIndex(i);
                if (container is ListBoxItem listBoxItem)
                {
                    var button = listBoxItem.FindLogicalDescendantOfType<Button>();
                    if (button != null)
                    {
                        containers.Add(button);
                    }
                }
            }
            
            for (int i = 0; i < containers.Count; i++)
            {
                var index = i;
                var control = containers[i];
                
                _saveGamesInputManager.RegisterSelectable(
                    control, 
                    tabIndex: i,
                    gridRow: i,
                    gridColumn: 0,
                    onSelected: item => 
                    {
                        // Update the ListBox selection to match the focused save game
                        if (index < _viewModel.SavedGames.Count)
                        {
                            _viewModel.SelectedSave = _viewModel.SavedGames[index];
                            _saveGamesList.SelectedIndex = index;
                        }
                    }
                );
            }
            
            // Select first save game if available
            if (containers.Count > 0 && _viewModel.SelectedSave != null)
            {
                var selectedIndex = _viewModel.SavedGames.IndexOf(_viewModel.SelectedSave);
                if (selectedIndex >= 0)
                {
                    _saveGamesInputManager.SelectItem(selectedIndex);
                }
                else
                {
                    _saveGamesInputManager.SelectItem(0);
                }
            }
        }, Avalonia.Threading.DispatcherPriority.Background);
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Check if focus is on a text input element
        if (IsTextInputFocused())
        {
            // Let text input handle the key naturally, only handle navigation keys
            if (e.Key == Key.Tab || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                _inputManager.HandleKeyInput(e);
            }
            return;
        }
        
        // Handle loading selected save game from main navigation
        if ((e.Key == Key.Enter || e.Key == Key.Space) && _saveGamesList?.IsFocused == true && _viewModel?.HasSaveGames == true)
        {
            Logger.Debug($"OnKeyDown: Enter/Space on SaveGamesList, SelectedSave={_viewModel?.SelectedSave?.GameName ?? "null"}, HasSaveGames={_viewModel?.HasSaveGames}");
            if (_viewModel?.SelectedSave != null)
            {
                Logger.Debug($"OnKeyDown: Executing PlaySaveCommand for {_viewModel.SelectedSave.GameName}");
                // Load the currently selected save game directly
                var observable = _viewModel.PlaySaveCommand.Execute(_viewModel.SelectedSave);
                observable.Subscribe(
                    onNext: _ => Logger.Debug($"OnKeyDown: PlaySaveCommand executed successfully"),
                    onError: ex => Logger.Debug($"OnKeyDown: PlaySaveCommand error: {ex.Message}")
                );
                e.Handled = true;
                return;
            }
        }
        
        _inputManager.HandleKeyInput(e);
    }
    
    private bool IsTextInputFocused()
    {
        var focusedElement = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        return focusedElement is TextBox || focusedElement is ComboBox;
    }
    
    public bool HandleGamepadInput(string input)
    {
        // Handle loading selected save game when A is pressed on the save games list
        if (input == "Confirm" && _saveGamesList?.IsFocused == true && _viewModel?.HasSaveGames == true)
        {
            Logger.Debug($"HandleGamepadInput: Confirm on SaveGamesList, SelectedSave={_viewModel?.SelectedSave?.GameName ?? "null"}");
            if (_viewModel?.SelectedSave != null)
            {
                Logger.Debug($"HandleGamepadInput: Executing PlaySaveCommand for {_viewModel.SelectedSave.GameName}");
                var observable = _viewModel.PlaySaveCommand.Execute(_viewModel.SelectedSave);
                observable.Subscribe(
                    onNext: _ => Logger.Debug($"HandleGamepadInput: PlaySaveCommand executed successfully"),
                    onError: ex => Logger.Debug($"HandleGamepadInput: PlaySaveCommand error: {ex.Message}")
                );
                return true;
            }
        }
        
        // Return false to let the Tab simulation system handle all other gamepad input
        return false;
    }
    
    private void SaveGameButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Logger.Debug($"SaveGameButton_Click: sender={sender?.GetType().Name}, hasTag={sender is Button b && b.Tag != null}, hasViewModel={_viewModel != null}");
        if (sender is Button button && button.Tag is SaveGameData saveData && _viewModel != null)
        {
            try
            {
                Logger.Debug($"SaveGameButton_Click: Executing PlaySaveCommand for {saveData.GameName}");
                
                // Try async execution pattern for ReactiveCommand with parameters
                var observable = _viewModel.PlaySaveCommand.Execute(saveData);
                observable.Subscribe(
                    onNext: _ => Logger.Debug($"SaveGameButton_Click: PlaySaveCommand executed successfully"),
                    onError: ex => Logger.Debug($"SaveGameButton_Click: PlaySaveCommand error: {ex.Message}"),
                    onCompleted: () => Logger.Debug($"SaveGameButton_Click: PlaySaveCommand completed")
                );
                
                Logger.Debug($"SaveGameButton_Click: PlaySaveCommand.Execute initiated");
            }
            catch (Exception ex)
            {
                Logger.Debug($"SaveGameButton_Click: Exception during PlaySaveCommand.Execute: {ex.Message}");
                Logger.Debug($"SaveGameButton_Click: Exception details: {ex}");
            }
        }
        else
        {
            Logger.Debug($"SaveGameButton_Click: Failed - button={sender is Button}, tag={sender is Button b2 && b2.Tag is SaveGameData}, viewModel={_viewModel != null}");
        }
    }
}
