﻿using System;
using System.Collections.Generic;
using System.Text;

namespace JavScraper.Infrastructure
{
    //public class Singleton<T> where T : class, new()
    //{
    //    private static T instance = null;

    //    private Singleton() { }

    //    public static T Instance
    //    {
    //        get
    //        {
    //            if (instance == null)
    //                instance = new T();
    //            return instance;
    //        }
    //    }
    //}

    /// <summary>
    /// A statically compiled "singleton" used to store objects throughout the 
    /// lifetime of the app domain. Not so much singleton in the pattern's 
    /// sense of the word as a standardized way to store single instances.
    /// </summary>
    /// <typeparam name="T">The type of object to store.</typeparam>
    /// <remarks>Access to the instance is not synchronized.</remarks>
    public class Singleton<T> where T : class
    {
        private static T instance;

        /// <summary>
        /// The singleton instance for the specified type T. Only one instance (at the time) of this object for each type of T.
        /// </summary>
        public static T Instance
        {
            get => instance;
            set
            {
                instance = value;
                AllSingletons[typeof(T)] = value;
            }
        }

        static Singleton()
        {
            AllSingletons = new Dictionary<Type, object>();
        }

        /// <summary>
        /// Dictionary of type to singleton instances.
        /// </summary>
        public static IDictionary<Type, object> AllSingletons { get; }
    }
    //public class Singleton<T> : BaseSingleton
    //{
    //    private static T instance;

    //    /// <summary>
    //    /// The singleton instance for the specified type T. Only one instance (at the time) of this object for each type of T.
    //    /// </summary>
    //    public static T Instance
    //    {
    //        get => instance;
    //        set
    //        {
    //            instance = value;
    //            AllSingletons[typeof(T)] = value;
    //        }
    //    }
    //}
}
