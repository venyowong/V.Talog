using System.ComponentModel.DataAnnotations;

namespace V.Talog.Server.Models
{
    public class AdminLoginRequest
    {
        [Required]
        public string Pwd { get; set; }
    }
}
