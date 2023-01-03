using Microsoft.AspNetCore.Mvc;
using MemoryLeak.Repositories;

namespace MemoryLeak.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    public class ButterToastController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SampleRepo repo;

        public ButterToastController(IHttpContextAccessor httpContextAccessor, SampleRepo repo)
        {
            _httpContextAccessor = httpContextAccessor;
            this.repo = repo;
        }

        [HttpGet("GetMyToast")]
        public async Task<IActionResult> GetMyToast()
        {
            try
            {
                var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.MapToIPv4().ToString();
                return Ok(ip);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GC")]
        public async Task<IActionResult> GC()
        {
            try
            {
                System.GC.Collect();
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }



        [HttpGet("GetLayers/{ToastID}")]
        public async Task<IActionResult> GetLayers(string ToastID)
        {
            try
            {
                var dto = repo.GetLayers(ToastID);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
