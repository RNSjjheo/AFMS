using log4net;
using Microsoft.Extensions.Logging;

namespace AFMSDll
{
    /// <summary>
    /// Microsoft ILogger 로그를 AFMSLog가 구성한 log4net 로그에도 전달합니다.
    /// </summary>
    public sealed class AFMSLogLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            int separatorIndex = categoryName.LastIndexOf('.');
            string shortCategoryName = separatorIndex >= 0
                ? categoryName[(separatorIndex + 1)..]
                : categoryName;

            return new AFMSLogger(LogManager.GetLogger(shortCategoryName));
        }

        public void Dispose()
        {
        }

        private sealed class AFMSLogger(ILog log) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel switch
                {
                    LogLevel.Trace => log.IsDebugEnabled,
                    LogLevel.Debug => log.IsDebugEnabled,
                    LogLevel.Information => log.IsInfoEnabled,
                    LogLevel.Warning => log.IsWarnEnabled,
                    LogLevel.Error => log.IsErrorEnabled,
                    LogLevel.Critical => log.IsFatalEnabled,
                    _ => false
                };
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;

                string message = formatter(state, exception);

                switch (logLevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        log.Debug(message, exception);
                        break;
                    case LogLevel.Information:
                        log.Info(message, exception);
                        break;
                    case LogLevel.Warning:
                        log.Warn(message, exception);
                        break;
                    case LogLevel.Error:
                        log.Error(message, exception);
                        break;
                    case LogLevel.Critical:
                        log.Fatal(message, exception);
                        break;
                }
            }
        }
    }
}
