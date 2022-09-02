using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Reactive;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.UI.ViewModels
{
    public class BrandedViewModelBase : ObservableObjectEx
    {
        private static BrandingInfo? _brandingInfo;
        private readonly IBrandingProvider _brandingProvider;
        private readonly ILogger<BrandedViewModelBase> _logger;
        private readonly IAvaloniaDispatcher _dispatcher;


        public BrandedViewModelBase(
            IBrandingProvider brandingProvider,
            IAvaloniaDispatcher dispatcher,
            ILogger<BrandedViewModelBase> logger)
        {

            _brandingProvider = brandingProvider;
            _dispatcher = dispatcher;
            _logger = logger;
            _ = Task.Run(ApplyBranding);
        }

        public Bitmap? Icon
        {
            get => Get<Bitmap?>();
            set => Set(value);
        }

        public string? ProductName
        {
            get => Get<string?>();
            set => Set(value);
        }
        public SolidColorBrush? TitleBackgroundColor
        {
            get => Get<SolidColorBrush?>();
            set => Set(value);
        }

        public SolidColorBrush? TitleButtonForegroundColor
        {
            get => Get<SolidColorBrush?>();
            set => Set(value);
        }

        public SolidColorBrush? TitleForegroundColor
        {
            get => Get<SolidColorBrush?>();
            set => Set(value);
        }

        public WindowIcon? WindowIcon { get; set; }


        public async Task ApplyBranding()
        {
            await _dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    _brandingInfo ??= await _brandingProvider.GetBrandingInfo();

                    ProductName = "Remote Control";

                    if (!string.IsNullOrWhiteSpace(_brandingInfo?.Product))
                    {
                        ProductName = _brandingInfo.Product;
                    }

                    TitleBackgroundColor = new SolidColorBrush(Color.FromRgb(
                        _brandingInfo?.TitleBackgroundRed ?? 70,
                        _brandingInfo?.TitleBackgroundGreen ?? 70,
                        _brandingInfo?.TitleBackgroundBlue ?? 70));

                    TitleForegroundColor = new SolidColorBrush(Color.FromRgb(
                       _brandingInfo?.TitleForegroundRed ?? 29,
                       _brandingInfo?.TitleForegroundGreen ?? 144,
                       _brandingInfo?.TitleForegroundBlue ?? 241));

                    TitleButtonForegroundColor = new SolidColorBrush(Color.FromRgb(
                       _brandingInfo?.ButtonForegroundRed ?? 255,
                       _brandingInfo?.ButtonForegroundGreen ?? 255,
                       _brandingInfo?.ButtonForegroundBlue ?? 255));

                    if (_brandingInfo?.Icon?.Any() == true)
                    {
                        using var imageStream = new MemoryStream(_brandingInfo.Icon);
                        Icon = new Bitmap(imageStream);
                    }
                    else
                    {
                        using var imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Remotely.Desktop.XPlat.Assets.Remotely_Icon.png");
                        Icon = new Bitmap(imageStream);
                    }

                    WindowIcon = new WindowIcon(Icon);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying branding.");
                }
            });
        }
    }
}
