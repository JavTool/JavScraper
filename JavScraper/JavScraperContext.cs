using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using JavScraper.Infrastructure;

namespace JavScraper
{
    public class JavScraperContext
    {
        #region Methods

        /// <summary>
        /// Create a static instance of the jav scraper context.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static IJavScraperApplication Create()
        {
            //create JavScraperContext as engine
            return Singleton<IJavScraperApplication>.Instance ?? (Singleton<IJavScraperApplication>.Instance = new JavScraperApplication());
        }

        /// <summary>
        /// Sets the static engine instance to the supplied engine. Use this method to supply your own engine implementation.
        /// </summary>
        /// <param name="engine">The engine to use.</param>
        /// <remarks>Only use this method if you know what you're doing.</remarks>
        public static void Replace(IJavScraperApplication engine)
        {
            Singleton<IJavScraperApplication>.Instance = engine;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the singleton jav scraper context used to access jav scraper services.
        /// </summary>
        public static IJavScraperApplication Current
        {
            get
            {
                if (Singleton<IJavScraperApplication>.Instance == null)
                {
                    Create();
                }

                return Singleton<IJavScraperApplication>.Instance;
            }
        }

        #endregion
    }
}
