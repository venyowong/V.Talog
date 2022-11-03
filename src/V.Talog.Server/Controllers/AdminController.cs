using Microsoft.AspNetCore.Mvc;
using V.Talog.Server.Models;
using V.User.Services;

namespace V.Talog.Server.Controllers
{
    [ApiController]
    [Route("admin")]
    public class AdminController
    {
        [HttpPost("login")]
        public object Login([FromBody] AdminLoginRequest request, [FromServices] IConfiguration config,
            [FromServices] JwtService jwt)
        {
            var pwdCfg = config["Admin:Password"];
            if (!string.IsNullOrEmpty(pwdCfg) && request.Pwd != pwdCfg)
            {
                return new { status = -1, msg = "密码错误" };
            }

            return new { status = 0, data = jwt.GenerateToken(new Dictionary<string, string> { { "role", "admin" } }) };
        }
    }
}
