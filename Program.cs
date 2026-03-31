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

var builder = WebApplication.CreateBuilder(args);

// 基础服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//// 添加MQTT配置
//builder.Services.Configure<MqttOptions>(
//    builder.Configuration.GetSection("Mqtt"));
//// 注册MQTT相关服务
//builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
//builder.Services.AddScoped<IMqttMessageHandler, MqttMessageHandler>();
//builder.Services.AddSingleton<IDeviceStatusService, DeviceStatusService>();
//builder.Services.AddHostedService<MqttBackgroundService>();

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
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
builder.Services.AddScoped<IMqttMessageHandler, MqttMessageHandler>();
builder.Services.AddSingleton<IDeviceStatusService, DeviceStatusService>();
builder.Services.AddHostedService<MqttBackgroundService>();

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

// 开发环境自动建库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
