﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyRpc.AspNetCore
{
    public interface IExposureConfiguration
    {
        IExposureConfiguration As(string name);

        IExposureConfiguration Authorize(string role = null, string policy = null);
    }

    public interface IExposureConfiguration<T>
    {
        IExposureConfiguration<T> As(string name);

        IExposureConfiguration<T> Authorize(string role = null, string policy = null);
    }
}