using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace JavScraper
{
    public class ServiceRegister
    {


        public static void Register(IServiceCollection services)
        {
            services.AddLogging();

        }
    }
}
