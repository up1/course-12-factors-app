using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api
{
    [Route("api/config")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly AppSettings _appSettings;

        public ConfigController(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        [HttpGet("db-connection")]
        public IActionResult GetDbConnection()
        {
            return Ok(new { ConnectionString = _appSettings.PostgreSqlConnection });
        }
    }
}
