using Immense.RemoteControl.Shared.Primitives;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Shared.Extensions;

public static class ILoggerExtensions
{
    public static IDisposable Enter<T>(
        this ILogger<T> logger, 
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName]string memberName = "")
    {
        logger.Log(logLevel, "Enter: {name}", memberName);

        return new CallbackDisposable(() =>
        {
            logger.Log(logLevel, "Exit: {name}", memberName);
        });
    }
}
