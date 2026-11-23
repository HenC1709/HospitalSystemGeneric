using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace HospitalSystem.Desktop.Themes;

/// <summary>
/// Administra el cambio en caliente entre Modo Claro Clínico y Modo Oscuro Hospitalario,
/// sincronizando tanto WPF-UI como los pinceles dinámicos de la aplicación.
/// </summary>
public static class HospitalThemeManager
{
    public static void AplicarTema(bool modoOscuro)
    {
        var appTheme = modoOscuro ? ApplicationTheme.Dark : ApplicationTheme.Light;
        var accent = modoOscuro
            ? Color.FromRgb(56, 189, 248)   // Celeste clínico luminoso para fondo oscuro
            : Color.FromRgb(2, 132, 199);   // Azul médico para fondo claro

        ApplicationThemeManager.Apply(appTheme);
        ApplicationAccentColorManager.Apply(accent, appTheme);

        var res = System.Windows.Application.Current?.Resources;
        if (res is null)
            return;

        if (modoOscuro)
        {
            SetBrush(res, "HospitalBgBrush", 11, 21, 34);            // #0B1522 noche clínica profunda
            SetBrush(res, "HospitalSurfaceBrush", 18, 33, 51);       // #122133 superficie tarjeta
            SetBrush(res, "HospitalNavbarBrush", 15, 28, 44);        // #0F1C2C navbar oscuro
            SetBrush(res, "HospitalBorderBrush", 36, 62, 92);        // #243E5C borde sutil
            SetBrush(res, "HospitalSubPanelBrush", 22, 42, 64);      // #162A40 subpanel lab
            SetBrush(res, "HospitalSecondaryBtnBgBrush", 24, 44, 68);// #182C44 botón secundario
            SetBrush(res, "HospitalTabHoverBrush", 25, 48, 74);      // #19304A hover pestaña

            SetBrush(res, "HospitalPrimaryBrush", 14, 165, 233);     // #0EA5E9
            SetBrush(res, "HospitalPrimaryHoverBrush", 56, 189, 248);// #38BDF8
            SetBrush(res, "HospitalPrimarySoftBrush", 18, 56, 86);   // #123856

            SetBrush(res, "HospitalTealBrush", 45, 212, 191);        // #2DD4BF
            SetBrush(res, "HospitalTealHoverBrush", 94, 234, 212);   // #5EEAD4
            SetBrush(res, "HospitalTealSoftBrush", 17, 62, 62);      // #113E3E

            SetBrush(res, "HospitalSuccessBrush", 52, 211, 153);     // #34D399
            SetBrush(res, "HospitalSuccessSoftBrush", 14, 60, 47);   // #0E3C2F
            SetBrush(res, "HospitalWarningBrush", 251, 191, 36);     // #FBBF24
            SetBrush(res, "HospitalWarningSoftBrush", 69, 48, 14);   // #45300E
            SetBrush(res, "HospitalDangerBrush", 248, 113, 113);     // #F87171
            SetBrush(res, "HospitalDangerSoftBrush", 72, 24, 29);    // #48181D

            SetBrush(res, "HospitalTextPrimaryBrush", 232, 242, 252);// #E8F2FC
            SetBrush(res, "HospitalTextMutedBrush", 148, 176, 204);  // #94B0CC
        }
        else
        {
            SetBrush(res, "HospitalBgBrush", 240, 246, 250);         // #F0F6FA
            SetBrush(res, "HospitalSurfaceBrush", 255, 255, 255);    // #FFFFFF
            SetBrush(res, "HospitalNavbarBrush", 255, 255, 255);     // #FFFFFF
            SetBrush(res, "HospitalBorderBrush", 213, 229, 242);     // #D5E5F2
            SetBrush(res, "HospitalSubPanelBrush", 244, 250, 255);   // #F4FAFF
            SetBrush(res, "HospitalSecondaryBtnBgBrush", 248, 251, 254);
            SetBrush(res, "HospitalTabHoverBrush", 237, 246, 252);

            SetBrush(res, "HospitalPrimaryBrush", 2, 132, 199);      // #0284C7
            SetBrush(res, "HospitalPrimaryHoverBrush", 3, 105, 161); // #0369A1
            SetBrush(res, "HospitalPrimarySoftBrush", 224, 242, 254);// #E0F2FE

            SetBrush(res, "HospitalTealBrush", 13, 148, 136);        // #0D9488
            SetBrush(res, "HospitalTealHoverBrush", 15, 118, 110);   // #0F766E
            SetBrush(res, "HospitalTealSoftBrush", 204, 251, 241);   // #CCFBF1

            SetBrush(res, "HospitalSuccessBrush", 5, 150, 105);      // #059669
            SetBrush(res, "HospitalSuccessSoftBrush", 209, 250, 229);// #D1FAE5
            SetBrush(res, "HospitalWarningBrush", 217, 119, 6);      // #D97706
            SetBrush(res, "HospitalWarningSoftBrush", 254, 243, 199);// #FEF3C7
            SetBrush(res, "HospitalDangerBrush", 220, 38, 38);       // #DC2626
            SetBrush(res, "HospitalDangerSoftBrush", 254, 226, 226); // #FEE2E2

            SetBrush(res, "HospitalTextPrimaryBrush", 15, 41, 66);   // #0F2942
            SetBrush(res, "HospitalTextMutedBrush", 82, 109, 130);   // #526D82
        }
    }

    private static void SetBrush(System.Windows.ResourceDictionary res, string key, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        res[key] = brush;
    }
}
