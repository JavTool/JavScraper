using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.Tasks;
using JavScraper.Tools.Services;

namespace JavScraper.Tools
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 配置 Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("Logs/app-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // 使用 Serilog 作为提供程序
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            var menuService = new MenuService(loggerFactory);
            menuService.PrintMenu();

            while (true)
            {
                Console.Write("选择你要执行的功能模块：");
                var command = Console.ReadLine();
                Console.WriteLine();

                await menuService.HandleCommand(command);
                menuService.PrintMenu();
            }
        }
    }
}
