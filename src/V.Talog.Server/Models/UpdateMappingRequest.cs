using System.ComponentModel.DataAnnotations;

namespace V.Talog.Server.Models
{
    public class UpdateMappingRequest
    {
        [Required]
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

        /// <summary>
        /// tag、field 名称
        /// <para>仅当 Mapping 为 null 时生效</para>
        /// <para>用于更新单个字段的映射</para>
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 数据类型
        /// <para>仅当 Mapping 为 null 时生效</para>
        /// <para>用于更新单个字段的映射</para>
        /// </summary>
        public string ValueType { get; set; }
    }
}
