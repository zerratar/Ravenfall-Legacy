using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class IoCContainer : MonoBehaviour
{
    private IoC ioc;
    private IoCContainerRegistry container;

    private void Awake()
    {
        ioc = new IoC();
        container = new IoCContainerRegistry(ioc);
    }

    public TInterface Resolve<TInterface>(params object[] args)
    {
        return ioc.Resolve<TInterface>(args);
    }

    private class IoC : IIoC, IDisposable
    {
        private readonly ConcurrentDictionary<Type, object> instances
            = new ConcurrentDictionary<Type, object>();

        private readonly ConcurrentDictionary<Type, TypeLookup> typeLookup
            = new ConcurrentDictionary<Type, TypeLookup>();

        private readonly ConcurrentDictionary<Type, Func<object>> typeFactories
            = new ConcurrentDictionary<Type, Func<object>>();

        public void RegisterShared<TInterface, TImplementation>()
        {
            typeLookup[typeof(TInterface)] = new TypeLookup(typeof(TImplementation), true);
        }

        public void Register<TInterface, TImplementation>()
        {
            typeLookup[typeof(TInterface)] = new TypeLookup(typeof(TImplementation), false);
        }

        public void Register<TImplementation>()
        {
            typeLookup[typeof(TImplementation)] = new TypeLookup(typeof(TImplementation), false);
        }

        public void RegisterCustomShared<T>(Func<object> func)
        {
            typeLookup[typeof(T)] = new TypeLookup(typeof(T), true);
            typeFactories[typeof(T)] = func;
        }

        public void RegisterCustom<T>(Func<object> func)
        {
            typeLookup[typeof(T)] = new TypeLookup(typeof(T), false);
            typeFactories[typeof(T)] = func;
        }

        public TInterface Resolve<TInterface>(params object[] args)
        {
            return (TInterface)Resolve(typeof(TInterface), args);
        }

        public object Resolve(Type t, params object[] args)
        {
            var interfaceType = t;

            if (!typeLookup.TryGetValue(t, out var targetType))
                throw new Exception($"Unable to resolve the type {t.Name}");

            if (targetType.Shared)
            {
                if (instances.TryGetValue(t, out var obj))
                {
                    return obj;
                }
            }

            if (typeFactories.TryGetValue(interfaceType, out var factory))
            {
                var item = factory();
                instances[interfaceType] = item;
                return item;
            }

            var publicConstructors = targetType.Type
                .GetConstructors(BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance);

            foreach (var ctor in publicConstructors)
            {
                var param = ctor.GetParameters();
                if (param.Length == 0)
                {
                    var instance = ctor.Invoke(null);
                    if (targetType.Shared) instances[interfaceType] = instance;
                    return instance;
                }

                var customArgIndex = 0;
                var hasCustomArgs = args.Length > 0;
                var badConstructor = false;
                var ctorArgs = new List<object>();
                foreach (var x in param)
                {
                    if (x.ParameterType.IsValueType || x.ParameterType == typeof(string))
                    {
                        if (!hasCustomArgs || args.Length <= customArgIndex)
                        {
                            badConstructor = true;
                            break;
                        }

                        ctorArgs.Add(args[customArgIndex++]);
                        continue;
                    }

                    ctorArgs.Add(Resolve(x.ParameterType));
                }

                if (badConstructor)
                {
                    continue;
                }

                var item = ctor.Invoke(ctorArgs.ToArray());
                if (targetType.Shared) instances[interfaceType] = item;
                return item;
            }
            throw new Exception($"Unable to resolve the type {targetType.Type.Name}");
        }

        public void Dispose()
        {
            foreach (var instance in instances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private class TypeLookup
        {
            public TypeLookup(Type type, bool shared)
            {
                Type = type;
                Shared = shared;
            }

            public Type Type { get; }
            public bool Shared { get; }
        }
    }
}