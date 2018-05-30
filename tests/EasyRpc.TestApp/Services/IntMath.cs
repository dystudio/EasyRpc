﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grace.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EasyRpc.TestApp.Services
{
    /// <summary>
    /// Integer math service
    /// </summary>
    public class IntMath
    {
        private IExportLocatorScope _scope;

        public IntMath(IExportLocatorScope scope, IEnumerable<ILoggerProvider> providers)
        {
            Console.WriteLine("Count: " + providers.Count());
            _scope = scope;
        }

        /// <summary>
        /// Add two integers together
        /// </summary>
        /// <param name="x">first integer value</param>
        /// <param name="y">second integer value</param>
        /// <returns>x + y</returns>
        public int Add(int x, int y)
        {
            return x + y;
        }

        /// <summary>
        /// Subtract two integers
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Subtract(int x, int y)
        {
            return x - y;
        }

        /// <summary>
        /// Muliply two integers together
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Multiply(int x, int y)
        {
            return x * y;
        }
    }
}