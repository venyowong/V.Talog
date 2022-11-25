using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public class Config
    {
        public string DataPath { get; set; } = "data";

        /// <summary>
        /// Talogger 自动保存时间间隔，毫秒为单位
        /// </summary>
        public int TaloggerAutoSaveInterval = 1000;

        /// <summary>
        /// index 被判定为空闲的时间间隔，秒为单位
        /// </summary>
        public double IdleIndexInterval = 30;
    }
}
