using Autofac;
using JavScraper.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace JavScraper.Scrapers
{
    public interface IJavScraper
    {

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        void Configure(IHostBuilder application);

        void Register(ContainerBuilder containerBuilder, ITypeFinder typeFinder);
    }
}
