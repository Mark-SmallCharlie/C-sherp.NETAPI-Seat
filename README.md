# 智慧座位预约系统后端
--------

## 1.Controllers

### 控制器

## 2.Data

### 读取连接数据库

## 3.Models

### Device

### DTOs

### Entites

### Mqtt

- MQTT连接配置  
- OneNet连接配置
## 4.Services
### Interface接口
### Mqtt  
- 连接服务控制服务报错重新连服务
### 其他为微信小程序服务
## 5.Program.cs
### 项目启动main函数
## 6.appsetings.json
### json配置文件


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
  - 根据服务返回结果，返回成功（200）或失败（400）响应。

#### 设计特点：
- 未继承`BaseController`（响应格式未标准化，直接返回`BadRequest(ModelState)`/`Ok(result)`）。
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
- 区分管理员/普通用户登录逻辑，支持微信登录的“待审核”业务场景。

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
**核心定位**：处理座位预约的创建、取消、查询、冲突检测等，继承`BaseController`，需登录认证。  

#### 核心功能：
| 方法 | 路由 | 权限 | 功能 |
|------|------|------|------|
| `CreateReservation()` | `POST api/Reservation/create` | 登录用户 | 创建预约（关联用户ID，校验时间/座位冲突） |
| `CancelReservation()` | `POST api/Reservation/cancel/{reservationId}` | 登录用户（管理员可取消任意预约） | 取消预约，管理员可添加备注 |
| `GetMyReservations()` | `GET api/Reservation/my-reservations` | 登录用户 | 获取当前用户的所有预约 |
| `GetAllReservations()` | `GET api/Reservation/all-reservations` | 仅管理员 | 获取所有预约列表 |
| `GetActiveReservations()` | `GET api/Reservation/active-reservations` | 仅管理员 | 获取活跃（未结束）预约列表 |
| `CheckSeatConflict()` | `POST api/Reservation/check-conflict` | 登录用户 | 检测指定座位+时间段是否存在预约冲突 |

#### 附属DTO：
- `CancelReservationRequest`：取消预约请求（管理员备注）。
- `CheckConflictRequest`：冲突检测请求（座位号、开始/结束时间、排除的预约ID）。

#### 设计特点：
- 区分普通用户/管理员权限（管理员可操作所有预约，普通用户仅操作自己的）。
- 核心业务校验（预约冲突、预约存在性、操作权限）。
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
- 异常日志记录设备操作的失败原因。

---

### 8. `WeatherForecastController`（天气预测控制器）
**文件路径**：Controllers/WeatherForecastController.cs  
**核心定位**：ASP.NET Core默认生成的示例控制器，无业务意义。  

#### 核心功能：
- `Get()`：`GET /WeatherForecast`，返回随机生成的5条天气预测数据（日期、温度、天气描述）。

#### 设计特点：
- 未继承`BaseController`，使用默认`ControllerBase`。
- 无认证/授权限制，纯示例代码。

---

### 整体设计总结
1. **分层与复用**：通过`BaseController`封装公共逻辑，子类专注业务，符合DRY原则。
2. **权限控制**：区分匿名/登录用户/管理员，通过`[Authorize]`+角色校验实现精细化权限。
3. **标准化**：统一响应格式、日志记录、异常处理，提升代码可维护性。
4. **解耦**：依赖服务接口（如`IUserService`/`IReservationService`），而非具体实现，便于测试和扩展。
5. **业务适配**：贴合“预约系统”核心场景（用户审核、座位预约、设备管理、统计分析），覆盖C端（用户）和B端（管理员）需求。

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
- **OnModelCreating 方法**：重写基类方法，用于配置实体模型的额外规则：
  - 为 `User` 实体的 `OpenId` 字段配置**唯一索引**，确保每个用户的 OpenId 不重复；
  - 为 `Reservation` 实体配置**复合索引**（SeatNumber + StartTime + EndTime），优化座位预约冲突的查询效率；
  - 为 `AdminUser` 配置**种子数据**：初始化一个默认管理员账户（用户名 `admin`，密码哈希对应 `admin`，仅演示用）。

### 2. DbInitializer 类（Data/DbInitializer.cs）
`DbInitializer` 是静态工具类，用于数据库初始化，核心作用是确保数据库创建完成，并初始化基础数据（如默认管理员账户）。

