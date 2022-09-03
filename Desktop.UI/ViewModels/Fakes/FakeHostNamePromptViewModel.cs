using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Immense.RemoteControl.Desktop.UI.ViewModels.Fakes
{
    public class FakeHostNamePromptViewModel : FakeBrandedViewModelBase, IHostNamePromptViewModel
    {
        public string Host { get; set; } = "https://localhost:7024";

        public ICommand OKCommand => new RelayCommand(() => { });
    }
}
