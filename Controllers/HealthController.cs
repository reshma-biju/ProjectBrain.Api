using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ProjectBrain.Api.Controllers
{
   
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IConfiguration configuration,
            ILogger<HealthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet("db")]
        public async Task<IActionResult> GetDatabaseHealth()
        {
            var connectionString =
                _configuration.GetConnectionString("Default");

            try
            {
                using var connection = new SqlConnection(connectionString);

                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";

                await command.ExecuteScalarAsync();

                return Ok(new
                {
                    status = "ok"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed.");

                return StatusCode(503, new
                {
                    status = "unavailable"
                });
            }
        }

    }
}
