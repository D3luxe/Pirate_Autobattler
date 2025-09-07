using System;
using System.Collections.Generic;

namespace PirateRoguelike.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "Service cannot be null.");
            }

            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service of type {type.Name} already registered.");
            }
            _services.Add(type, service);
        }

        public static T Resolve<T>()
        {
            Type type = typeof(T);
            if (!_services.TryGetValue(type, out object service))
            {
                throw new InvalidOperationException($"Service of type {type.Name} not registered.");
            }
            return (T)service;
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}
