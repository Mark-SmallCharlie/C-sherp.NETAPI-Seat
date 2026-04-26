# 智慧座位预约系统后端（C-sherp.NETAPI-Seat）

> 基于 **ASP.NET Core 8** 的智慧座位预约系统后端，支持用户认证、座位预约、设备状态查询与管理、统计分析等核心能力。

---

## ✨ 项目概览

- **技术栈**：ASP.NET Core 8 / EF Core / JWT / OneNet HTTP 轮询
- **主要场景**：微信小程序 + 后端 API + 物联网设备状态同步
- **当前设备接入方式**：HTTP 轮询 OneNet（MQTT 相关代码保留作参考）

---

## 🗂️ 目录结构

```text
C-sherp.NETAPI-Seat/
├── Controllers/                # API 控制器
├── Data/                       # 数据库上下文与初始化
├── Migrations/                 # EF Core 迁移
├── Models/
│   ├── DTOs/                   # 请求/响应 DTO
│   ├── Entities/               # 数据库实体
│   ├── Device/                 # 设备模型
│   └── Mqtt/                   # MQTT 相关模型（保留）
├── Security/                   # 安全相关工具（如 PasswordHasher）
├── Services/
│   ├── Interfaces/             # 服务接口定义
│   ├── Mqtt/                   # MQTT 服务（保留）
│   └── OneNet/                 # OneNet HTTP 轮询服务
├── pages/                      # 微信小程序页面（index/login）
├── WeChatApp/                  # 小程序相关目录
├── Program.cs                  # 启动入口与依赖注入
└── appsettings*.json           # 配置文件
```

---

## 🚀 快速开始

### 1) 环境要求

- .NET SDK 8.x（见 `global.json`）

### 2) 本地运行

```bash
dotnet restore
dotnet build
dotnet run
```

### 3) 运行测试

```bash
dotnet test
```

---

## 🧩 核心模块说明

### Controllers（接口层）

| 控制器 | 主要职责 |
|---|---|
| `BaseController` | 通用身份信息提取与统一响应封装 |
| `AuthController` | 管理员登录、微信登录、Token 验证 |
| `UserController` | 用户资料、审核、角色管理 |
| `ReservationController` | 预约创建/取消/查询、冲突检测 |
| `DeviceController` | 设备状态查询、设备与座位映射管理 |
| `StatisticsController` | 日/月统计、热门座位、仪表盘数据 |
| `RegistrationController` | 用户注册接口 |
| `WeatherForecastController` | ASP.NET 默认示例控制器 |

### Data（数据层）

- `AppDbContext`：EF Core 数据库上下文，包含实体映射与索引配置
- `DbInitializer`：数据库初始化逻辑（如默认管理员数据）

### Services（业务层）

- `AuthService`：认证与授权
- `UserService`：用户管理
- `ReservationService`：预约业务
- `DeviceStatusService`：设备状态维护
- `StatisticsService`：统计分析
- `OneNetPollingService`：OneNet HTTP 轮询设备状态并同步座位状态

### Models（模型层）

- `DTOs/Requests`：接口入参模型
- `DTOs/Responses`：接口出参模型（含统一 `ApiResponse`）
- `Entities`：数据库实体（用户、预约、管理员、座位状态历史）
- `Device`：设备状态与映射模型
- `Mqtt`：MQTT 消息与配置模型（保留）

### 其他

- `Security/PasswordHasher.cs`：密码哈希与验证工具
- `pages/` & `WeChatApp/`：小程序侧资源目录

---

## 🔐 权限与认证

- 使用 JWT 进行认证
- 角色分层：匿名 / 登录用户 / 管理员
- 典型策略：`[Authorize]` 与 `[Authorize(Roles = "Admin")]`

---

## 📡 设备接入说明

- 当前主路径：**OneNet HTTP 轮询**（按固定间隔拉取设备状态）
- MQTT 服务：目前停用，但相关代码保留用于后续扩展或参考

---

## 📝 更新记录（近期）

### 2026-04-23
- 补充文件类注释，完善到 Model 层

### 2026-04-19
- 更新 README 文档

### 2026-04-15
- 修复并完善 `OneNetPollingService` 鉴权 Token 生成逻辑
- 支持动态生成基于 HMACSHA1 的合法签名 Token

### 2026-04-13
- 补充 MQTT 相关与 OneNet 注册服务类头部注释

### 2026-04-11
- 弃用 MQTT 实时连接方案，切换为 OneNet HTTP 轮询方案
- `Program.cs` 中关闭 MQTT 连接服务并启用 HTTP 轮询服务

---

## 📄 配置文件说明

- `appsettings.json`：通用配置
- `appsettings.Development.json`：开发环境配置
- `Properties/launchSettings.json`：本地启动配置
- `Properties/serviceDependencies*.json`：服务依赖配置

---

## 🤝 说明

本 README 主要用于帮助开发者快速理解项目结构与职责分层。若后续新增模块，建议按当前目录规范与分层方式同步更新本文档。
