using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM.Behaviors;

// LLM-Dev:v2 Behavior to invoke a command once when a Page appears.
// Keeps page code-behind empty while providing a reliable lifecycle hook.
public class OnAppearingOnceBehavior : Behavior<Page>
{
    private Page? _page;
    private bool _fired;

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(OnAppearingOnceBehavior));

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(OnAppearingOnceBehavior));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnAttachedTo(Page bindable)
    {
        base.OnAttachedTo(bindable);
        _page = bindable;
        // Ensure behavior BindingContext mirrors the page BindingContext so {Binding ...} works
        BindingContext = bindable.BindingContext;
        bindable.BindingContextChanged += OnBindingContextChanged;
        bindable.Appearing += OnAppearingAsync;
    }

    protected override void OnDetachingFrom(Page bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.BindingContextChanged -= OnBindingContextChanged;
        bindable.Appearing -= OnAppearingAsync;
        _page = null;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (_page is not null)
            BindingContext = _page.BindingContext;
    }

    private async void OnAppearingAsync(object? sender, EventArgs e)
    {
        if (_fired) return;
        _fired = true;

        var cmd = Command;
        var param = CommandParameter;
        if (cmd is IAsyncRelayCommand asyncCmd)
        {
            await asyncCmd.ExecuteAsync(param);
        }
        else if (cmd?.CanExecute(param) == true)
        {
            cmd.Execute(param);
        }
    }
}