#### 核心功能与结构：
- **静态方法 InitializeAsync**：异步初始化数据库的入口方法：
  - 调用 `context.Database.EnsureCreatedAsync()`：确保数据库存在（若不存在则创建，仅在数据库首次启动时生效）；
  - 检查 `AdminUsers` 表是否已有数据：若为空，则创建默认管理员账户并写入数据库；
- **私有静态方法 HashPassword**：密码哈希工具方法：
  - 使用 SHA256 算法对明文密码进行哈希处理；
  - 将哈希后的字节数组转换为十六进制字符串返回，避免明文密码存储（注：生产环境建议使用更安全的哈希方案，如 BCrypt）。

### 两类的核心协作关系
1. `AppDbContext` 定义了数据库的“结构规则”（表映射、索引、种子数据）；
2. `DbInitializer` 负责“数据初始化”（确保库创建、补充基础数据）；
3. 两者结合：既保证数据库表结构符合业务规则，又确保系统启动时拥有必要的初始数据（如默认管理员）。

### 补充说明
- 种子数据（`OnModelCreating` 中的 `HasData`）与 `DbInitializer` 的管理员初始化是两种不同的初始化方式：
  - `HasData` 是 EF Core 迁移（Migration）时生效，写入数据库架构；
  - `DbInitializer` 是程序运行时检查并写入，更灵活，适合动态初始化；
- 密码哈希仅为演示：SHA256 是不可逆哈希，但无“盐值”（Salt），生产环境需搭配盐值或使用 `PasswordHasher<T>` 等专用工具。

以下是对该代码库中各文件夹下类的详细介绍，按文件路径分类说明：

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
  - `Username`：用户名（默认空字符串）
  - `Password`：密码（默认空字符串）
  - `DisplayName`：显示名称（默认空字符串）

#### 4. `ApproveUserRequest`
- **作用**：接收管理员审核用户的请求参数
- **核心属性**：
  - `Approve`：是否通过审核（bool）
  - `Note`：审核备注（可选）

#### 5. `LoginRequest`
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
  - `AvatarUrl`：头像URL（可选，最大长度500）
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
  - `CreatedAt`：创建时间（必填，默认UTC当前时间）
  - `User`：导航属性（所属用户，默认新User实例）
- **枚举 `ReservationStatus`**：
  - `Active`：活跃/有效
  - `Completed`：已完成
  - `Cancelled`：用户取消  
  - `ForceCancelled`：管理员强制取消

以下是对该`Services`文件夹中各类文件（及子目录）的功能定位与核心职责介绍（基于常见业务系统的服务层设计逻辑，结合文件名语义推导）：

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
  - 预约提醒（如预约开始前推送通知、超时预约自动取消）；
  - 预约记录查询（按用户、设备、时间范围查询预约历史）。

### 6. Mqtt/ 子目录
**核心定位**：MQTT物联网协议相关的服务封装（物联网场景下的设备通信核心）。
- 典型内容：
  - MQTT客户端连接管理（如连接MQTT Broker、断线重连、客户端配置）；
  - 主题（Topic）订阅/发布逻辑（如订阅设备状态上报主题、发布设备控制指令）；
  - 消息解析与转发（如将设备上报的二进制/JSON消息解析为业务模型，转发给`DeviceStatusService`）；
  - MQTT消息的QoS配置、消息缓存、异常消息处理等。

### 7. Interfaces/ 子目录
**核心定位**：服务层接口定义（遵循“面向接口编程”设计原则）。
- 典型内容：
  - 上述所有服务类对应的接口（如`IStatisticsService`、`IAuthService`、`IDeviceStatusService`）；
  - 接口中定义服务的核心方法签名（无具体实现，仅约定输入输出）；
  - 可能包含接口的公共常量、枚举（如权限枚举、设备状态枚举）；
  - 作用：解耦实现与调用、便于单元测试（Mock接口）、支持服务的多实现扩展（如不同数据源的`UserService`实现）。

### 补充说明
以上是基于“服务层（Service）”通用设计逻辑的推导，实际功能需结合代码实现确认，但核心职责与文件名强关联；这类服务层通常会依赖数据访问层（如Repository）、第三方SDK（如MQTT客户端、短信服务），并向上为控制器（Controller）/API层提供业务逻辑支撑。