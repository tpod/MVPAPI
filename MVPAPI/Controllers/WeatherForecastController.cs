using Dapper;
using LazyCache;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace MVPAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IAppCache _cache;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IAppCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<dynamic>> Get()
    {
        var x = await _cache.GetAsync<List<string>>("DatabaseTableNames");
        
        // using (var connection = new NpgsqlConnection("User ID=postgres;Password=example;Host=postgres;Port=5432;Database=test;"))
        // {
        //     var x = await connection.QueryAsync("SELECT * FROM tabletest");
        //     return x.ToArray();
        // }
        
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
    
}