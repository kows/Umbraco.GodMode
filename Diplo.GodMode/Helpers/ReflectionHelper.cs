﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Diplo.GodMode.Models;
using Umbraco.Core;

namespace Diplo.GodMode.Helpers
{
    /// <summary>
    /// Helper for dealing with that nasty reflection stuff
    /// </summary>
    public static class ReflectionHelper
    {
        public static readonly Func<Assembly, bool> IsUmbracoAssemblyPredicate = a => a.ManifestModule.Name.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase);

        public static readonly Func<Type, Type, bool> IsAssignableClassFromPredicate = (a, b) => a != null && b != null && b.IsClass && !b.IsAbstract && a.IsAssignableFrom(b);

        public static readonly Func<Type, Type, bool> IsAssignableFromPredicate = (a, b) => a.Inherits(b);

        public static IEnumerable<Type> GetTypesAssignableFrom(Type baseType, Func<Assembly, bool> predicate = null)
        {
            return GetLoadableTypes(predicate).Where(t => IsAssignableClassFromPredicate(baseType, t));
        }

        public static IEnumerable<Type> GetUmbracoTypesAssignableFrom(Type baseType)
        {
            return GetLoadableTypes(IsUmbracoAssemblyPredicate).Where(t => IsAssignableClassFromPredicate(baseType, t));
        }

        public static IEnumerable<Assembly> GetAssemblies(Func<Assembly, bool> predicate = null)
        {
            return predicate != null ? AppDomain.CurrentDomain.GetAssemblies().Where(ass => !ass.IsDynamic).Where(predicate) : AppDomain.CurrentDomain.GetAssemblies().Where(ass => !ass.IsDynamic);
        }

        public static IEnumerable<Type> GetLoadableTypes(Func<Assembly, bool> assemblyPredicate = null)
        {
            return GetAssemblies(assemblyPredicate).SelectMany(s => GetTypesThatCanBeLoaded(s));
        }

        public static IEnumerable<Assembly> GetUmbracoAssemblies()
        {
            return GetAssemblies(IsUmbracoAssemblyPredicate);
        }

        public static IEnumerable<Type> GetLoadableUmbracoTypes()
        {
            return GetLoadableTypes(IsUmbracoAssemblyPredicate);
        }

        public static IEnumerable<TypeMap> GetTypeMapFrom(Type myType)
        {
            return GetTypesAssignableFrom(myType).Select(t => new TypeMap(t));
        }

        public static IEnumerable<TypeMap> GetNonGenericInterfaces(Assembly assembly)
        {
            return assembly.GetLoadableTypes().Where(t => t != null && !t.IsGenericType && t.IsPublic && !t.IsGenericTypeDefinition && t.IsInterface).Select(t => new TypeMap(t));
        }

        public static IEnumerable<TypeMap> GetNonGenericTypes(Assembly assembly)
        {
            return assembly.GetLoadableTypes().Where(t => t != null && !t.IsGenericType && t.IsPublic).Select(t => new TypeMap(t));
        }

        internal static IEnumerable<Diagnostic> PopulateDiagnosticsFrom(object obj, bool onlyUmbraco = true)
        {
            if (obj == null)
            {
                return Enumerable.Empty<Diagnostic>();
            }

            var props = obj.GetType().GetAllProperties();

            if (onlyUmbraco)
                props = props.Where(x => x.Module.Name.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase)).ToArray();

            List<Diagnostic> diagnostics = new List<Diagnostic>(props.Count());

            foreach (var prop in props)
            {
                try
                {
                    diagnostics.Add(new Diagnostic(GetPropertyDisplayName(prop), prop.GetValue(obj)));
                }
                catch { };
            }

            return diagnostics;
        }

        private static IEnumerable<Type> GetTypesThatCanBeLoaded(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            return GetTypesThatCanBeLoaded(assembly);
        }

        private static string GetPropertyDisplayName(PropertyInfo prop)
        {
            return prop.Name.Split('.').Last().SplitPascalCasing() + (prop.PropertyType == typeof(bool) ? "?" : String.Empty);
        }
    }
}