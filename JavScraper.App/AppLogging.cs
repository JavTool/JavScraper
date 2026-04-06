using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace JavScraper.App
{
    /// <summary>
    /// 全局日志工厂的静态访问点，允许应用内各处共享 ILoggerFactory/ILogger。
    /// </summary>
    public static class AppLogging
    {
        private static ILoggerFactory _factory;

        /// <summary>
        /// 全局可访问的 ILoggerFactory 实例。
        /// </summary>
        public static ILoggerFactory Factory => _factory ?? throw new InvalidOperationException("AppLogging 未初始化，请在 Program.Main 中调用 AppLogging.Initialize().");

        /// <summary>
        /// 初始化默认的日志工厂（Console + Debug）。
        /// </summary>
        public static void Initialize()
        {
            if (_factory != null)
                return;

            _factory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                });
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
        }

        /// <summary>
        /// Dispose the logger factory when application is shutting down.
        /// </summary>
        public static void Shutdown()
        {
            _factory?.Dispose();
            _factory = null;
        }
    }
}
