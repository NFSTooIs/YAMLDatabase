﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;

namespace YAMLDatabase.ModScript.Utils
{
    public static class ValueConversionUtils
    {
        private static Dictionary<Type, Type> _typeCache = new Dictionary<Type, Type>();

        public static VLTBaseType DoPrimitiveConversion(PrimitiveTypeBase primitiveTypeBase, string str)
        {
            var type = primitiveTypeBase.GetType();
            if (_typeCache.TryGetValue(type, out var conversionType))
            {
                return DoPrimitiveConversion(primitiveTypeBase, str, conversionType);
            }

            // Do primitive conversion
            var primitiveInfoAttribute =
                type.GetCustomAttribute<PrimitiveInfoAttribute>();

            if (primitiveInfoAttribute == null)
            {
                // Try to determine enum type
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(VLTEnumType<>))
                {
                    primitiveInfoAttribute = new PrimitiveInfoAttribute(type.GetGenericArguments()[0]);
                }
                else
                {
                    throw new InvalidDataException("Cannot determine primitive type");
                }
            }

            var primitiveType = primitiveInfoAttribute.PrimitiveType;
            _typeCache[type] = primitiveType;
            return DoPrimitiveConversion(primitiveTypeBase, str, primitiveType);
        }

        private static VLTBaseType DoPrimitiveConversion(PrimitiveTypeBase primitiveTypeBase, string str, Type conversionType)
        {
            if (conversionType.IsEnum)
            {
                if (str.StartsWith("0x") &&
                    uint.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint val))
                {
                    primitiveTypeBase.SetValue((IConvertible) Enum.Parse(conversionType, val.ToString()));
                }
                else
                {
                    primitiveTypeBase.SetValue((IConvertible) Enum.Parse(conversionType, str));
                }
            }
            else
            {
                if (str.StartsWith("0x") && uint.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out uint val))
                {
                    primitiveTypeBase.SetValue((IConvertible) Convert.ChangeType(val, conversionType));
                }
                else
                {
                    primitiveTypeBase.SetValue(
                        (IConvertible) Convert.ChangeType(str, conversionType, CultureInfo.InvariantCulture));
                }
            }

            return primitiveTypeBase;
        }

        public static object DoPrimitiveConversion(object value, string str)
        {
            if (value == null)
            {
                // we don't know the type, just assume we need a string
                return str;
            }

            Type type = value.GetType();

            if (type == typeof(uint))
            {
                if (str.StartsWith("0x"))
                    return uint.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
                if (!uint.TryParse(str, out _))
                    return VLT32Hasher.Hash(str);
            }
            else if (type == typeof(int))
            {
                if (str.StartsWith("0x"))
                    return int.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
                if (!uint.TryParse(str, out _))
                    return unchecked((int)VLT32Hasher.Hash(str));
            }

            return type.IsEnum ? Enum.Parse(type, str) : Convert.ChangeType(str, type, CultureInfo.InvariantCulture);
        }
    }
}