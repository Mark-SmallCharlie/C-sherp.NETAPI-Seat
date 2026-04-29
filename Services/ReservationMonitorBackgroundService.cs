using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Services
{
    public class ReservationMonitorBackgroundService : BackgroundService
    {
        private readonly ILogger<ReservationMonitorBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ReservationMonitorBackgroundService(ILogger<ReservationMonitorBackgroundService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("预约超时监控服务已启动...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                        int releasedCount = await reservationService.ReleaseTimeoutReservationsAsync();
                        
                        if (releasedCount > 0)
                        {
                            _logger.LogInformation($"成功释放了 {releasedCount} 个超时无人使用的座位。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "预约超时监控服务发生异常。");
                }

                // 每 5 分钟执行一次检查
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
