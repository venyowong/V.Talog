namespace V.Talog.Server.Models
{
    public class UpdateMappingRequest
    {
        public string Index { get; set; }

        /// <summary>
        /// 0 tag 1 field
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// key 为 tag、field 名称，value 为数据类型
        /// <para>value 支持 string、bool、byte、char、DateTime、decimal、double、int16、int、int64、sbyte、float、uint16、uint、uint64 </para>
        /// </summary>
        public Dictionary<string, string> Mapping { get; set; }
    }
}
