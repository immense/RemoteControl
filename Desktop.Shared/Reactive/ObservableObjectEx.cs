using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Reactive
{
    public class ObservableObjectEx : ObservableObject
    {
        private readonly ConcurrentDictionary<string, object?> _backingFields = new();

        protected void Set<T>(T newValue, [CallerMemberName] string propertyName = "")
        {
            _backingFields.AddOrUpdate(propertyName, newValue, (k, v) => newValue);
            OnPropertyChanged(propertyName);
        }

        protected T? Get<T>([CallerMemberName] string propertyName = "", T? defaultValue = default)
        {
            if (_backingFields.TryGetValue(propertyName, out var value) &&
                value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }
    }
}
