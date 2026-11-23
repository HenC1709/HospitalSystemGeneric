using System.Windows.Media.Animation;
using HospitalSystem.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace HospitalSystem.Desktop;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.VistaCambiada += AnimarTransicionVista;
        viewModel.FeedbackActualizado += AnimarBannerFeedback;

        Loaded += async (_, _) =>
        {
            AnimarTransicionVista();
            await viewModel.InicializarVistaAsync();
        };
    }

    private void AnimarTransicionVista()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var fade = new DoubleAnimation(0.25, 1.0, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = ease
        };
        var slide = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = ease
        };

        ActiveViewContainer.BeginAnimation(OpacityProperty, fade);
        ViewSlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
    }

    private void AnimarBannerFeedback()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var fade = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = ease
        };
        var slide = new DoubleAnimation(-8, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = ease
        };

        FeedbackBanner.BeginAnimation(OpacityProperty, fade);
        FeedbackSlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
    }
}
