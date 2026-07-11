# 更新：
Master --更新 6.26（安全加固 + 代码质量 + 功能扩展）：
**安全加固（问题1-5）**
 * 1. JWT Key 与微信 AppSecret 从 appsettings.json 中移除，改用 dotnet user-secrets 存储，Program.cs 启动时校验 JWT Key 长度（≥16字符）；
 * 2. 密码哈希从 SHA256 替换为 BCrypt（`BCrypt.Net-Next`），自带盐值防彩虹表攻击；
 * 3. CORS 改为从配置读取 `AllowedOrigins`（支持 `*` 或逗号分隔的域名列表），不再硬编码 `AllowAll`；
 * 4. 添加 ASP.NET Core 内置速率限制：`AuthPolicy`（每分钟每 IP 最多 10 次）、`ReservationPolicy`（每分钟每 IP 最多 20 次）；
 * 5. DbInitializer 替代 `HasData()` 种子数据（BCrypt 非确定性哈希不兼容编译时种子），在运行时创建默认管理员。
**代码质量（问题1-5）**
 * 6. 所有 `DateTime.Now` 统一替换为 `DateTime.UtcNow`（ReservationService、ReservationMonitorBackgroundService 等）；
 * 7. RegistrationController 继承 BaseController，使用标准化响应格式（OkResponse/BadRequestResponse），Console.WriteLine 改为 ILogger；
 * 8. DTO 内联类提取到独立文件：`CancelReservationRequest.cs`、`CheckConflictRequest.cs` → Models/DTOs/Requests/；`StatisticsResponses.cs` → Models/DTOs/Responses/；
 * 9. DeviceController 匿名响应对象统一改为 BaseController 标准化响应方法（OkResponse、NotFoundResponse 等）；
 * 10. IStatisticsService/StatisticsService 清理未使用的 using、注释掉的重复代码。
**功能 #3：消息推送系统**
 * 11. 新增 `Notification` 实体（7 种通知类型：ReservationStart/TimeoutWarning/ForceReleased/Suspended/BanExpired/SystemMessage/WaitlistAvailable），AppDbContext 添加 DbSet<Notification>；
 * 12. 新增 `INotificationService` 接口与 `NotificationService` 实现（创建通知、按用户查询、未读筛选、标记已读）；
 * 13. 新增 `NotificationController`（GET my-notifications、GET unread-count、PUT {id}/read、PUT read-all）；
 * 14. 6 个通知触发点：预约创建成功、强制释放警告、冻结封禁通知、暂离即将超时（5分钟预警）、封禁到期解封通知、候补可用通知。
**功能 #4：排队候补机制**
 * 15. 新增 `WaitlistEntry` 实体（5 种状态：Waiting/Notified/Confirmed/Expired/Cancelled），AppDbContext 添加 DbSet<WaitlistEntry>；
 * 16. 新增 `IWaitlistService` 接口与 `WaitlistService` 实现（加入候补、确认候补→自动创建预约、取消候补、取消预约时自动推进队列）；
 * 17. ReservationController 新增 3 个候补端点：POST join-waitlist、POST confirm-waitlist/{id}、POST cancel-waitlist/{id}；取消预约时自动调用 PromoteWaitlistAsync；
 * 18. ReservationMonitorBackgroundService 新增候补超时扫描：Notified 状态超过 15 分钟未确认 → 自动通知下一位候补者。
**功能 #5：违规计数衰减**
 * 19. User 实体新增 `GoodReservationStreak` 字段（连续正常完成预约计数）；
 * 20. ReservationService：每次正常完成预约 → GoodReservationStreak++，每满 5 次 → ViolationCount -= 1（最小为 0）；发生违规时 Streak 归零。
**其他优化**
 * 21. ReservationMonitorBackgroundService 轮询间隔从 5 分钟改为 1 分钟，新增 4 个子任务：超时释放、暂离预警、封禁解封、候补超时处理。
 * 22. Csproj 添加 `BCrypt.Net-Next` NuGet 包依赖。
    
Master --更新 7.11;
*  1.添加硬件层.c文件；
*  2.添加硬件层.h文件；
*  3.上传硬件层文件；

Master --更新 6.26（代码审查 — 已知待修复问题）：
以下为最新版本代码的自我审查缺陷，按影响程度排列，后续逐一修改。

🔴 影响较大：
 * 1.【密码哈希不兼容】SHA256→BCrypt 后旧用户密码全部失效无法登录，缺少兼容迁移逻辑（BCrypt 验证失败→回退 SHA256→自动升级为 BCrypt）。
 * 2.【违规衰减触发时机】GoodReservationStreak 递增写在 UpdateExpiredReservationsAsync，仅用户查预约时才调用。不查预约则永远不会标记 Completed，Streak 永远不涨。应改为后台定时任务自动触发。
 * 3.【候补确认非原子操作】ConfirmWaitlistAsync 冲突检查→创建预约之间有时间窗口，极端并发下可能被其他用户抢占。

🟡 影响中等：
 * 5.【暂离预警重复通知】CheckLeaveExpiringAsync 每分钟扫描一次，同一个暂离预约可能被连续发送 5 次预警通知，缺少去重标记。
 * 6.【封禁解封检查窗口过窄】仅扫描 SuspendedUntil 在过去 2 分钟内的用户，后台宕机超过 2 分钟则解封通知丢失。解封后 SuspendedUntil 未归 null。
 * 7.【候补位置不重排】用户取消候补后 QueuePosition 出现空洞，虽然排序正常但显示给用户的排位不准确。
 * 8.【候补匹配粒度粗】按 SeatNumber+StartTime+EndTime 精确匹配，时间差 5 分钟的两条预约不在同一队列，取消一个不会触发另一个候补。

🟢 影响较小：
 * 9.【通知表无限增长】无定期清理旧通知机制，长期数据库性能会下降。
 * 10.【速率限制未穿透代理】使用 RemoteIpAddress 做 Key，前面有 Nginx/K8s Ingress 时所有 IP 相同，限制变成全局。未读 X-Forwarded-For 头。
 * 11.【预约即将开始通知未实现】NotificationType 枚举定义了 ReservationStart，但代码中未触发。
 * 12.【取消+推进候补无事务】CancelReservation 中预约取消成功→推进候补失败时无回滚，预约已丢但候补未通知，两边受损。
 * 13.【候补队列无上限】热门时段可无限排队。
 * 14.【违规衰减无通知】ViolationCount 减少时未告知用户。

Master --更新 5.13：
*  1.引入分布式锁（如基于 Redis 的 Redlock）锁住具体的 SeatNumber，
*  2.（待补充）或者在 EF Core 中对座位表添加乐观并发控制（Concurrency Token / RowVersion），确保同一时间只有一个预约能落库成功，彻底解决高并发预约冲突问题。
Master --更新 5.8：
*  1.实体扩展 (User.cs, Reservation.cs)：
    * 在 User 实体中添加了 ViolationCount（违约次数统计）和 SuspendedUntil（封禁截至时间）字段。
	* 在 Reservation 实体中添加了 LeaveEndTime（允许暂离的结束有效期限标记）字段。
*  2.控制器公开新接口 (ReservationController.cs)：
	* 添加了 POST: api/Reservation/temp-leave/{reservationId} 接口，供前端点击触发暂离（默认 15 分钟）。
	* 添加了 POST: api/Reservation/return-leave/{reservationId} 接口，供用户提前返回并取消暂离状态。
