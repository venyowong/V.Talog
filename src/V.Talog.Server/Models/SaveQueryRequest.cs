using System.ComponentModel.DataAnnotations;

namespace V.Talog.Server.Models
{
    public class SaveQueryRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Index { get; set; }

        [Required]
        public string TagQuery { get; set; } 

        public string Regex { get; set; }

        public string FieldQuery { get; set; }
    }
}
