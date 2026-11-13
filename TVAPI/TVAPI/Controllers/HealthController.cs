using Microsoft.AspNetCore.Mvc;

namespace TVAPI.Controllers
{
    // Sağlık kontrolü: istemci ya da izleme aracı API'nin çalışıp çalışmadığını buradan anlar.
    // Bilerek veritabanına dokunmaz; "süreç ayakta mı" sorusuna cevap verir, "veri alabiliyor muyum" sorusuna değil.
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                service = "TVAPI",
                time = DateTimeOffset.Now
            });
        }
    }
}
