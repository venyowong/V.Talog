using System.ComponentModel.DataAnnotations;

namespace V.Talog.Server.Models
{
    public class SearchLogRequest
    {
        [Required]
        [MinLength(1)]
        public string Index { get; set; }

        [Required]
        [MinLength(1)]
        public List<Tag> Tags { get; set; }
    }
}
