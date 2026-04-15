using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
/***
 WeatherForecastController是一个ASP.NET Core Web API控制器，
负责处理与天气预报相关的HTTP请求。它提供了一个GET端点，
用于生成和返回一个包含未来五天天气预报的列表。
每个天气预报包括日期、温度（摄氏度）和天气摘要。该控制器使用依赖注入来获取日志记录器，
并通过随机数生成器来模拟天气数据。每次调用GET端点时，都会返回不同的天气预报数据。
 */
namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
