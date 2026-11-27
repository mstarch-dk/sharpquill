using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace SharpQuill.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;

    public HealthController(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var databaseStatus = "disconnected";

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();
            databaseStatus = "connected";
        }
        catch
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = databaseStatus
            });
        }

        return Ok(new
        {
            status = "healthy",
            database = databaseStatus
        });
    }
}
