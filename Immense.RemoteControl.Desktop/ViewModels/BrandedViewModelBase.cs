using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Reactive;
using Immense.RemoteControl.Desktop.Services;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.ViewModels;

public interface IBrandedViewModelBase
{
    Bitmap? Icon { get; set; }
    string ProductName { get; set; }
    SolidColorBrush? TitleBackgroundColor { get; set; }
    SolidColorBrush? TitleButtonForegroundColor { get; set; }
    SolidColorBrush? TitleForegroundColor { get; set; }
    WindowIcon? WindowIcon { get; set; }

    Task ApplyBranding();
}

public class BrandedViewModelBase : ObservableObject, IBrandedViewModelBase
{
    private static BrandingInfoBase? _brandingInfo;
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
        
        if (_brandingInfo is not null)
        {
            ApplyBrandingImpl();
        }
        else
        {
            _ = Task.Run(ApplyBranding);
        }
    }

    public Bitmap? Icon
    {
        get => Get<Bitmap?>();
        set => Set(value);
    }

    public string ProductName
    {
        get => Get<string?>() ?? "Remote Control";
        set => Set(value ?? "Remote Control");
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

                ApplyBrandingImpl();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying branding.");
            }
        });
    }
    private void ApplyBrandingImpl()
    {
        _dispatcher.Invoke(() =>
        {
            try
            {
                _brandingInfo ??= new BrandingInfoBase();

                ProductName = _brandingInfo.Product;

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

                if (_brandingInfo.Icon?.Any() == true)
                {
                    using var imageStream = new MemoryStream(_brandingInfo.Icon);
                    Icon = new Bitmap(imageStream);
                }
                else
                {
                    using var imageStream = 
                        Assembly
                            .GetExecutingAssembly()
                            .GetManifestResourceStream("Immense.RemoteControl.Desktop.Shared.Assets.DefaultIcon.png") ?? new MemoryStream();

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
