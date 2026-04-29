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
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGeneration12345";
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

// CORS（开发联调）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 应用迁移（含 Users.PasswordHash 等结构变更）；首次运行会建库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //dbContext.Database.Migrate();
    dbContext.Database.EnsureCreated();
}

app.Run();
