using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.API.Services;
using System.Text;
using WebApplication1.Data;
using WebApplication1.Models.Mqtt;
using WebApplication1.Services;
using WebApplication1.Services.Interfaces;
using WebApplication1.Services.Mqtt;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 基础服务
// 替换原来的 AddControllers()
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // 忽略 JSON 序列化时的对象循环引用
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Add services to the container.
// 1. 添加数据库上下文
// 数据库
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT 认证
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 16)
{
    throw new InvalidOperationException(
        "JWT Key 未配置或长度不足（至少 16 字符）。请通过 User Secrets 或环境变量设置 Jwt:Key。\n" +
        "  dotnet user-secrets set \"Jwt:Key\" \"<your-strong-key-at-least-16-chars>\"");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WebApplication1API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WebApplication1Client";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// MQTT
//builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));
////builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
//builder.Services.AddScoped<IMqttMessageHandler, MqttMessageHandler>();
builder.Services.AddSingleton<IDeviceStatusService, DeviceStatusService>();
//builder.Services.AddHostedService<MqttBackgroundService>();

// 2. 添加 HTTP 客户端的注册：
builder.Services.AddHttpClient();
// 3. 注册新的 HTTP 轮询服务：
builder.Services.AddHostedService<WebApplication1.Services.OneNet.OneNetPollingService>();
builder.Services.AddHostedService<WebApplication1.Services.ReservationMonitorBackgroundService>();
// 业务服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWaitlistService, WaitlistService>();

// 速率限制（防暴力破解和 API 滥用）
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 认证接口：每分钟每 IP 最多 10 次请求
    options.AddPolicy("AuthPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // 预约创建接口：每分钟每 IP 最多 20 次请求
    options.AddPolicy("ReservationPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// CORS（从配置读取允许的来源）
var allowedOrigins = builder.Configuration["AllowedOrigins"] ?? "*";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (allowedOrigins == "*")
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .ToArray());
        }
        policy.AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 初始化数据库并创建默认管理员账户
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //dbContext.Database.Migrate();
    dbContext.Database.EnsureCreated();
    await DbInitializer.InitializeAsync(dbContext);
}

app.Run();
