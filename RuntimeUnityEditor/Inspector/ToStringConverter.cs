﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RuntimeUnityEditor.Core.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace RuntimeUnityEditor.Core.Inspector
{
    public static class ToStringConverter
    {
        private static readonly Dictionary<Type, Func<object, string>> _toStringConverters = new Dictionary<Type, Func<object, string>>();

        public static void AddConverter<TObj>(Func<TObj, string> objectToString)
        {
            _toStringConverters.Add(typeof(TObj), o => objectToString.Invoke((TObj)o));
        }

        public static string ObjectToString(object value)
        {
            var isNull = value.IsNullOrDestroyed();
            if (isNull != null) return isNull;

            switch (value)
            {
                case string str:
                    return str;
                case Transform t:
                    return t.name;
                case GameObject o:
                    return o.name;
                case Exception ex:
                    return "EXCEPTION: " + ex.Message;
                case Delegate d:
                    return DelegateToString(d);
            }

            var valueType = value.GetType();

            if (_toStringConverters.TryGetValue(valueType, out var func))
                return func(value);

            if (value is ICollection collection)
                return $"Count = {collection.Count}";

            if (value is IEnumerable _)
            {
                var property = valueType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanRead)
                {
                    if (property.GetValue(value, null) is int count)
                        return $"Count = {count}";
                }

                return "IS ENUMERABLE";
            }

            try
            {
                if (valueType.IsGenericType)
                {
                    var baseType = valueType.GetGenericTypeDefinition();
                    if (baseType == typeof(KeyValuePair<,>))
                    {
                        //var argTypes = baseType.GetGenericArguments();
                        var kvpKey = valueType.GetProperty("Key")?.GetValue(value, null);
                        var kvpValue = valueType.GetProperty("Value")?.GetValue(value, null);
                        return $"[{ObjectToString(kvpKey)} | {ObjectToString(kvpValue)}]";
                    }
                }

                return value.ToString();
            }
            catch
            {
                return valueType.Name;
            }
        }

        private static string DelegateToString(Delegate unityAction)
        {
            if (unityAction == null) return "[NULL]";
            string str;
            var isNull = unityAction.Target.IsNullOrDestroyed();
            if (isNull != null) str = "[" + isNull + "]";
            else str = unityAction.Target.GetType().FullName;
            var actionString = $"{str}.{unityAction.Method.Name}";
            return actionString;
        }

        internal static string EventEntryToString(UnityEventBase eventObj, int i)
        {
            if (eventObj == null) return "[NULL]";
            if (i < 0 || i >= eventObj.GetPersistentEventCount()) return "[Event index out of range]";
            // It's fine to use ? here because GetType works fine on disposed objects and we want to know the type name
            return $"{eventObj.GetPersistentTarget(i)?.GetType().FullName ?? "[NULL]"}.{eventObj.GetPersistentMethodName(i)}";
        }
    }
}