*  3.核心业务逻辑改进 (ReservationService.cs)：
	* 拦截预约：在 CreateReservationAsync(CreateReservationRequest, int) 首行添加逻辑，如果用户的 SuspendedUntil 在未来，将阻止其预约并以错误提示“因违规多次被冻结”拦截请求。
	* 违规处分：在后台监控任务 ReleaseTimeoutReservationsAsync() 中，若座位超 30 分钟未感知落座，被“强制释放”后：顺带增加用户 ViolationCount。满 3 次时，将用户 SuspendedUntil 设置为三天后。
	* 暂离保护：同在 ReleaseTimeoutReservationsAsync() 中，扫描时如果发现该预定单的 LeaveEndTime 已存在且大于当前时间，则强制取消的巡检将自动 continue;（跳过它），保护处于洗手间等场景的用户

Master --更新5.1：
 * 添加 `ReservationMonitorBackgroundService` 系统定时监控后台服务：如果预约超过 30 分钟硬件感知无人使用，则自动进行强制释放座位（`ForceCancelled`）。
 * 并在 `IReservationService` 与 `ReservationService` 中实现了 `ReleaseTimeoutReservationsAsync` 作为判断与释放的具体逻辑。
 * 对`IReservationService`和`ReservationService`的显示接口调用方法进行注释；
 * 修改了三个方法：
	* GetSeatUtilizationAsync (line 130）
	* GetPopularSeatsAsync (line 240)
	* GetUserActivityAsync (line 280)
 * 增加**信用积分/违约惩罚机制**（如果在没有请暂离且不就坐，将触发违约记录，满 3 次自动封禁 3 天）。
 * 增加**签到打卡、暂离状态**机制（支持用户前端点击“暂离”，15 分钟内暂停硬件无人的自动释放判定）。
 * SeatUtilizationResponse 新增字段：
    * Dictionary<int, double> ActualUtilizationRates   // 每座实际使用率%（硬件数据）
    * double OverallActualUtilization                   // 整体实际使用率%

 *  GetSeatUtilizationAsync 新增逻辑（算法）：
    * 1. 查询 SeatStatusHistories 表近30天记录，按座位号+时间排序
    * 2. 按座位分组，遍历状态变化事件：
      	- IsOccupied=true  → 记录开始时间
  		- IsOccupied=false → 计算与开始时间的差值，累加到占用时长
    * 3. 末尾仍为"占用中" → 用 DateTime.UtcNow 作为结束时间
    * 4. 占用时长 / 理论可用时长 × 100% = 实际使用率
 * 更新 `README.md` 文档。

Master --更新 4.30：
 *  添加修改超时预约的接口和实现类；
 *  在ReservationController中添加修改预约的接口；
 *  系统对超时预约进行自动取消处理，并在日志中记录相关信息。

Master --更新 4.29：
  * 添加Services下的接口和实现类的头部注释

Master --更新 4.26：
  * 添加Models下的OneNetMqttMessages文件的类的注释
  * 添加Models下的MqttM文件的类的注释

Master --更新 4.23：
  * 对文件类的注释补充已经补充到了Modle。

Master --更新 4.19：
  * 更新Readme.md文档

Master --更新 4.15：

  * 修复且完善了 `OneNetPollingService` 中 HTTP 轮询服务生成鉴权 Token 的加密转换逻辑（解决 `byte[]` 无法转换为 `char[]` 的错误），支持动态生成基于 HMACSHA1 的合法签名 Token。
  * 更新该 README.md 文档，补充了关于 `OneNetPollingService` 服务层的相关描述。

Master --更新 4.13：

  * 添加Mqtt类的头部注释，取消掉Mqtt服务但保留mqtt相关类
  * 添加OneNet注册服务类的头部注释

Master --更新 4.11：
 * 摒弃掉MQTT连接订阅服务，用HTTP连接OneNet接口获取设备状态，后端每个3秒钟轮询一次设备状态，前端通过API接口获取设备状态显示在小程序上。
 * Program.cs中注释掉MQTT连接服务的相关代码，启用HTTP轮询设备状态服务。
 * OneNET采用固定Token进行认证。
--------
# 智慧座位预约系统后端

## ✨ 项目概览

- **技术栈**：ASP.NET Core 8 / EF Core / JWT / OneNet HTTP 轮询
- **主要场景**：微信小程序 + 后端 API + 物联网设备状态同步
- **当前设备接入方式**：HTTP 轮询 OneNet（MQTT 相关代码保留作参考）

---

## 🗂️ 目录结构

```text
C-sherp.NETAPI-Seat/
├── .github/                    # GitHub 相关配置（如 Actions 工作流）
├── Controllers/                # API 控制器（Auth/Base/Device/Notification/Registration/Reservation/Statistics/User/WeatherForecast）
├── Data/                       # 数据库上下文（AppDbContext）与初始化（DbInitializer）
├── Migrations/                 # EF Core 迁移
├── Models/
│   ├── Device/                 # 设备模型（DeviceStatus、DeviceSeatMapping）
│   ├── DTOs/                   # 请求/响应 DTO
│   │   ├── Requests/           # 请求参数模型（Login/CreateReservation/WechatLogin/Register/ApproveUser/CancelReservation/CheckConflict 等）
│   │   └── Responses/          # 响应数据模型（Login/ApiResponse/UserInfo/Statistics 等）
│   ├── Entities/               # 数据库实体（User/Reservation/AdminUser/SeatStatusHistory/Notification/WaitlistEntry）
│   └── Mqtt/                   # MQTT 相关模型（保留作参考）
├── pages/                      # 微信小程序页面（index/login）
├── Properties/                 # 项目配置（launchSettings.json 等）
├── Security/                   # 安全相关工具（PasswordHasher — BCrypt 哈希）
├── Services/
│   ├── Interfaces/             # 服务接口定义（IAuth/IUser/IReservation/IStatistics/IDeviceStatus/INotification/IWaitlist 等）
│   ├── Mqtt/                   # MQTT 服务（保留作参考）
│   └── OneNet/                 # OneNet HTTP 轮询服务
│   └── 其他 Service 服务类     # 不做为单独目录的服务类（Auth/User/Reservation/Statistics/DeviceStatus/Notification/Waitlist 等）
├── WeChatApp/                  # 小程序主入口（app.js）
├── Program.cs                  # 启动入口与依赖注入（速率限制/JWT验证/CORS/服务注册/数据库初始化）
└── appsettings*.json           # 配置文件（敏感信息通过 User Secrets 管理）
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

## 1.Controllers

### 控制器

 - Auth、Base、Device、预约、注册等控制器
 - User、天气信息控制器

## 2.Data

### 读取连接数据库
 - Dblinitializer

### 创建数据库的表
 - AppDbContext 

## 3.Migrations

* AddUserPasswordHash.cs
* AddUDbcontextMoodelSnapshot.cs
* ExtendAvatarUrlLength.cs

## 4.Models

### Device

### DTOs

### Entites

- 创建数据库表里键属性用于调用

### Mqtt

- 更新：取消MQTT连接，取代的是HTTP连接
- MQTT连接配置  
- OneNet连接配置

## 5.Services
### Interface接口

### OneNet
- `OneNetPollingService`：HTTP 设备状态轮询服务，通过动态生成专属鉴权 Token 轮询拉取 OneNet 平台的设备最新属性数据，并同步更新后台的座位状态。

### Mqtt

- 连接服务控制服务报错重新连服务（现已停用并替换为 HTTP 轮询服务，保留相关类主要用作参考）

### 微信小程序服务

## 6.Program.cs

### 项目启动main函数

## 7.Properties

### json配置文件

* lauchSettings.json
* serviceDependencies.json
* serviceDependenices.local.json

## 8.补充哈希值加密类
* PasswordHasher.cs

## 9.pages(微信小程序)
### index
### login

## 10.WeChatApp
* .gitkeep
* app.js
-----

## 1.Controllers

以下是对代码库中`Controllers`文件夹下每个控制器类的详细介绍，包含类的功能、核心特性、关键方法及设计特点：
### 1. `BaseController`（基础控制器）
**文件路径**：Controllers/BaseController.cs  
**核心定位**：所有业务控制器的基类，封装通用的身份认证、响应格式化等公共逻辑，实现代码复用。  

#### 核心功能：
- **身份信息提取**：
  - `GetUserId()`：从`User`的`Claim`中解析用户ID（支持`nameid`/`NameIdentifier`两种Claim类型）。
  - `GetUserRole()`：提取用户角色（`ClaimTypes.Role`）。
  - `IsAdmin()`：判断当前用户是否为管理员（角色等于"Admin"）。
- **标准化响应**：封装统一的API响应格式（包含`success`/`message`/`data`字段），避免重复编码：
  - `OkResponse<T>()`：成功响应（200）。
  - `BadRequestResponse()`：无效请求（400）。
  - `NotFoundResponse()`：资源不存在（404）。
  - `UnauthorizedResponse()`：未授权（401）。
  - `ServerErrorResponse()`：服务器内部错误（500）。

#### 设计特点：
- 继承`ControllerBase`（无视图的API控制器基类）。
- 所有方法标记为`protected`，仅子类可访问。
- 统一响应格式，降低前端对接成本。

---

### 2. `RegistrationController`（注册控制器）
**文件路径**：Controllers/RegistrationController.cs  
**核心定位**：处理用户注册请求（推测为微信小程序用户注册）。  

#### 核心功能：
- **注册接口**：`[HttpPost] Register()`
  - 接收`RegisterRequest`类型的请求体（DTO）。
  - 验证模型合法性（`ModelState.IsValid`）。
  - 调用`IUserService.RegisterAsync()`完成注册逻辑。
  - 根据服务返回结果，返回成功或失败响应（使用 BaseController 标准化方法）。

#### 设计特点：
- 继承`BaseController`，使用标准化响应格式（`OkResponse`/`BadRequestResponse`/`ServerErrorResponse`）。
- 注入 `ILogger<RegistrationController>`，日志记录替代原有的 `Console.WriteLine`。
- 依赖`IUserService`接口，解耦业务逻辑（符合依赖注入/面向接口编程）。

---

### 3. `AuthController`（认证控制器）
**文件路径**：Controllers/AuthController.cs  
**核心定位**：处理登录、Token验证等认证相关逻辑，继承`BaseController`。  

#### 核心功能：
| 方法 | 路由 | 权限 | 功能 |
|------|------|------|------|
| `AdminLogin()` | `POST api/Auth/admin-login` | 匿名 | 管理员账号密码登录，调用`IAuthService.AdminLoginAsync()`生成Token |
| `WechatLogin()` | `POST api/Auth/wechat-login` | 匿名 | 微信小程序登录，支持“待审核”状态返回（`RequiresApproval`） |
| `ValidateToken()` | `GET api/Auth/validate-token` | 登录用户 | 验证Token有效性，返回用户ID/角色 |
| `AdminOnlyEndpoint()` | `GET api/Auth/admin-only` | 仅管理员 | 测试管理员权限的示例接口 |

#### 设计特点：
- 集成日志（`ILogger`），记录登录/验证的关键行为与异常。
- 标准化响应（复用`BaseController`的`OkResponse`/`UnauthorizedResponse`等）。
- 区分管理员/普通用户登录逻辑，支持微信登录的”待审核”业务场景。
- 登录接口启用速率限制（`[EnableRateLimiting(“AuthPolicy”)]`），每分钟每 IP 最多 10 次请求。

---

### 4. `UserController`（用户管理控制器）
**文件路径**：Controllers/UserController.cs  
**核心定位**：处理用户资料、审核、角色管理等用户相关业务，继承`BaseController`，需登录认证。  

#### 核心功能：
| 方法 | 路由 | 权限 | 功能 |
|------|------|------|------|
| `GetProfile()` | `GET api/User/profile` | 登录用户 | 获取当前登录用户的资料 |
| `GetPendingUsers()` | `GET api/User/pending-users` | 仅管理员 | 获取待审核用户列表 |
| `GetAllUsers()` | `GET api/User/all-users` | 仅管理员 | 获取所有用户列表 |
| `ApproveUser()` | `POST api/User/approve-user/{userId}` | 仅管理员 | 审核用户（通过/拒绝），支持备注 |
| `UpdateUserRole()` | `PUT api/User/update-role/{userId}` | 仅管理员 | 更新用户角色（校验`UserRole`枚举合法性） |

#### 附属DTO：
- `ApproveUserRequest`：审核请求（是否通过、备注）。
- `UpdateRoleRequest`：角色更新请求（新角色`NewRole`）。

#### 设计特点：
- 严格的权限控制（`[Authorize]`/`[Authorize(Roles = "Admin")]`）。
- 异常捕获+日志记录，定位用户操作失败原因。
- 模型验证+业务校验（如角色枚举合法性）。

---

### 5. `ReservationController`（预约控制器）
**文件路径**：Controllers/ReservationController.cs  
**核心定位**：处理座位预约的创建、取消、查询、冲突检测、暂离/返回、候补排队等，继承`BaseController`，需登录认证。  

#### 核心功能：
| 方法 | 路由 | 权限 | 功能 |
|------|------|------|------|
| `CreateReservation()` | `POST api/Reservation/create` | 登录用户（限流） | 创建预约（关联用户ID，校验时间/座位冲突） |
| `CancelReservation()` | `POST api/Reservation/cancel/{reservationId}` | 登录用户（管理员可取消任意预约） | 取消预约，自动推进候补队列 |
| `GetMyReservations()` | `GET api/Reservation/my-reservations` | 登录用户 | 获取当前用户的所有预约 |
| `GetAllReservations()` | `GET api/Reservation/all-reservations` | 仅管理员 | 获取所有预约列表 |
| `GetActiveReservations()` | `GET api/Reservation/active-reservations` | 登录用户 | 获取活跃（未结束）预约列表 |
| `CheckSeatConflict()` | `POST api/Reservation/check-conflict` | 登录用户 | 检测指定座位+时间段是否存在预约冲突 |
| `SetTemporaryLeave()` | `POST api/Reservation/temp-leave/{reservationId}` | 登录用户 | 设置暂离（默认15分钟），期间硬件自动释放暂停 |
| `ReturnFromLeave()` | `POST api/Reservation/return-leave/{reservationId}` | 登录用户 | 提前结束暂离状态 |
| `JoinWaitlist()` | `POST api/Reservation/join-waitlist` | 登录用户 | 加入候补队列（需确认座位有冲突） |
| `ConfirmWaitlist()` | `POST api/Reservation/confirm-waitlist/{waitlistId}` | 登录用户 | 确认候补名额，自动创建预约 |
| `CancelWaitlist()` | `POST api/Reservation/cancel-waitlist/{waitlistId}` | 登录用户 | 主动取消候补排队 |

#### 附属DTO：
- `CreateReservationRequest`：创建预约请求（座位号、开始/结束时间）。
- `CancelReservationRequest`：取消预约请求（管理员备注）。
- `CheckConflictRequest`：冲突检测请求（座位号、开始/结束时间、排除的预约ID）。

#### 设计特点：
- 区分普通用户/管理员权限（管理员可操作所有预约，普通用户仅操作自己的）。
- 核心业务校验（预约冲突、预约存在性、操作权限、封禁状态检查）。
- 候补与预约联动：取消预约时自动推进候补队列；确认候补时自动创建预约。
- 详细的日志记录（预约ID、用户ID、操作类型）。

---

### 6. `StatisticsController`（统计控制器）
**文件路径**：Controllers/StatisticsController.cs  
**核心定位**：处理管理员端的统计分析需求，继承`BaseController`，仅管理员可访问。  

#### 核心功能：
| 方法 | 路由 | 功能 |
|------|------|------|
| `GetDailyStatistics()` | `GET api/Statistics/daily/{date}` | 获取指定日期的预约/使用统计 |
| `GetMonthlyStatistics()` | `GET api/Statistics/monthly/{year}/{month}` | 获取指定年月的统计数据（校验年月合法性） |
| `GetSeatUtilization()` | `GET api/Statistics/seat-utilization` | 获取座位利用率统计 |
| `GetPopularSeats()` | `GET api/Statistics/popular-seats` | 获取热门座位TOP N（默认10，限制1-100） |
| `GetUserActivity()` | `GET api/Statistics/user-activity` | 获取指定天数内的用户活跃度（默认30天，限制1-365） |
| `GetDashboardData()` | `GET api/Statistics/dashboard` | 聚合仪表盘数据（月度统计+座位利用率+热门座位+周活跃度） |

#### 设计特点：
- 仅管理员可访问（`[Authorize(Roles = "Admin")]`）。
- 参数合法性校验（年月范围、TOP N/天数限制）。
- 聚合统计能力（`GetDashboardData`），适配后台仪表盘展示。

---

### 7. `DeviceController`（设备控制器）
**文件路径**：Controllers/DeviceController.cs  
**核心定位**：处理设备状态、座位映射等物联网相关逻辑，继承`BaseController`，需登录认证。  

#### 核心功能：
| 方法 | 路由 | 权限 | 功能 |
|------|------|------|------|
| `GetAllDeviceStatus()` | `GET api/Device/status` | 登录用户 | 获取所有设备的状态 |
| `GetDeviceStatus()` | `GET api/Device/status/{deviceId}` | 登录用户 | 获取指定设备的状态 |
| `GetSeatOccupancyStatus()` | `GET api/Device/seat-occupancy` | 登录用户 | 获取座位占用状态（设备关联座位） |
| `SetDeviceMapping()` | `POST api/Device/mapping` | 仅管理员 | 设置设备与座位的映射关系（设备ID/座位号/位置） |
| `RemoveDeviceMapping()` | `DELETE api/Device/mapping/{deviceId}` | 仅管理员 | 移除设备的座位映射 |
| `GetDeviceMappings()` | `GET api/Device/mappings` | 登录用户 | 占位接口（返回映射功能正常提示） |

#### 附属DTO：
- `SetDeviceMappingRequest`：设备映射请求（设备ID、座位号、位置）。

#### 设计特点：
- 设备与座位解耦（通过映射关联），适配物联网场景。
- 部分接口未完全实现（如`GetDeviceMappings`仅返回提示），预留扩展空间。
- 响应格式统一使用 BaseController 标准化方法（OkResponse、NotFoundResponse 等）。
- 异常日志记录设备操作的失败原因。

---

### 8. `NotificationController`（通知控制器）
**文件路径**：Controllers/NotificationController.cs  
**核心定位**：处理用户通知的查询与已读标记，继承`BaseController`，需登录认证。  

#### 核心功能：
| 方法 | 路由 | 权限 | 功能 |
|------|------|------|------|
| `GetMyNotifications()` | `GET api/Notification/my-notifications` | 登录用户 | 获取当前用户的通知列表（含未读数量），支持 unreadOnly 筛选 |
| `GetUnreadCount()` | `GET api/Notification/unread-count` | 登录用户 | 获取当前用户的未读通知数量 |
| `MarkAsRead()` | `PUT api/Notification/{id}/read` | 登录用户 | 标记指定通知为已读（校验归属） |
| `MarkAllAsRead()` | `PUT api/Notification/read-all` | 登录用户 | 标记当前用户所有通知为已读 |

#### 设计特点：
- 返回最近 100 条通知（按创建时间降序）。
- 通知归属校验：用户只能操作自己的通知。
- 支持 7 种通知类型：ReservationStart（预约开始）/ TimeoutWarning（暂离预警）/ ForceReleased（强制释放）/ Suspended（冻结封禁）/ BanExpired（封禁解除）/ SystemMessage（系统消息）/ WaitlistAvailable（候补可用）。

---

### 9. `WeatherForecastController`（天气预测控制器）
**文件路径**：Controllers/WeatherForecastController.cs  
**核心定位**：ASP.NET Core默认生成的示例控制器，无业务意义。  

#### 核心功能：
- `Get()`：`GET /WeatherForecast`，返回随机生成的5条天气预测数据（日期、温度、天气描述）。

#### 设计特点：
- 未继承`BaseController`，使用默认`ControllerBase`。
- 无认证/授权限制，纯示例代码。

---

### 整体设计总结
1. **分层与复用**：通过`BaseController`封装公共逻辑（身份提取、标准化响应），子类专注业务，符合DRY原则。
2. **权限控制**：区分匿名/登录用户/管理员，通过`[Authorize]`+角色校验实现精细化权限；敏感接口启用速率限制（AuthPolicy/ReservationPolicy）。
3. **标准化**：统一响应格式（`ApiResponse<T>` / BaseController 响应方法）、日志记录（ILogger）、异常处理，提升代码可维护性。
4. **解耦**：依赖服务接口（如`IUserService`/`IReservationService`/`INotificationService`/`IWaitlistService`），而非具体实现，便于测试和扩展。
5. **业务适配**：贴合”预约系统”核心场景（用户审核、座位预约、设备管理、统计分析、消息通知、候补排队），覆盖C端（用户）和B端（管理员）需求。
6. **安全加固**：JWT密钥/AppSecret 使用 User Secrets 管理、BCrypt 密码哈希、速率限制、CORS 可配置。
7. **数据一致性**：所有时间统一使用 `DateTime.UtcNow`，避免时区差异导致的逻辑错误。

## 2.Data

### 1. AppDbContext 类（Data/AppDbContext.cs）
`AppDbContext` 是基于 EF Core（Entity Framework Core）的数据库上下文类，是应用程序与数据库交互的核心入口，负责映射实体与数据库表、配置模型规则、管理数据连接等。

#### 核心功能与结构：
- **继承关系**：继承自 EF Core 的 `DbContext` 基类，是 EF Core 操作数据库的基础。
- **构造函数**：接收 `DbContextOptions<AppDbContext>` 参数，用于注入数据库配置（如连接字符串、数据库提供器），并传递给基类。
- **DbSet 属性**：映射实体类到数据库表，每个 `DbSet<T>` 对应一张表：
  - `DbSet<User> Users`：用户表
  - `DbSet<Reservation> Reservations`：预约记录表
  - `DbSet<SeatStatusHistory> SeatStatusHistories`：座位状态历史表
  - `DbSet<AdminUser> AdminUsers`：管理员用户表
  - `DbSet<Notification> Notifications`：通知消息表
  - `DbSet<WaitlistEntry> WaitlistEntries`：候补排队表
- **OnModelCreating 方法**：重写基类方法，用于配置实体模型的额外规则：
  - 为 `User` 实体的 `OpenId` 字段配置**唯一索引**，确保每个用户的 OpenId 不重复；
  - 为 `Reservation` 实体配置**复合索引**（SeatNumber + StartTime + EndTime），优化座位预约冲突的查询效率；
  - 管理员账户不再使用 `HasData()` 种子数据（BCrypt 每次生成不同哈希值，编译期种子的验证逻辑已失效），改由 `DbInitializer` 运行时创建。

### 2. DbInitializer 类（Data/DbInitializer.cs）
`DbInitializer` 是静态工具类，用于数据库初始化，核心作用是确保数据库创建完成，并初始化基础数据（如默认管理员账户）。

#### 核心功能与结构：
- **静态方法 InitializeAsync**：异步初始化数据库的入口方法：
  - 调用 `context.Database.EnsureCreatedAsync()`：确保数据库存在（若不存在则创建，仅在数据库首次启动时生效）；
  - 检查 `AdminUsers` 表是否已有数据：若为空，则创建默认管理员账户并写入数据库；
- **密码哈希**：使用 `PasswordHasher.Hash()`（BCrypt 算法）对管理员密码进行哈希处理，自带盐值防彩虹表攻击。

### 两类的核心协作关系
1. `AppDbContext` 定义了数据库的”结构规则”（表映射、索引）；
2. `DbInitializer` 负责”数据初始化”（确保库创建、补充基础数据）；
3. 两者结合：既保证数据库表结构符合业务规则，又确保系统启动时拥有必要的初始数据（如默认管理员）。

### 补充说明
- 管理员初始化由 `DbInitializer` 在程序运行时完成，而非 `HasData()` 迁移种子数据。原因是密码改用 BCrypt 哈希后每次哈希结果不同，无法在编译期硬编码。
- BCrypt 是当前业界推荐的密码哈希算法，自带盐值（Salt），可有效防止彩虹表攻击。

以下是对该代码库中各文件夹下类的详细介绍，按文件路径分类说明：

## 4.Modles

### 一、Models/DTOs/Responses（响应类DTO）
该目录下的类主要用于封装接口返回给前端的响应数据，标准化返回格式。

#### 1. `LoginResponse`
- **作用**：封装用户登录接口的返回结果
- **核心属性**：
  - `Success`：登录是否成功（bool）
  - `Token`：登录成功后返回的令牌（默认空字符串）
  - `UserInfo`：用户信息（`UserInfoResponse` 类型，可选）
  - `RequiresApproval`：是否需要审核（bool）
  - `Message`：提示信息（可选）

#### 2. `AdminUserResponse`
- **作用**：封装管理员用户信息的响应数据
- **核心属性**：
  - `Id`：管理员ID（int）
  - `Username`：登录用户名（默认空字符串）
  - `DisplayName`：显示名称（默认空字符串）
  - `IsActive`：是否激活（bool）
  - `CreatedAt`：创建时间（DateTime）

#### 3. `RegisterResult`
- **作用**：封装用户注册（微信注册）的返回结果
- **核心属性**：
  - `OpenId`：微信OpenId（默认空字符串）
  - `NickName`：昵称（默认空字符串）
  - `AvatarUrl`：头像URL（可选）
  - `Success`：注册是否成功（bool）
  - `Message`：提示信息
  - `UserId`：注册后生成的用户ID（int）

#### 4. `WeChatSessionResult`
- **作用**：封装微信登录接口（获取session）的返回结果
- **核心属性**（适配微信接口字段，通过`JsonPropertyName`映射）：
  - `OpenId`：微信用户唯一标识（可选）
  - `SessionKey`：微信会话密钥（可选）
  - `UnionId`：微信联合ID（可选）
  - `ErrorCode`：错误码（int）
  - `ErrorMessage`：错误信息（可选）

#### 5. `ApiResponse<T>`（泛型） & `ApiResponse`（非泛型）
- **作用**：通用接口响应封装，统一返回格式
- **泛型版属性**：
  - `Success`：操作是否成功（bool）
  - `Message`：提示信息（默认空字符串）
  - `Data`：返回的业务数据（泛型T，可选）
- **非泛型版属性**：
  - `Success`：操作是否成功（bool）
  - `Message`：提示信息（默认空字符串）
- **静态方法**：
  - `Ok()`：快速创建成功响应（默认消息“操作成功”）
  - `Fail()`：快速创建失败响应（默认消息“操作失败”）

#### 6. `UserInfoResponse`
- **作用**：封装通用用户信息的响应数据（兼容普通用户/管理员）
- **核心属性**：
  - `Id`：用户ID（int）
  - `NickName`：昵称（默认空字符串）
  - `AvatarUrl`：头像URL（可选）
  - `Role`：用户角色（字符串）
  - `DisplayName`：显示名称（用于管理员，默认空字符串）

#### 7. `StatisticsResponse` / `SeatUtilizationResponse` / `PopularSeatResponse` / `UserActivityResponse`
- **作用**：封装统计数据相关的响应数据（独立于 StatisticsService，存放在 Models/DTOs/Responses/StatisticsResponses.cs）
- **StatisticsResponse**：日/月统计（预约总数、活跃数、新用户数、待审核数、日期/年月）
- **SeatUtilizationResponse**：座位利用率（预约利用率字典、硬件实际使用率字典、整体利用率、分析天数等）
- **PopularSeatResponse**：热门座位（`List<PopularSeat>`，每条含座位号、预约次数、总时长）
- **UserActivityResponse**：用户活跃度（`List<UserActivity>`，每条含用户ID、预约次数、总时长、最后活跃时间）

### 二、Models/DTOs/Requests（请求类DTO）
该目录下的类主要用于接收前端传入的请求参数，标准化入参格式。

#### 1. `WechatLoginRequest`
- **作用**：接收微信登录的请求参数
- **核心属性**：
  - `Code`：微信临时登录凭证（默认空字符串）
  - `OpenId`：微信OpenId（可选，演示用）
  - `NickName`：微信昵称（默认空字符串）
  - `AvatarUrl`：微信头像URL（可选）

#### 2. `RegisterRequest`
- **作用**：接收用户注册（微信注册）的请求参数
- **核心属性**：
  - `OpenId`：微信OpenId
  - `NickName`：昵称
  - `AvatarUrl`：头像URL（可选）

#### 3. `CreateReservationRequest`
- **作用**：接收创建座位预约的请求参数
- **核心属性**：
  - `SeatNumber`：座位编号（int）
  - `StartTime`：预约开始时间（DateTime）
  - `EndTime`：预约结束时间（DateTime）

#### 4. `CancelReservationRequest`
- **作用**：接收取消预约的请求参数
- **核心属性**：
  - `AdminNote`：管理员取消时的备注（可选）

#### 5. `CheckConflictRequest`
- **作用**：接收座位冲突检测的请求参数（也用于加入候补时传参）
- **核心属性**：
  - `SeatNumber`：座位编号（int）
  - `StartTime`：开始时间（DateTime）
  - `EndTime`：结束时间（DateTime）
  - `ExcludeReservationId`：排除的预约ID（int?，检测时排除自身）

#### 6. `ApproveUserRequest`
- **作用**：接收管理员审核用户的请求参数
- **核心属性**：
  - `Approve`：是否通过审核（bool）
  - `Note`：审核备注（可选）

#### 7. `LoginRequest`
- **作用**：接收普通账号密码登录的请求参数
- **核心属性**：
  - `Username`：用户名（默认空字符串）
  - `Password`：密码（默认空字符串）

### 三、Models/Mqtt（MQTT相关模型）
该目录下的类用于封装MQTT通信的配置和消息格式（对接OneNet物联网平台）。

#### 1. `OneNetMqttMessage`
- **作用**：封装OneNet平台MQTT消息格式
- **核心属性**：
  - `DeviceId`：设备ID（默认值"vCRg326c00"）
  - `Data`：数据流消息列表（`List<DataStreamMessage>`，默认空列表）
- **嵌套类 `DataStreamMessage`**：
  - `Id`：数据流ID（默认空字符串）
  - `Value`：数据值（object类型，默认空对象）
  - `At`：数据时间戳（DateTime）

#### 2. `MqttOptions`
- **作用**：封装MQTT客户端的配置项
- **核心属性**：
  - `Server`：MQTT服务器地址（默认"mqtt://mqtt.heclouds.com"）
  - `Port`：端口（默认1883）
  - `ClientId`：客户端ID（默认"ESP8266"）
  - `Username`：用户名（通常为产品ID，默认"vCRg326c00"）
  - `Password`：密码（产品API Key/设备密钥，默认带签名的字符串）
  - `ReconnectDelaySeconds`：重连延迟（默认5秒）
  - `ProductId`：产品ID（默认"vCRg326c00"）
  - `DeviceName`：设备名称（默认"ESP8266"）
  - `AccessKey`：访问密钥（默认base64字符串）
  - `SubscribeTopics`：订阅的主题列表（默认空数组）

### 四、Models/Device（设备相关模型）
该目录下的类用于封装设备状态、设备与座位的映射关系。

#### 1. `DeviceStatus`
- **作用**：封装设备实时状态
- **核心属性**：
  - `DeviceId`：设备ID（默认空字符串）
  - `SeatNumber`：关联的座位编号（可选int）
  - `IsOccupied`：是否被占用（bool）
  - `LastUpdated`：最后更新时间（DateTime）
  - `AdditionalData`：额外数据（字典类型，存储温度/湿度等，默认空字典）

#### 2. `DeviceSeatMapping`
- **作用**：封装设备与座位的映射配置（可存储到数据库/配置文件）
- **核心属性**：
  - `DeviceId`：设备ID（默认空字符串）
  - `SeatNumber`：座位编号（int）
  - `Location`：位置信息（默认空字符串）

### 五、Models/Entities（数据库实体类）
该目录下的类是EF Core的实体模型，对应数据库表结构，包含数据验证注解。

#### 1. `AdminUser`
- **作用**：管理员用户实体（对应管理员表）
- **核心属性**（含数据验证）：
  - `Id`：主键（int，`[Key]`注解）
  - `Username`：登录用户名（必填，最大长度50）
  - `PasswordHash`：哈希后的密码（必填，最大长度255）
  - `DisplayName`：显示名称（必填，最大长度50）
  - `IsActive`：是否激活（必填，默认true）
  - `CreatedAt`：创建时间（必填，默认UTC当前时间）

#### 2. `SeatStatusHistory`
- **作用**：座位状态历史记录实体（对应座位状态日志表）
- **核心属性**（含数据验证）：
  - `Id`：主键（int，`[Key]`注解）
  - `SeatNumber`：座位编号（必填）
  - `IsOccupied`：是否被占用（必填）
  - `Timestamp`：状态变更时间（必填，默认UTC当前时间）

#### 3. `User`
- **作用**：普通用户（微信用户）实体（对应用户表）
- **核心属性**（含数据验证）：
  - `Id`：主键（int，`[Key]`注解）
  - `OpenId`：微信唯一标识（必填，最大长度100）
  - `NickName`：昵称（必填，最大长度50）
  - `Role`：用户角色（`UserRole`枚举，默认`Pending`）
  - `CreatedAt`：创建时间（必填，默认UTC当前时间）
  - `AvatarUrl`：头像URL（可选，最大长度1000）
  - `PasswordHash`：BCrypt 密码哈希（可选，最大长度255，微信用户可为空）
  - `ViolationCount`：违规次数计数（int，默认0，满3次冻结3天）
  - `GoodReservationStreak`：连续正常完成预约次数（int，默认0，每5次减1违规计数）
  - `SuspendedUntil`：账号预约权限封禁截止时间（DateTime?，可选）
  - `Reservations`：导航属性（该用户的所有预约，默认空列表）
- **枚举 `UserRole`**：
  - `Pending`：待审核
  - `User`：普通用户
  - `Admin`：管理员
  - `Rejected`：已拒绝

#### 4. `Reservation`
- **作用**：座位预约实体（对应预约表）
- **核心属性**（含数据验证+外键）：
  - `Id`：主键（int，`[Key]`注解）
  - `UserId`：关联用户ID（必填，`[ForeignKey("User")]`注解）
  - `SeatNumber`：座位编号（必填）
  - `StartTime`：预约开始时间（必填）
  - `EndTime`：预约结束时间（必填）
  - `Status`：预约状态（`ReservationStatus`枚举，默认`Active`）
  - `AdminNote`：管理员备注（可选）
  - `LeaveEndTime`：用户暂离截止时间（DateTime?，期间硬件自动释放判定暂停）
  - `CreatedAt`：创建时间（必填，默认UTC当前时间）
  - `User`：导航属性（所属用户）
- **枚举 `ReservationStatus`**：
  - `Active`：活跃/有效
  - `Completed`：已完成
  - `Cancelled`：用户取消
  - `ForceCancelled`：管理员强制取消

#### 5. `Notification`（新增）
- **作用**：用户通知消息实体（对应通知表）
- **核心属性**（含数据验证+外键）：
  - `Id`：主键（int，`[Key]`注解）
  - `UserId`：接收用户ID（必填，`[ForeignKey("User")]`）
  - `Title`：通知标题（必填，最大长度200）
  - `Content`：通知内容（必填，最大长度1000）
  - `Type`：通知类型（`NotificationType`枚举，默认`SystemMessage`）
  - `IsRead`：是否已读（bool，默认false）
  - `RelatedReservationId`：关联预约ID（int?，可选）
  - `CreatedAt`：创建时间（必填，默认UTC当前时间）
- **枚举 `NotificationType`**：
  - `ReservationStart`：预约即将开始
  - `TimeoutWarning`：暂离/超时预警
  - `ForceReleased`：预约被强制释放
  - `Suspended`：账号被冻结/封禁
  - `BanExpired`：封禁已解除
  - `SystemMessage`：系统消息
  - `WaitlistAvailable`：候补可用通知

#### 6. `WaitlistEntry`（新增）
- **作用**：候补排队实体（对应候补表）
- **核心属性**（含数据验证+外键）：
  - `Id`：主键（int，`[Key]`注解）
  - `UserId`：候补用户ID（必填，`[ForeignKey("User")]`）
  - `SeatNumber`：座位编号（必填）
  - `StartTime`：期望开始时间（必填）
  - `EndTime`：期望结束时间（必填）
  - `Status`：候补状态（`WaitlistStatus`枚举，默认`Waiting`）
  - `QueuePosition`：排队位置（同座位+时段内排序）
  - `NotifiedAt`：通知时间（DateTime?，可选）
  - `ConfirmDeadline`：确认截止时间（DateTime?，超时自动顺延）
  - `CreatedAt`：创建时间（必填，默认UTC当前时间）
- **枚举 `WaitlistStatus`**：
  - `Waiting`：排队中
  - `Notified`：已通知（等待确认）
  - `Confirmed`：已确认（转预约）
  - `Expired`：确认超时
  - `Cancelled`：用户主动取消

以下是对该`Services`文件夹中各类文件（及子目录）的功能定位与核心职责介绍（基于常见业务系统的服务层设计逻辑，结合文件名语义推导）：

## 5.Service

### 1. StatisticsService.cs
**核心职责**：数据统计分析相关的业务逻辑封装。
- 典型功能：
  - 各类业务数据的统计（如设备在线率统计、用户预约次数统计、设备使用时长统计等）；
  - 统计报表生成（日/周/月维度的统计数据聚合）；
  - 向其他模块提供统计结果查询接口（如给前端返回可视化图表所需的统计数据）；
  - 可能包含数据脱敏、统计规则配置（如阈值计算、异常数据过滤）等逻辑。

### 2. AuthService.cs
**核心职责**：身份认证与授权相关的核心业务逻辑。
- 典型功能：
  - 用户登录/登出的身份验证（如验证账号密码、生成/刷新Token、校验Token有效性）；
  - 权限校验（如验证用户是否拥有访问某接口/操作某设备的权限）；
  - 角色管理（如查询用户所属角色、判断角色对应的权限范围）；
  - 密码加密/解密、权限缓存（提升校验性能）等辅助逻辑。

### 3. DeviceStatusService.cs
**核心职责**：设备状态全生命周期的管理与维护。
- 典型功能：
  - 实时获取设备在线/离线/故障等状态（可能对接MQTT、TCP等物联网协议）；
  - 设备状态变更的监听与通知（如设备离线时触发告警、状态更新时同步到数据库）；
  - 设备状态的查询（如按设备ID/类型/区域查询状态、历史状态轨迹查询）；
  - 设备状态异常的处理（如故障标记、自动重试连接、故障恢复确认）。

### 4. UserService.cs
**核心职责**：用户基础信息与生命周期管理。
- 典型功能：
  - 用户基础信息CRUD（创建、查询、更新、删除，如注册、修改手机号、查询用户资料）；
  - 用户信息校验（如手机号/邮箱唯一性验证、用户状态激活/冻结）；
  - 关联业务（如用户与设备的绑定关系、用户预约记录关联查询）；
  - 用户数据的批量处理、导入导出等辅助操作。

### 5. ReservationService.cs
**核心职责**：预约业务的全流程管理。
- 典型功能：
  - 预约创建/取消/修改（如用户预约设备使用时间、校验预约冲突）；
  - 预约状态管理（待确认、已确认、已完成、已取消、超时未使用等状态流转）；
  - 违规处分逻辑：`ReleaseTimeoutReservationsAsync` 中座位超 30 分钟未感知落座 → 强制释放 → ViolationCount++，满 3 次 → SuspendedUntil 设为 3 天后；同时 GoodReservationStreak 归零；
  - 违规衰减逻辑：`UpdateExpiredReservationsAsync` 中正常完成预约 → GoodReservationStreak++，每连续 5 次 → ViolationCount -= 1（最小为 0）；
  - 预约拦截：`CreateReservationAsync` 首行检查用户 SuspendedUntil，若封禁未到期则拒绝预约；
  - 暂离保护：扫描时若 LeaveEndTime > 当前时间则跳过自动释放；
  - 通知集成：创建预约、强制释放、封禁时通过 INotificationService 发送通知；
  - 预约提醒（如预约开始前推送通知、超时预约自动取消）；
  - 预约记录查询（按用户、设备、时间范围查询预约历史）。
  - 所有时间使用 DateTime.UtcNow，确保时区一致性。

### 6. NotificationService.cs（新增）
**核心职责**：通知消息的全生命周期管理。
- 典型功能：
  - `CreateNotificationAsync`：创建通知（指定用户ID、标题、内容、通知类型、关联预约ID）；
  - `GetUserNotificationsAsync`：按用户查询通知（支持 unreadOnly 筛选，最多返回最近 100 条，按创建时间降序）；
  - `GetUnreadCountAsync`：获取用户未读通知数量；
  - `MarkAsReadAsync`：标记单条通知为已读（校验用户归属）；
  - `MarkAllAsReadAsync`：标记用户所有未读通知为已读。
- 通知触发点覆盖：预约创建成功、强制释放警告、冻结封禁通知、暂离即将超时（5分钟预警）、封禁到期解封通知、候补可用通知。

### 7. WaitlistService.cs（新增）
**核心职责**：候补排队机制的全流程管理。
- 典型功能：
  - `JoinWaitlistAsync`：加入候补队列（防重复排队，自动计算 QueuePosition = 当前最大位置 + 1）；
  - `ConfirmWaitlistAsync`：确认候补名额（校验状态为 Notified 且未超时，再次检查座位冲突 → 自动调用 ReservationService 创建预约）；
  - `CancelWaitlistAsync`：用户主动取消候补；
  - `GetUserWaitlistAsync`：查询用户在指定座位时段的候补状态；
  - `PromoteWaitlistAsync`：座位取消时推进队列 → 通知 Waiting 状态的第一位候补者，设 Status=Notified、ConfirmDeadline = now + 15 分钟。
- 依赖：INotificationService（发送通知）+ IReservationService（创建预约），设计上通过 Controller 层编排避免循环依赖。所有时间使用 DateTime.UtcNow。

### 8. ReservationMonitorBackgroundService（后台监控服务）
**核心职责**：定时后台任务，监控预约状态并执行自动化操作。
- 典型功能（每 1 分钟执行一次）：
  - **超时释放**（子任务1）：调用 `ReleaseTimeoutReservationsAsync`，座位超 30 分钟未感知落座 → 强制释放 + 违规处分；
  - **暂离预警**（子任务2）：检查 LeaveEndTime 在 5 分钟内到期的预约 → 发送 TimeoutWarning 通知提醒用户返回；
  - **封禁解封**（子任务3）：检查 SuspendedUntil 刚过期的用户 → 发送 BanExpired 通知告知权限恢复；
  - **候补超时**（子任务4）：扫描 Notified 状态且 ConfirmDeadline 已过的候补 → 标记为 Expired → 自动通知下一位候补者。
- 通过 IServiceScopeFactory 创建作用域获取 Scoped 服务实例（IReservationService、INotificationService、AppDbContext）。

### 9. Mqtt/ 子目录
**核心定位**：MQTT物联网协议相关的服务封装（物联网场景下的设备通信核心）。
- 典型内容：
  - MQTT客户端连接管理（如连接MQTT Broker、断线重连、客户端配置）；
  - 主题（Topic）订阅/发布逻辑（如订阅设备状态上报主题、发布设备控制指令）；
  - 消息解析与转发（如将设备上报的二进制/JSON消息解析为业务模型，转发给`DeviceStatusService`）；
  - MQTT消息的QoS配置、消息缓存、异常消息处理等。

### 10. Interfaces/ 子目录
**核心定位**：服务层接口定义（遵循”面向接口编程”设计原则）。
- 典型内容：
  - 所有服务类对应的接口：
    - `IAuthService`：认证服务接口（管理员登录、微信登录、Token 生成）
    - `IUserService`：用户服务接口（注册、审核、角色管理）
    - `IReservationService`：预约服务接口（创建/取消/查询/冲突检测/超时释放/暂离管理）
    - `IStatisticsService`：统计服务接口（日/月统计、座位利用率、热门座位、用户活跃度）
    - `IDeviceStatusService`：设备状态服务接口（设备状态获取、更新、座位映射）
    - `INotificationService`：通知服务接口（创建通知、查询列表、未读计数、标记已读）
    - `IWaitlistService`：候补服务接口（加入/确认/取消候补、查询状态、推进队列）
  - 接口中定义服务的核心方法签名（无具体实现，仅约定输入输出）；
  - 作用：解耦实现与调用、便于单元测试（Mock接口）、支持服务的多实现扩展。

### 补充说明
以上是基于”服务层（Service）”通用设计逻辑的推导，实际功能需结合代码实现确认，但核心职责与文件名强关联；这类服务层通常会依赖数据访问层（如Repository）、第三方SDK（如MQTT客户端、短信服务），并向上为控制器（Controller）/API层提供业务逻辑支撑。

### 补充说明
以上是基于“服务层（Service）”通用设计逻辑的推导，实际功能需结合代码实现确认，但核心职责与文件名强关联；这类服务层通常会依赖数据访问层（如Repository）、第三方SDK（如MQTT客户端、短信服务），并向上为控制器（Controller）/API层提供业务逻辑支撑。

## 6.Program.cs
**核心职责**： 项目主函数，负责注册所有应用服务、中间件和初始化逻辑。

### 注册的服务：
 - **数据库上下文**：`AppDbContext`（SQL Server，连接字符串来自配置）
 - **JWT 认证**：启动时校验 `Jwt:Key` 长度（≥16字符），配置 TokenValidationParameters（Issuer/Audience/SigningKey）
 - **速率限制**（`AddRateLimiter`）：
   - `AuthPolicy`：每分钟每 IP 最多 10 次（用于登录接口防暴力破解）
   - `ReservationPolicy`：每分钟每 IP 最多 20 次（用于预约创建接口）
 - **CORS**：从配置读取 `AllowedOrigins`（`*` 或逗号分隔域名），运行时根据值选择 `AllowAnyOrigin` 或 `WithOrigins`
 - **设备状态**：`IDeviceStatusService`（Singleton，内存中实时维护设备状态）
 - **后台服务**（`AddHostedService`）：
   - `OneNetPollingService`：每 3 秒轮询 OneNet 平台设备属性
   - `ReservationMonitorBackgroundService`：每 1 分钟执行超时释放、暂离预警、封禁解封、候补超时处理
 - **业务服务**（Scoped）：
   - `IAuthService` / `AuthService`
   - `IUserService` / `UserService`
   - `IReservationService` / `ReservationService`
   - `IStatisticsService` / `StatisticsService`
   - `INotificationService` / `NotificationService`
   - `IWaitlistService` / `WaitlistService`
 - **MQTT 服务**：已注释停用，相关类保留作参考
 - **数据库初始化**：运行时通过 `IServiceScopeFactory` 创建作用域，调用 `DbInitializer.InitializeAsync()` 创建默认管理员

### 中间件管道顺序：
`Swagger（开发环境）` → `HttpsRedirection` → `Cors("AllowAll")` → `UseRateLimiter` → `UseAuthentication` → `UseAuthorization` → `MapControllers`

## 7.Properties
* json配置文件
* lauchSettings.json：定义项目的启动配置（如环境变量、启动URL等）；
* serviceDependencies.json：定义服务依赖关系的配置文件（如服务名称、版本、依赖的其他服务等）；
* serviceDependenices.local.json：本地开发环境的服务依赖配置，覆盖或补充`serviceDependencies.json`中的内容（如本地数据库连接字符串、调试用的服务配置等）。


## 8.补充哈希值加密类Security
### PasswordHasher.cs
**核心职责**：提供密码哈希加密功能，确保用户密码安全存储。使用 **BCrypt 算法**（`BCrypt.Net-Next` NuGet 包）。
- 核心方法：
  - `Hash(string password)`：将明文密码进行 BCrypt 哈希处理，自动生成盐值（Salt）并嵌入哈希字符串中。空值或空白字符串会抛出 ArgumentException。
  - `Verify(string password, string storedHash)`：验证明文密码与已存储的 BCrypt 哈希是否匹配。参数为空时直接返回 false，异常捕获后返回 false。
- 设计特点：
  - BCrypt 是最新一代密码哈希算法，自适应计算成本（work factor），暴力破解成本高；
  - 每次哈希结果不同（盐值随机生成），即使相同密码也不能直接比对哈希字符串，必须使用 Verify 方法；
  - 替代了原先的 SHA256 无盐哈希方案，有效防止彩虹表攻击。
  - 在用户注册/修改密码时调用 `Hash()` 生成哈希值存储，在用户登录时调用 `Verify()` 验证密码正确性，确保系统不存储明文密码。
  

## 9.pages
**存放功能**： 微信小程序前端页面，包含用户交互界面（如登录页、预约页、个人中心等），与后端API进行数据交互。
### index
**核心职责**：小程序的主页，通常包含欢迎信息、功能入口（如预约、设备状态查询等），以及导航到其他页面的链接。
- 典型功能：
  - 显示欢迎信息和用户基本信息（如昵称、头像）；
  - 提供预约入口（如预约座位、查看预约记录）；
  - 提供设备状态查询入口（如查看座位占用状态）；
  - 提供个人中心入口（如修改个人信息、查看历史预约等）。

### login
**核心职责**：小程序的登录页面，负责用户身份认证（微信登录或账号密码登录）。
- 典型功能：
  - 微信登录按钮，调用微信登录API获取临时登录凭证（code），并发送到后端进行登录/注册处理；
  - 账号密码登录表单，允许用户输入用户名和密码进行登录，调用后端认证接口验证身份；
  - 登录成功后跳转到主页（index），并在页面上显示用户信息；
  - 提供错误提示（如登录失败、账号未审核等）以提升用户体验。

## 10.WeChatApp

* .gitkeep - 占位文件，确保Git版本控制系统跟踪该目录，即使目录中暂时没有其他文件。
* .app.js - 微信小程序的主入口文件，负责初始化小程序、处理全局数据、定义生命周期函数等核心逻辑。
