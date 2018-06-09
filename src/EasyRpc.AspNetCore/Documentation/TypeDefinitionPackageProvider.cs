﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EasyRpc.AspNetCore.Messages;
using EasyRpc.AspNetCore.Middleware;

namespace EasyRpc.AspNetCore.Documentation
{
    public interface ITypeDefinitionPackageProvider
    {
        IEnumerable<TypeDefinition> GetTypeDefinitions(List<JsonDataPackage> packages);

    }

    public class TypeDefinitionPackageProvider : ITypeDefinitionPackageProvider
    {
        public IEnumerable<TypeDefinition> GetTypeDefinitions(List<JsonDataPackage> exposedMethods)
        {
            var typeDefinitions = new Dictionary<Type, TypeDefinition>();

            foreach (var dataPackage in exposedMethods)
            {
                foreach (var exposedMethod in dataPackage.Methods)
                {
                    var methodInfo = exposedMethod.Method.MethodInfo;

                    GenerateTypeDefinitionFor(typeDefinitions, methodInfo.ReturnType);

                    foreach (var parameterInfo in methodInfo.GetParameters())
                    {
                        GenerateTypeDefinitionFor(typeDefinitions, parameterInfo.ParameterType);
                    }
                }
            }

            return typeDefinitions.Values;
        }

        private void GenerateTypeDefinitionFor(Dictionary<Type, TypeDefinition> typeDefinitions, Type type)
        {
            var typeInfo = type.GetTypeInfo();

            if (type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(void) ||
                typeInfo.IsPrimitive)
            {
                return;
            }

            if (typeDefinitions.ContainsKey(type))
            {
                return;
            }

            if (typeInfo.IsEnum)
            {
                var enumValues = new List<EnumValueDefinition>();

                foreach (var fieldInfo in type.GetFields())
                {
                    enumValues.Add(new EnumValueDefinition
                    {
                        Name = fieldInfo.Name,
                        Value = fieldInfo.GetRawConstantValue()
                    });
                }

                var enumDefinition = new TypeDefinition
                {
                    Name = type.Name,
                    FullName = type.FullName,
                    EnumValues = enumValues
                };

                typeDefinitions[type] = enumDefinition;

                return;
            }

            var enumerableInterface = typeInfo.ImplementedInterfaces.FirstOrDefault(t =>
                t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface != null)
            {
                GenerateTypeDefinitionFor(typeDefinitions, enumerableInterface.GenericTypeArguments[0]);
                return;
            }
            
            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();

                if (genericType == typeof(IEnumerable<>))
                {
                    GenerateTypeDefinitionFor(typeDefinitions, typeInfo.GenericTypeArguments[0]);
                    return;
                }

                if (genericType == typeof(Nullable<>))
                {
                    GenerateTypeDefinitionFor(typeDefinitions,typeInfo.GenericTypeArguments[0]);
                    return;
                }

                if (genericType == typeof(Task<>))
                {
                    GenerateTypeDefinitionFor(typeDefinitions, typeInfo.GenericTypeArguments[0]);
                    return;
                }

                if (genericType == typeof(ResponseMessage<>))
                {
                    GenerateTypeDefinitionFor(typeDefinitions, typeInfo.GenericTypeArguments[0]);
                    return;
                }
            }
            
            var properties = new List<PropertyDefinition>();

            var typeDefinition = new TypeDefinition
            {
                Name = type.Name,
                FullName = type.FullName,
                Properties = properties,
                Type = type
            };

            typeDefinitions[type] = typeDefinition;

            foreach (var property in typeInfo.DeclaredProperties)
            {
                if (property.CanRead &&
                    property.GetMethod.IsPublic &&
                    !property.GetMethod.IsStatic)
                {
                    var propertyDef = new PropertyDefinition
                    {
                        Name = property.Name,
                        PropertyType = TypeUtilities.CreateTypeRef(property.PropertyType),
                        Required = property.GetCustomAttributes<Attribute>().Any(a => a.GetType().Name == "RequiredAttribute")
                    };

                    properties.Add(propertyDef);

                    GenerateTypeDefinitionFor(typeDefinitions, property.PropertyType);
                }
            }
        }
    }
}

