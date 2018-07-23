﻿using System.Collections.Generic;
using System.Threading.Tasks;
using EasyRpc.AspNetCore.Messages;
using Microsoft.AspNetCore.Http;

namespace EasyRpc.AspNetCore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="parameterValues"></param>
    /// <param name="version"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public delegate Task<ResponseMessage> InvokeMethodWithArray(object instance, object[] parameterValues, string version, string id);

}
