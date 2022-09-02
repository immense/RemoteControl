using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Immense.RemoteControl.Desktop.UI.Controls;
using Immense.RemoteControl.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.UI.ViewModels.Fakes
{
    public class FakeBrandedViewModelBase : IBrandedViewModelBase
    {
        private readonly BrandingInfo _brandingInfo;
        private Bitmap? _icon;

        public FakeBrandedViewModelBase()
        {
            _brandingInfo = new BrandingInfo();
            _icon = GetBitmapImageIcon(_brandingInfo);

            TitleBackgroundColor = new SolidColorBrush(Color.FromRgb(
                _brandingInfo.TitleBackgroundRed,
                _brandingInfo.TitleBackgroundGreen,
                _brandingInfo.TitleBackgroundBlue));

            TitleForegroundColor = new SolidColorBrush(Color.FromRgb(
               _brandingInfo.TitleForegroundRed,
               _brandingInfo.TitleForegroundGreen,
               _brandingInfo.TitleForegroundBlue));

            TitleButtonForegroundColor = new SolidColorBrush(Color.FromRgb(
               _brandingInfo.ButtonForegroundRed,
               _brandingInfo.ButtonForegroundGreen,
               _brandingInfo.ButtonForegroundBlue));
        }
        public Bitmap? Icon
        {
            get => _icon;
            set => _icon = value;
        }
        public string? ProductName { get; set; } = "Test Product";
        public SolidColorBrush? TitleBackgroundColor { get; set; }
        public SolidColorBrush? TitleButtonForegroundColor { get; set; }
        public SolidColorBrush? TitleForegroundColor { get; set; }
        public WindowIcon? WindowIcon { get; set; }

        public Task ApplyBranding()
        {
            return Task.CompletedTask;
        }

        private Bitmap? GetBitmapImageIcon(BrandingInfo bi)
        {
            try
            {
                using var imageStream = typeof(Shared.Services.AppState)
                    .Assembly
                    .GetManifestResourceStream("Immense.RemoteControl.Desktop.Shared.Assets.DefaultIcon.png") ?? new MemoryStream();

                return new Bitmap(imageStream);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, "Design-Time Error", MessageBoxType.OK);
                return null;
            }
        }
    }
}
