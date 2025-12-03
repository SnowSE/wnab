namespace WNAB.Maui.Behaviors;

using Microsoft.Maui.Controls;

public class PickerForceRefreshBehavior : Behavior<Picker>
{
    protected override void OnAttachedTo(Picker picker)
    {
        picker.HandlerChanged += (_, __) =>
        {
            if (picker.BindingContext is EditableSplitItem split && split.SelectedCategory != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100); // let UI settle
                    picker.SelectedItem = null;
                    picker.SelectedItem = split.SelectedCategory;
                });
            }
        };
    }
}