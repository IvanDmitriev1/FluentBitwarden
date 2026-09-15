using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.AttachedProperties;

[AttachedDependencyProperty<object>("BringToView")]
public static partial class VaultListSelectionBehavior
{
    static partial void OnBringToViewChanged(DependencyObject dependencyObject, object? newValue)
    {
        if (dependencyObject is not ListViewBase listViewBase || newValue is null)
            return;

        PerformScroll(listViewBase, newValue);
    }

    private static void PerformScroll(ListViewBase listView, object item)
    {
        if (listView.SelectedItem == item)
            return;

        listView.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            listView.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
        });
    }
}