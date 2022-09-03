using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels.Fakes
{
    public class FakeHostNamePromptViewModel : FakeBrandedViewModelBase, IHostNamePromptViewModel
    {
        public string Host { get; set; } = "https://localhost:7024";
    }
}
