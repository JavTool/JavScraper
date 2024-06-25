using Autofac;
using JavScraper.Scrapers;
using System;
using System.Collections.Generic;
using System.Text;

namespace JavScraper.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>

        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder)
        {
            //file provider
            builder.RegisterType<JavFileProvider>().As<IJavFileProvider>().InstancePerLifetimeScope();

            //builder.RegisterGeneric(typeof(BaseScraper)).As(typeof(IJavScraper)).InstancePerLifetimeScope();

            //builder.RegisterType<ArzonScraper>().As<BaseScraper>().InstancePerLifetimeScope();
            //builder.RegisterType<AVSOXScraper>().As<BaseScraper>().InstancePerLifetimeScope();
            //builder.RegisterType<FC2Scraper>().As<BaseScraper>().InstancePerLifetimeScope();
            //builder.RegisterType<JavBusScraper>().As<BaseScraper>().InstancePerLifetimeScope();
            //builder.RegisterType<JavDBScraper>().As<BaseScraper>().InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(typeof(AbstractScraper).Assembly).As<AbstractScraper>().AsImplementedInterfaces().InstancePerLifetimeScope();

            // scrapers
            //var javScrapes = typeFinder.FindClassesOfType<BaseScraper>();
            //foreach (var item in javScrapes)
            //{


            //    builder.RegisterType(item.GetType()).As<BaseScraper>().InstancePerLifetimeScope();
            //}

        }

    }
}
