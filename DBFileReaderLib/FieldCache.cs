﻿using DBFileReaderLib.Attributes;
using System;
using System.Reflection;

namespace DBFileReaderLib
{
    class FieldCache<T>
    {
        public readonly FieldInfo Field;
        public readonly bool IsArray = false;
        public readonly bool IsLocalisedString = false;
        public readonly Action<T, object> Setter;
        public readonly LocaleAttribute LocaleInfo;

        public bool IndexMapField { get; set; } = false;
        public int Cardinality { get; set; } = 1;

        public FieldCache(FieldInfo field)
        {
            Field = field;
            IsArray = field.FieldType.IsArray;
            IsLocalisedString = GetStringInfo(field, out LocaleInfo);
            Setter = field.GetSetter<T>();
            Cardinality = GetCardinality(field);

            IndexAttribute indexAttribute = (IndexAttribute)Attribute.GetCustomAttribute(field, typeof(IndexAttribute));
            IndexMapField = (indexAttribute != null) ? indexAttribute.NonInline : false;
        }

        private int GetCardinality(FieldInfo field)
        {
            var cardinality = field.GetAttribute<CardinalityAttribute>()?.Count;
            return cardinality.HasValue && cardinality > 0 ? cardinality.Value : 1;
        }

        private bool GetStringInfo(FieldInfo field, out LocaleAttribute attribute)
        {
            return (attribute = field.GetAttribute<LocaleAttribute>()) != null;
        }
    }
}
