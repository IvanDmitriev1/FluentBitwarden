using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.Shared;

public sealed partial class MyTitleBar : TitleBar
{
    private static readonly Thickness ContentMargin = new(128, 0, 0, 0);

    public MyTitleBar()
    {
        DefaultStyleKey = typeof(TitleBar);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_ContentPresenterGrid") is FrameworkElement contentPresenterGrid)
        {
            contentPresenterGrid.Margin = ContentMargin;
        }

        if (GetTemplateChild("PART_ContentPresenter") is ContentPresenter contentPresenter)
        {
            contentPresenter.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
    }
}
