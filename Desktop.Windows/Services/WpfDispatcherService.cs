using Immense.RemoteControl.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp = System.Windows.Application;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    public interface IWpfDispatcher
    {
        void Invoke(Action action);
        Task InvokeAsync(Action action);
        Task<Result<T>> InvokeAsync<T>(Func<T> func);
    }

    internal class WpfDispatcher : IWpfDispatcher
    {
        public void Invoke(Action action)
        {
            WpfApp.Current?.Dispatcher.Invoke(action);
        }

        public async Task InvokeAsync(Action action)
        {
            if (WpfApp.Current is null)
            {
                return;
            }

            await WpfApp.Current.Dispatcher.InvokeAsync(action);
        }

        public async Task<Result<T>> InvokeAsync<T>(Func<T> func)
        {
            if (WpfApp.Current is null)
            {
                return Result.Fail<T>("Application.Current is null.");
            }

            var result = await WpfApp.Current.Dispatcher.InvokeAsync(func);
            return Result.Ok(result);
        }
    }
}
