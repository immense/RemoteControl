using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.ViewModels
{
    [ObservableObject]
    public partial class FileUpload
    {
        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private double _percentProgress;

        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public string DisplayName => Path.GetFileName(_filePath);
    }
}
