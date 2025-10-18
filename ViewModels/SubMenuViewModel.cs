using ReactiveUI;
using System.Reactive;

namespace FullCrisis3.ViewModels;

public class SubMenuViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _contentText = string.Empty;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string ContentText
    {
        get => _contentText;
        set => this.RaiseAndSetIfChanged(ref _contentText, value);
    }

    public ReactiveCommand<Unit, Unit>? BackCommand { get; set; }
}