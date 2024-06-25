using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JavScraper.Test
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            //var logger = loggerFactory.CreateLogger();
            services.AddLogging();

            //services.AddSingleton(logger);

            //logger.LogInformation("Carregando configurações...");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json");
            var configuration = builder.Build();

            services.AddSingleton<IConfiguration>(configuration);

            //services.AddSingleton<AcoesRepository>();

            services.AddTransient<JavScraperApplication>();
        }
    }
    //public class Startup
    //{


    //    public Startup(IConfiguration configuration, IHostEnvironment env)
    //    {
    //        Configuration = configuration;
    //        _env = env;
    //    }

    //    public IConfiguration Configuration { get; }
    //    private readonly IHostEnvironment _env;

    //    public void ConfigureServices(IServiceCollection services)
    //    {
    //        if (_env.IsDevelopment())
    //        {
    //            Console.WriteLine(_env.EnvironmentName);
    //        }
    //        else if (_env.IsStaging())
    //        {
    //            Console.WriteLine(_env.EnvironmentName);
    //        }
    //        else
    //        {
    //            Console.WriteLine("Not dev or staging");
    //        }
    //    }

    //    //public void Configure(IAppBuilder app)
    //    //{
    //    //    if (_env.IsDevelopment())
    //    //    {
    //    //        app.UseDeveloperExceptionPage();
    //    //    }
    //    //    else
    //    //    {
    //    //        app.UseExceptionHandler("/Error");
    //    //        app.UseHsts();
    //    //    }

    //    //    app.UseHttpsRedirection();
    //    //    app.UseStaticFiles();

    //    //    app.UseRouting();

    //    //    app.UseAuthorization();

    //    //    app.UseEndpoints(endpoints =>
    //    //    {
    //    //        endpoints.MapRazorPages();
    //    //    });
    //    //}
    //}
}
