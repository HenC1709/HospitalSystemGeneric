using System.Windows.Controls;
using System.Windows.Media;
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
        viewModel.DialogoExportacionAbierto += AnimarDialogoExportacion;

        Loaded += async (_, _) =>
        {
            AnimarTransicionVista();
            await viewModel.InicializarVistaAsync();
        };
    }

    private void AnimarTransicionVista()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var fade = new DoubleAnimation(0.25, 1.0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };
        var slide = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };

        ActiveViewContainer.BeginAnimation(OpacityProperty, fade);
        ViewSlideTransform.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private void AnimarBannerFeedback()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var fade = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease };
        var slide = new DoubleAnimation(-8, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease };

        FeedbackBanner.BeginAnimation(OpacityProperty, fade);
        FeedbackSlideTransform.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private void AnimarDialogoExportacion()
    {
        var backdropFade = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180));
        ExportModalBackdrop.BeginAnimation(OpacityProperty, backdropFade);

        var cardEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.32 };
        var cardScaleAnim = new DoubleAnimation(0.84, 1.0, TimeSpan.FromMilliseconds(280)) { EasingFunction = cardEase };
        ExportCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, cardScaleAnim);
        ExportCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, cardScaleAnim);

        var checkEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.65 };
        var checkScaleAnim = new DoubleAnimation(0.3, 1.0, TimeSpan.FromMilliseconds(420)) { EasingFunction = checkEase };
        ExportCheckScale.BeginAnimation(ScaleTransform.ScaleXProperty, checkScaleAnim);
        ExportCheckScale.BeginAnimation(ScaleTransform.ScaleYProperty, checkScaleAnim);

        var progressAnim = new DoubleAnimation(0, 100, TimeSpan.FromMilliseconds(440))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ExportProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, progressAnim);
    }
}
