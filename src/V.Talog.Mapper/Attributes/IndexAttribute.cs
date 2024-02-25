using System;

namespace V.Talog.Mapper.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class IndexAttribute : Attribute
    {
        public string Name{get;set;}
    }
}