﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mapster.Models;

namespace Mapster
{
    public static class TypeAdapter
    {
        public static ITypeAdapterBuilder<TSource> BuildAdapter<TSource>(this TSource source)
        {
            return new TypeAdapterBuilder<TSource>(source, TypeAdapterConfig.GlobalSettings);
        }

        public static ITypeAdapterBuilder<TSource> BuildAdapter<TSource>(this TSource source, TypeAdapterConfig config)
        {
            return new TypeAdapterBuilder<TSource>(source, config);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TDestination>(this object? source)
        {
            return Adapt<TDestination>(source, TypeAdapterConfig.GlobalSettings);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TDestination>(this object? source, TypeAdapterConfig config)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (source == null)
                return default!;
            var type = source.GetType();
            var fn = config.GetDynamicMapFunction<TDestination>(type);
            return fn(source);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source)
        {
            return TypeAdapter<TSource, TDestination>.Map(source);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source, TypeAdapterConfig config)
        {
            var fn = config.GetMapFunction<TSource, TDestination>();
            return fn(source);
        }

        /// <summary>
        /// Adapt the source object to the existing destination object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">The destination object to populate.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source, TDestination destination)
        {
            return Adapt(source, destination, TypeAdapterConfig.GlobalSettings);
        }

        /// <summary>
        /// Adapt the source object to the existing destination object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">The destination object to populate.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source, TDestination destination, TypeAdapterConfig config)
        {
            var sourceType = source?.GetType();
            var destinationType = destination?.GetType();

            if (sourceType == typeof(object)) // Infinity loop in ObjectAdapter if Runtime Type of source is Object 
                return destination;

            if (typeof(TSource) == typeof(object) || typeof(TDestination) == typeof(object))                
                return UpdateFuncFromPackedinObject(source, destination, config, sourceType, destinationType);
                       
            var fn = config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(source, destination);
        }

        private static TDestination UpdateFuncFromPackedinObject<TSource, TDestination>(TSource source, TDestination destination, TypeAdapterConfig config, Type sourceType, Type destinationType)
        {
            dynamic del = config.GetMapToTargetFunction(sourceType, destinationType);


            if (sourceType.GetTypeInfo().IsVisible && destinationType.GetTypeInfo().IsVisible)
            {
                dynamic objfn = del;
                return objfn((dynamic)source, (dynamic)destination);
            }
            else
            {
                //NOTE: if type is non-public, we cannot use dynamic
                //DynamicInvoke is slow, but works with non-public
                return (TDestination)del.DynamicInvoke(source, destination);
            }
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object? Adapt(this object source, Type sourceType, Type destinationType)
        {
            return Adapt(source, sourceType, destinationType, TypeAdapterConfig.GlobalSettings);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static object? Adapt(this object source, Type sourceType, Type destinationType, TypeAdapterConfig config)
        {
            var del = config.GetMapFunction(sourceType, destinationType);
            if (sourceType.GetTypeInfo().IsVisible && destinationType.GetTypeInfo().IsVisible)
            {
                dynamic fn = del;
                return fn((dynamic)source);
            }
            else
            {
                //NOTE: if type is non-public, we cannot use dynamic
                //DynamicInvoke is slow, but works with non-public
                return del.DynamicInvoke(source);
            }
        }

        /// <summary>
        /// Adapt the source object to an existing destination object.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">Destination object to populate.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object? Adapt(this object source, object destination, Type sourceType, Type destinationType)
        {
            return Adapt(source, destination, sourceType, destinationType, TypeAdapterConfig.GlobalSettings);
        }

        /// <summary>
        /// Adapt the source object to an existing destination object.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">Destination object to populate.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static object? Adapt(this object source, object destination, Type sourceType, Type destinationType, TypeAdapterConfig config)
        {
            var del = config.GetMapToTargetFunction(sourceType, destinationType);
            if (sourceType.GetTypeInfo().IsVisible && destinationType.GetTypeInfo().IsVisible)
            {
                dynamic fn = del;
                return fn((dynamic)source, (dynamic)destination);
            }
            else
            {
                //NOTE: if type is non-public, we cannot use dynamic
                //DynamicInvoke is slow, but works with non-public
                return del.DynamicInvoke(source, destination);
            }
        }
        
        /// <summary>
        /// Validate properties and Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination ValidateAndAdapt<TSource, TDestination>(this TSource source)
        {
            var sourceType = typeof(TSource);
            var selectorType = typeof(TDestination);

            var sourceProperties = new HashSet<string>(sourceType.GetProperties().Select(p => p.Name));
            var selectorProperties = new HashSet<string>(selectorType.GetProperties().Select(p=> p.Name));

            foreach (var selectorProperty in selectorProperties)
            {
                if (sourceProperties.Contains(selectorProperty)) continue;
                throw new Exception($"Property {selectorProperty} does not exist in {sourceType.Name} and is not configured in Mapster");
            }
            return source.Adapt<TDestination>();
        }
        
        /// <summary>
        /// Validate properties with configuration and Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination ValidateAndAdapt<TSource, TDestination>(this TSource source, TypeAdapterConfig config)
        {
            var sourceType = typeof(TSource);
            var selectorType = typeof(TDestination);

            var sourceProperties = new HashSet<string>(sourceType.GetProperties().Select(p => p.Name));
            var selectorProperties = new HashSet<string>(selectorType.GetProperties().Select(p=> p.Name));

            // Get the rule map for the current types
            var ruleMap = config.RuleMap;
            var typeTuple = new TypeTuple(sourceType, selectorType);
            ruleMap.TryGetValue(typeTuple, out var rule);

            foreach (var selectorProperty in selectorProperties)
            {
                if (sourceProperties.Contains(selectorProperty)) continue;
                // Check whether the adapter config has a config for the property
                if (rule != null && rule.Settings.Resolvers.Any(r => r.DestinationMemberName.Equals(selectorProperty))) continue;
                throw new Exception($"Property {selectorProperty} does not exist in {sourceType.Name} and is not configured in Mapster");
            }
            return source.Adapt<TDestination>(config);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "<Pending>")]
    public static class TypeAdapter<TSource, TDestination>
    {
        public static Func<TSource, TDestination> Map = TypeAdapterConfig.GlobalSettings.GetMapFunction<TSource, TDestination>();
    }
}
