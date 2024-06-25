using Autofac;
using Autofac.Extensions.DependencyInjection;
using JavScraper.Database;
using JavScraper.Infrastructure;
using JavScraper.Scrapers;
using JavScraper.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JavScraper
{
    /// <summary>
    /// JavScraper 应用程序上下文。
    /// </summary>
    public class JavScraperApplication : IJavScraperApplication
    {
        private readonly ILogger<JavScraperApplication> _logger;
        //private readonly IHostApplicationLifetime hostApplicationLifetime;
        //private readonly IHostBuilder hostBuilder;
        //private readonly IHostEnvironment hostEnvironment;
        //private readonly IServiceProvider serviceProvider;
        //private readonly IServiceCollection services;
        //private readonly IHost host;


        #region Properties

        /// <summary>
        /// Gets or sets service provider
        /// </summary>
        private IServiceProvider _serviceProvider { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        public const string NAME = "JavScraper";

        /// <summary>
        /// 唯一标识。
        /// </summary>
        public Guid Id => new Guid("3E43DF2B-7A7A-4241-930D-E9A30437ECA8");

        /// <summary>
        /// 数据库。
        /// </summary>
        public JavScraperDbContext db { get; }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name => NAME;

        /// <summary>
        /// 描述。
        /// </summary>
        public string Description => "Jav Scraper";


        /// <summary>
        /// JavScraper 应用程序上下文实例。
        /// </summary>
        public static JavScraperApplication Instance { get; private set; }

        /// <summary>
        /// 全部的刮削器。
        /// </summary>
        public IList<AbstractScraper> Scrapers { get; private set; }

        /// <summary>
        /// 图片服务。
        /// </summary>
        public ImageProxyService ImageProxyService { get; }

        /// <summary>
        /// 翻译服务。
        /// </summary>
        public TranslationService TranslationService { get; }

        /// <summary>
        /// 缩略图格式。
        /// </summary>
        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        #endregion

        #region Ctor...

        /// <summary>
        /// 初始化 <seealso cref="JavScraperApplication"/> 类的新实例。
        /// </summary>
        public JavScraperApplication()
        {
            //Instance = this;
            //_logger= 
            _logger?.LogInformation($"{Name} - Loaded.");
            //db = JavScraperDbContext.Create(hostEnvironment);
        }

        ///// <summary>
        ///// 初始化 <seealso cref="JavScraperApplication"/> 类的新实例。
        ///// </summary>
        ///// <param name="logManager"></param>
        ///// <param name="host"></param>
        ///// <param name="services"></param>
        ///// <param name="hostEnvironment"></param>
        ///// <param name="hostApplicationLifetime"></param>
        //public JavScraperApplication(ILoggerFactory logManager,
        //    //ILogger<JavScraperApplication> logger,
        //    IHost host,
        //    //IHostBuilder hostBuilder,
        //    IServiceCollection services,
        //    IHostEnvironment hostEnvironment,
        //    //IServiceProvider serviceProvider,
        //    IHostApplicationLifetime hostApplicationLifetime)
        //{
        //    _logger = logManager.CreateLogger<JavScraperApplication>();
        //    this.host = host;
        //    //this.hostBuilder = hostBuilder;
        //    this.hostEnvironment = hostEnvironment;
        //    //this.serviceProvider = serviceProvider;
        //    this.hostApplicationLifetime = hostApplicationLifetime;
        //    this.services = services;
        //    Instance = this;
        //    _logger?.LogInformation($"{Name} - Loaded.");
        //    db = JavScraperDbContext.Create(hostEnvironment);

        //    //var types = Container.GetTypesToRegister(typeof(BaseScraper), new[] { typeof(BaseScraper).Assembly });

        //    //Scrapers = GetExports<BaseScraper>(false).Where(o => o != null).ToList().AsReadOnly();
        //}


        #endregion

        #region Properties...

        /// <summary>
        /// Service provider
        /// </summary>
        public virtual IServiceProvider ServiceProvider => _serviceProvider;

        #endregion

        #region Methods...

        /// <summary>
        /// Add and configure services.
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        /// <returns>Service provider</returns>
        public IServiceProvider ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //find startup configurations provided by other assemblies
            var typeFinder = new AppDomainTypeFinder();

            var scrapers = typeFinder.FindClassesOfType<AbstractScraper>();

            ////create and sort instances of startup configurations
            //var instances = scrapers
            //    .Select(startup => (IJavScraper)Activator.CreateInstance(startup));

            ////configure services
            //foreach (var instance in instances)
            //    instance.ConfigureServices(services, configuration);

            //register dependencies
            RegisterDependencies(services, typeFinder);

            Scrapers = ResolveAll<AbstractScraper>().ToList();

            //resolve assemblies here. otherwise, plugins can throw an exception when rendering views
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            return _serviceProvider;
        }


        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //check for assembly already loaded
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            //get assembly from TypeFinder
            var tf = Resolve<ITypeFinder>();
            assembly = tf.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            return assembly;
        }


        /// <summary>
        /// Resolve dependency.
        /// </summary>
        /// <typeparam name="T">Type of resolved service</typeparam>
        /// <returns>Resolved service</returns>
        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }
        /// <summary>
        /// Resolve dependency.
        /// </summary>
        /// <param name="type">Type of resolved service</param>
        /// <returns>Resolved service</returns>
        public object Resolve(Type type)
        {
            return ServiceProvider.GetService(type);
        }

        /// <summary>
        /// Resolve dependencies.
        /// </summary>
        /// <typeparam name="T">Type of resolved services</typeparam>
        /// <returns>Collection of resolved services</returns>
        public virtual IEnumerable<T> ResolveAll<T>()
        {
            return (IEnumerable<T>)GetServiceProvider().GetServices(typeof(T));
        }

        /// <summary>
        /// Register dependencies.
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="typeFinder">Type finder</param>
        protected virtual IServiceProvider RegisterDependencies(IServiceCollection services, ITypeFinder typeFinder)
        {
            var containerBuilder = new ContainerBuilder();

            //register engine
            containerBuilder.RegisterInstance(this).As<IJavScraperApplication>().SingleInstance();

            //register type finder
            containerBuilder.RegisterInstance(typeFinder).As<ITypeFinder>().SingleInstance();

            //populate Autofac container builder with the set of registered service descriptors
            containerBuilder.Populate(services);

            ////find dependency registrars provided by other assemblies
            //var javScrapes = typeFinder.FindClassesOfType<IJavScraper>();

            ////create and sort instances of dependency registrars
            //var scrapers = javScrapes
            //    .Select(scraper => (IJavScraper)Activator.CreateInstance(scraper));

            ////register all provided dependencies
            //foreach (var scraper in scrapers)
            //    scraper.Register(containerBuilder, typeFinder);

            //find dependency registrars provided by other assemblies
            var dependencyRegistrars = typeFinder.FindClassesOfType<IDependencyRegistrar>();

            //create and sort instances of dependency registrars
            var instances = dependencyRegistrars
                .Select(dependencyRegistrar => (IDependencyRegistrar)Activator.CreateInstance(dependencyRegistrar));

            //register all provided dependencies
            foreach (var dependencyRegistrar in instances)
                dependencyRegistrar.Register(containerBuilder, typeFinder);

            //create service provider
            _serviceProvider = new AutofacServiceProvider(containerBuilder.Build());

            return _serviceProvider;
        }


        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream($"{type.Namespace}.thumb.png");
        }

        ///// <summary>
        ///// Configure HTTP request pipeline
        ///// </summary>
        ///// <param name="application">Builder for configuring an application's request pipeline</param>
        //public void ConfigureRequestPipeline(IHostBuilder application)
        //{
        //    //find startup configurations provided by other assemblies
        //    var typeFinder = Resolve<ITypeFinder>();
        //    var startupConfigurations = typeFinder.FindClassesOfType<IJavScraper>();

        //    //create and sort instances of startup configurations
        //    var instances = startupConfigurations
        //        .Select(startup => (IJavScraper)Activator.CreateInstance(startup))
        //        .OrderBy(startup => startup.Order);

        //    //configure request pipeline
        //    foreach (var instance in instances)
        //        instance.Configure(application);
        //}


        /// <summary>
        /// Get IServiceProvider.
        /// </summary>
        /// <returns>IServiceProvider</returns>
        protected IServiceProvider GetServiceProvider()
        {
            return ServiceProvider;
        }
        /// <summary>
        /// Resolve unregistered service.
        /// </summary>
        /// <param name="type">Type of service</param>
        /// <returns>Resolved service</returns>
        public virtual object ResolveUnregistered(Type type)
        {
            Exception innerException = null;
            foreach (var constructor in type.GetConstructors())
            {
                try
                {
                    //try to resolve constructor parameters
                    var parameters = constructor.GetParameters().Select(parameter =>
                    {
                        var service = Resolve(parameter.ParameterType);
                        if (service == null)
                            throw new Exception("Unknown dependency");
                        return service;
                    });

                    //all is ok, so create instance
                    return Activator.CreateInstance(type, parameters.ToArray());
                }
                catch (Exception ex)
                {
                    innerException = ex;
                }
            }

            throw new Exception("No constructor was found that had all the dependencies satisfied.", innerException);
        }


        #endregion
    }
}
