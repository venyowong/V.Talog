using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using V.Talog.Mapper.Attributes;

namespace V.Talog.Mapper
{
    public class TypeMapperInfo
    {
        public string IndexName{get;set;}

        public Type Type{get;set;}

        public Dictionary<string, TagMapperInfo> TagTypes{get;set;} = new Dictionary<string, TagMapperInfo>();

        public Dictionary<string, Type> FieldTypes{get;set;} = new Dictionary<string, Type>();

        public TypeMapperInfo(Type type)
        {
            this.Type = type;
            var attribute = Attribute.GetCustomAttribute(type, typeof(IndexAttribute)) as IndexAttribute;
            this.IndexName = string.IsNullOrEmpty(attribute.Name) ? type.Name : attribute.Name;

            foreach (var field in type.GetFields())
            {
                this.InitTagOrField(field);
            }
            foreach (var property in type.GetProperties())
            {
                this.InitTagOrField(property);
            }
        }

        private void InitTagOrField(FieldInfo fieldInfo)
        {
            var tagAttribute = Attribute.GetCustomAttribute(fieldInfo, typeof(TagAttribute)) as TagAttribute;
            if (tagAttribute == null)
            {
                if (!this.FieldTypes.ContainsKey(fieldInfo.Name))
                {
                    this.FieldTypes.Add(fieldInfo.Name, fieldInfo.FieldType);
                }
            }
            else
            {
                var name = tagAttribute.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = fieldInfo.Name;
                }
                if (!this.TagTypes.ContainsKey(name))
                {
                    this.TagTypes.Add(name, new TagMapperInfo{ Name = name, OriginalName = fieldInfo.Name, Type = fieldInfo.FieldType });
                }
            }
        }

        private void InitTagOrField(PropertyInfo propertyInfo)
        {
            var tagAttribute = Attribute.GetCustomAttribute(propertyInfo, typeof(TagAttribute)) as TagAttribute;
            if (tagAttribute == null)
            {
                if (!this.FieldTypes.ContainsKey(propertyInfo.Name))
                {
                    this.FieldTypes.Add(propertyInfo.Name, propertyInfo.PropertyType);
                }
            }
            else
            {
                var name = tagAttribute.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = propertyInfo.Name;
                }
                if (!this.TagTypes.ContainsKey(name))
                {
                    this.TagTypes.Add(name, new TagMapperInfo { Name = name, OriginalName = propertyInfo.Name, Type = propertyInfo.PropertyType });
                }
            }
        }
    }
}