using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog.Mapper
{
    public interface IVersioned
    {
        string Id { get; set; }

        bool IsValid { get; set; }

        DateTime CreateTime { get; set; }

        DateTime UpdateTime { get; set; }
    }
}
