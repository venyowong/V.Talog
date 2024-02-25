using System;

namespace V.Talog.Mapper.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TagAttribute : Attribute
    {
        public string Name{get;set;}

        public string Format{get;set;}
    }
}