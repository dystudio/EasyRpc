﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using EasyRpc.AspNetCore.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace EasyRpc.AspNetCore.Middleware
{
    /// <summary>
    /// Base class for expouse configurations
    /// </summary>
    public abstract class BaseExposureConfiguration
    {
        private readonly IInstanceActivator _activator;
        private readonly IArrayMethodInvokerBuilder _arrayMethodInvokerBuilder;

        /// <summary>
        /// Current api information
        /// </summary>
        protected readonly ICurrentApiInformation ApiInformation;

        /// <summary>
        /// Exposure names
        /// </summary>
        protected readonly List<string> Names = new List<string>();

        /// <summary>
        /// Authorizations
        /// </summary>
        protected readonly List<IMethodAuthorization> Authorizations = new List<IMethodAuthorization>();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="apiInformation"></param>
        /// <param name="activator"></param>
        /// <param name="arrayMethodInvokerBuilder"></param>
        protected BaseExposureConfiguration(ICurrentApiInformation apiInformation, IInstanceActivator activator, IArrayMethodInvokerBuilder arrayMethodInvokerBuilder)
        {
            ApiInformation = apiInformation;
            _activator = activator;
            _arrayMethodInvokerBuilder = arrayMethodInvokerBuilder;
        }

        /// <summary>
        /// Get exposed methods
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodFilter"></param>
        /// <param name="obsoleteMessage"></param>
        /// <returns></returns>
        protected IEnumerable<ExposedMethodInformation> GetExposedMethods(Type type,
            Func<MethodInfo, bool> methodFilter = null, string obsoleteMessage = null)
        {
            if (Names.Count == 0)
            {
                foreach (var routeName in ApiInformation.NamingConventions.RouteNameGenerator(type))
                {
                    Names.Add(routeName);
                }
            }

            return GetExposedMethods(type, ApiInformation, t => Names, Authorizations, methodFilter, _activator, _arrayMethodInvokerBuilder, obsoleteMessage);
        }

        /// <summary>
        /// Get all exposed methods from a type given a current state
        /// </summary>
        /// <param name="type"></param>
        /// <param name="currentApi"></param>
        /// <param name="namesFunc"></param>
        /// <param name="authorizations"></param>
        /// <param name="methodFilter"></param>
        /// <param name="activator"></param>
        /// <param name="invokerBuilder"></param>
        /// <param name="obsoleteMessage"></param>
        /// <returns></returns>
        public static IEnumerable<ExposedMethodInformation> GetExposedMethods(Type type,
            ICurrentApiInformation currentApi,
            Func<Type, IEnumerable<string>> namesFunc,
            List<IMethodAuthorization> authorizations,
            Func<MethodInfo, bool> methodFilter,
            IInstanceActivator activator,
            IArrayMethodInvokerBuilder invokerBuilder,
            string obsoleteMessage)
        {
            var names = namesFunc(type).ToList();

            List<string> finalNames;

            if (currentApi.Prefixes.Count > 0)
            {
                var newNames = new List<string>();

                foreach (var prefixes in currentApi.Prefixes)
                {
                    foreach (var prefix in prefixes(type))
                    {
                        foreach (var name in names)
                        {
                            newNames.Add(prefix + name);
                        }
                    }
                }

                finalNames = newNames;
            }
            else
            {
                finalNames = names;
            }

            foreach (var authorization in currentApi.Authorizations)
            {
                foreach (var methodAuthorization in authorization(type))
                {
                    authorizations.Add(methodAuthorization);
                }
            }

            foreach (var method in type.GetRuntimeMethods())
            {
                if (method.IsStatic ||
                    !method.IsPublic ||
                    method.DeclaringType == typeof(object))
                {
                    continue;
                }

                var filterOut = currentApi.MethodFilters.Any(func => !func(method));

                if (filterOut)
                {
                    continue;
                }

                if (methodFilter != null && !methodFilter(method))
                {
                    continue;
                }

                var filters = new List<Func<ICallExecutionContext, IEnumerable<ICallFilter>>>();

                foreach (var func in currentApi.Filters)
                {
                    var filter = func(method);

                    if (filter != null)
                    {
                        filters.Add(filter);
                    }
                }

                var currentAuth = new List<IMethodAuthorization>(authorizations);
                var obsolete = ProcessAttributesOnMethod(method, currentAuth, filters);

                obsoleteMessage = obsoleteMessage ?? obsolete;

                var authArray = currentAuth.Count > 0 ? currentAuth.ToArray() : Array.Empty<IMethodAuthorization>();

                var filterArray = filters.Count > 0
                    ? filters.ToArray()
                    : Array.Empty<Func<ICallExecutionContext, IEnumerable<ICallFilter>>>();

                yield return new ExposedMethodInformation(type,
                    finalNames,
                    currentApi.NamingConventions.MethodNameGenerator(method),
                    method,
                    authArray,
                    filterArray,
                    activator,
                    invokerBuilder,
                    currentApi.SupportResponseCompression,
                    obsoleteMessage);
            }
        }

        /// <summary>
        /// Process the attributes on a method/class/assembly
        /// </summary>
        /// <param name="method"></param>
        /// <param name="currentAuth"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        public static string ProcessAttributesOnMethod(MethodInfo method, List<IMethodAuthorization> currentAuth, List<Func<ICallExecutionContext, IEnumerable<ICallFilter>>> filters)
        {
            var obsoleteMethod = ProcessAttributeArray(method.GetCustomAttributes(true).Cast<Attribute>(), currentAuth, filters);
            
            var obsoleteClass = ProcessAttributeArray(method.DeclaringType.GetTypeInfo().GetCustomAttributes(true).Cast<Attribute>(), currentAuth, filters);
            
            var obsoleteAssembly =ProcessAttributeArray(method.DeclaringType.GetTypeInfo().Assembly.GetCustomAttributes(), currentAuth, filters);

            return obsoleteMethod ?? obsoleteClass ?? obsoleteAssembly;
        }

        private static string ProcessAttributeArray(IEnumerable<Attribute> methodAttributes, List<IMethodAuthorization> currentAuth,  List<Func<ICallExecutionContext, IEnumerable<ICallFilter>>> filters)
        {
            string obsolete = null;

            foreach (var attribute in methodAttributes)
            {
                if (attribute is IAuthorizeData authorizeData)
                {
                    if (!string.IsNullOrEmpty(authorizeData.Policy))
                    {
                        currentAuth.Add(new UserPolicyAuthorization(authorizeData.Policy));
                    }
                    else if (!string.IsNullOrEmpty(authorizeData.Roles))
                    {
                        currentAuth.Add(new UserRoleAuthorization(authorizeData.Roles));
                    }
                    else
                    {
                        currentAuth.Add(new UserAuthenticatedAuthorization());
                    }
                }
                else if (attribute is ObsoleteAttribute obsoleteAttribute)
                {
                    obsolete = obsoleteAttribute.Message ?? "This method is obsolete";
                }
                else if (attribute is IRpcFilterProviderAttribute filterAttribute && filterAttribute.Filter != null)
                {
                    filters.Add(filterAttribute.Filter);
                }
            }

            return obsolete;
        }
    }
}
