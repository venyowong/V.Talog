using System;
using System.Collections.Generic;
using System.Linq;
using V.Talog.Mapper.Attributes;

namespace V.Talog.Mapper
{
    public class TypeMapper : IIndexMapping
    {
        private static Dictionary<string, TypeMapperInfo> _indexMap = new Dictionary<string, TypeMapperInfo>();
        private static Dictionary<Type, TypeMapperInfo> _typeMap = new Dictionary<Type, TypeMapperInfo>();

        static TypeMapper()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attribute = Attribute.GetCustomAttribute(type, typeof(IndexAttribute)) as IndexAttribute;
                    if (attribute == null)
                    {
                        continue;
                    }

                    var info = new TypeMapperInfo(type);
                    if (!_indexMap.ContainsKey(info.IndexName))
                    {
                        _indexMap.Add(info.IndexName, info);
                    }
                    if (!_typeMap.ContainsKey(type))
                    {
                        _typeMap.Add(type, info);
                    }
                }
            }
        }

        public Type GetFieldType(string index, string field)
        {
            if (!_indexMap.ContainsKey(index))
            {
                throw new Exception($"不存在 {index} 的配置");
            }
            var info = _indexMap[index];
            if (!info.FieldTypes.ContainsKey(field))
            {
                throw new Exception($"{index} 不存在 {field} 字段");
            }

            return info.FieldTypes[field];
        }

        public Type GetTagType(string index, string tag)
        {
            if (!_indexMap.ContainsKey(index))
            {
                throw new Exception($"不存在 {index} 的配置");
            }
            var info = _indexMap[index];
            if (!info.TagTypes.ContainsKey(tag))
            {
                throw new Exception($"{index} 不存在 {tag} 字段");
            }

            return info.TagTypes[tag].Type;
        }

        public TypeMapperInfo GetInfo(Type type)
        {
            if (!_typeMap.ContainsKey(type))
            {
                return null;
            }

            return _typeMap[type];
        }
    }
}