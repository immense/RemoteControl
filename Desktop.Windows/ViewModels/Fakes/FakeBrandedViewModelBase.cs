using Immense.RemoteControl.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace Immense.RemoteControl.Desktop.Windows.ViewModels.Fakes
{
    public class FakeBrandedViewModelBase : IBrandedViewModelBase
    {
        private readonly BrandingInfo _brandingInfo;
        private BitmapImage? _icon;

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
        public BitmapImage? Icon
        {
            get => _icon;
            set => _icon = value;
        }
        public string? ProductName { get; set; } = "Test Product";
        public SolidColorBrush? TitleBackgroundColor { get; set; }
        public SolidColorBrush? TitleButtonForegroundColor { get; set; }
        public SolidColorBrush? TitleForegroundColor { get; set; }

        public Task ApplyBranding()
        {
            return Task.CompletedTask;
        }

        private BitmapImage GetBitmapImageIcon(BrandingInfo bi)
        {
            try
            {
                using var imageStream = typeof(Shared.Services.AppState)
                    .Assembly
                    .GetManifestResourceStream("Immense.RemoteControl.Desktop.Shared.Assets.DefaultIcon.png") ?? new MemoryStream();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = imageStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                imageStream.Close();

                return bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return new BitmapImage();
            }
        }
    }
}
