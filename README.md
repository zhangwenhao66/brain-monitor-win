# 脑镜BrainMirror - 脑电波监测与健康风险评估系统

这是一个完整的脑电波检测与健康风险评估系统，采用前后端分离架构，为医疗机构提供专业的认知健康评估服务。

## 📋 项目概述

脑镜BrainMirror是一款基于脑电波分析技术的医疗健康软件，集成了MOCA和MMSE认知评估量表，能够全面分析用户的认知功能状态，为早期发现阿尔茨海默病（AD）等认知障碍提供科学依据。

系统采用现代化的技术栈：
- **前端**: .NET 8.0 + WPF 桌面应用
- **后端**: Node.js + Express.js RESTful API
- **数据库**: MySQL 关系型数据库
- **通信**: HTTP/HTTPS + JWT 身份验证

## 🏗️ 系统架构

### 前端架构（WPF桌面应用）
- **技术栈**: .NET 8.0, WPF, C#, ModernWpfUI
- **主要功能**: 用户界面、数据展示、本地数据处理
- **通信方式**: HTTP客户端调用后端API
- **部署方式**: Windows桌面应用程序

### 后端架构（Node.js服务器）
- **技术栈**: Node.js, Express.js, MySQL2, JWT, bcryptjs
- **主要功能**: API服务、业务逻辑处理、数据持久化
- **安全特性**: JWT身份验证、密码加密、CORS防护、请求频率限制
- **部署方式**: 独立服务器，可使用PM2进程管理

### 数据库架构（MySQL）
- **数据库名**: brain_mirror
- **表结构**: 机构表、工作人员表、测试者表、测试记录表、测试结果表
- **特性**: 支持多机构多租户、数据关联完整性、外键约束

## 🔧 核心功能模块

### 1. 机构管理模块
- **机构注册**: 新机构注册，设置机构基本信息
- **机构登录**: 机构ID和密码验证登录
- **机构信息**: 联系人、联系电话、机构地址管理

### 2. 工作人员管理模块
- **工作人员注册**: 工号、姓名、账号密码等信息录入
- **工作人员登录**: 多机构环境下账号唯一性验证
- **人员信息管理**: 科室、职位、联系方式管理
- **权限控制**: 基于机构的访问权限控制

### 3. 测试者管理模块
- **测试者信息录入**: ID、姓名、年龄、性别、联系电话
- **测试者列表**: 分页查询、搜索功能
- **信息验证**: 数据完整性校验
- **隐私协议**: 用户隐私协议确认

### 4. 脑电波测试模块
- **设备连接**: 串口通信，脑电波设备连接管理
- **实时监测**: Theta、Alpha、Beta波段数据实时显示
- **测试流程**: 睁眼/闭眼状态分别测试
- **量表评估**: MOCA和MMSE评分录入

### 5. 健康报告模块
- **AD风险评估**: 基于脑电波数据计算AD风险值
- **大脑年龄计算**: 相对实际年龄的大脑年龄评估
- **结果可视化**: 脑电波图表展示
- **健康建议**: 个性化健康指导建议

### 6. 测试历史管理模块
- **历史记录查询**: 分页查看测试历史
- **数据筛选**: 按测试者、时间等条件筛选
- **报告导出**: 测试结果数据导出功能

## 📁 项目结构详解

```
brain-monitor-win/
├── BrainMonitor/                          # WPF前端项目
│   ├── BrainMonitor.csproj               # 项目配置文件
│   ├── App.xaml/App.xaml.cs              # 应用程序入口
│   ├── Assets/                           # 静态资源文件
│   │   ├── icon.ico/png                  # 应用图标
│   │   └── audio/                        # 音频资源
│   ├── Views/                            # UI界面层
│   │   ├── InstitutionLoginWindow.xaml   # 机构登录界面
│   │   ├── MedicalStaffWindow.xaml       # 工作人员操作界面
│   │   ├── TesterInfoWindow.xaml         # 测试者信息录入
│   │   ├── TestWindow.xaml               # 脑电波测试界面
│   │   ├── ReportWindow.xaml             # 健康报告界面
│   │   ├── TestHistoryWindow.xaml        # 测试历史界面
│   │   └── ModernMessageBoxWindow.xaml   # 自定义消息框
│   ├── Services/                         # 业务服务层
│   │   ├── HttpService.cs                # HTTP客户端服务
│   │   ├── BrainwaveDataProcessor.cs     # 脑电波数据处理
│   │   ├── TesterService.cs              # 测试者服务
│   │   └── TestHistoryService.cs         # 测试历史服务
│   ├── Models/                           # 数据模型
│   │   └── TestDataModels.cs             # 测试数据模型
│   ├── Converters/                       # 数据转换器
│   │   └── DateTimeConverter.cs          # 日期时间转换
│   ├── SDK/                              # 第三方SDK集成
│   │   └── BrainMonitorSDK.cs            # 脑电波设备SDK
│   ├── lib/                              # 本地库文件
│   └── demo/                             # SDK示例代码
├── server/                               # Node.js后端服务器
│   ├── package.json                      # 项目依赖配置
│   ├── server.js                         # 服务器入口文件
│   ├── ecosystem.config.js               # PM2配置文件
│   ├── config/                           # 配置文件
│   │   ├── database.js                   # 数据库配置
│   │   └── jwt.js                        # JWT配置
│   ├── middleware/                       # 中间件
│   │   └── auth.js                       # 身份验证中间件
│   ├── routes/                           # API路由
│   │   ├── auth.js                       # 认证相关API
│   │   ├── testers.js                    # 测试者管理API
│   │   ├── test-records.js               # 测试记录API
│   │   ├── brainwave-data.js             # 脑电波数据API
│   │   └── test-results.js               # 测试结果API
│   ├── database/                         # 数据库相关
│   │   └── schema.sql                    # 数据库表结构
│   ├── scripts/                          # 脚本文件
│   │   └── init-db.js                    # 数据库初始化
│   ├── test-api.http                     # API测试文件
│   └── README.md                         # 服务器说明文档
├── BrainMonitor.sln                      # Visual Studio解决方案
└── README.md                             # 项目总README
```

## 🔄 前后端协作流程

### 1. 用户认证流程
```
前端(WPF) → 后端API → 数据库
    ↓           ↓         ↓
机构登录 → /api/auth/institution/login → institutions表
医护登录 → /api/auth/medical-staff/login → medical_staff表
```

### 2. 测试流程
```
1. 测试者录入 → 前端表单 → POST /api/testers → testers表
2. 开始测试 → 设备连接 → 实时数据处理 → 本地存储
3. 测试完成 → 上传数据 → POST /api/test-records → test_records表
4. 生成报告 → 计算结果 → POST /api/test-results → test_results表
```

### 3. 数据流转
```
脑电波设备 → SDK → WPF应用 → HttpService → REST API → MySQL数据库
         ↑           ↑         ↑           ↑           ↑
    原始数据 → 数据处理 → UI显示 → 网络传输 → 业务逻辑 → 持久化存储
```

## 🚀 快速开始

### 前端部署（Windows）
```bash
# 1. 安装.NET 8.0 SDK
# 下载地址: https://dotnet.microsoft.com/download/dotnet/8.0

# 2. 克隆项目
git clone https://github.com/your-repo/brain-monitor-win.git
cd brain-monitor-win

# 3. 打开解决方案
start BrainMonitor.sln

# 4. 使用Visual Studio 2022编译运行
# 或使用命令行:
dotnet build
dotnet run --project BrainMonitor
```

### 后端部署
```bash
# 1. 安装Node.js (版本 >= 16.0.0)
# 下载地址: https://nodejs.org/

# 2. 进入服务器目录
cd server

# 3. 安装依赖
npm install

# 4. 配置环境变量
cp .env.example .env
# 编辑.env文件，配置数据库连接等信息

# 5. 初始化数据库
npm run init-db

# 6. 启动服务器
npm start
# 或开发模式:
npm run dev
# 或生产模式:
npm run pm2:start
```

### 数据库配置
```sql
-- 创建数据库
CREATE DATABASE brain_mirror CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 运行表结构脚本
source server/database/schema.sql;
```

## ⚙️ 环境配置

### 后端环境变量 (.env)
```env
# 服务器配置
NODE_ENV=development
PORT=3000

# 数据库配置
DB_HOST=localhost
DB_PORT=3306
DB_USER=root
DB_PASSWORD=your_password
DB_NAME=brain_mirror

# JWT配置
JWT_SECRET=your_jwt_secret_key
JWT_EXPIRES_IN=24h

# CORS配置
CORS_ORIGIN=http://localhost:8080
```

### 前端配置
```csharp
// HttpService.cs 中的BaseUrl配置
private const string BaseUrl = "https://bm.miyinbot.com/api";
// 开发环境可改为:
// private const string BaseUrl = "http://localhost:3000/api";
```

## 🔒 安全特性

### 身份验证安全
- JWT Token身份验证
- 密码bcrypt加密存储
- 多机构隔离访问控制
- Token过期自动失效

### 数据传输安全
- HTTPS加密传输
- 请求频率限制(Rate Limiting)
- CORS跨域防护
- 输入数据验证和清理

### 数据存储安全
- 敏感数据加密存储
- 数据库访问权限控制
- SQL注入防护
- 数据备份和恢复机制

## 📊 API接口文档

### 认证接口
- `POST /api/auth/institution/login` - 机构登录
- `POST /api/auth/medical-staff/login` - 工作人员登录
- `POST /api/auth/institution/register` - 机构注册
- `POST /api/auth/medical-staff/register` - 工作人员注册

### 测试者管理接口
- `GET /api/testers` - 获取测试者列表
- `POST /api/testers` - 创建测试者
- `GET /api/testers/:id` - 获取测试者详情
- `PUT /api/testers/:id` - 更新测试者信息

### 测试记录接口
- `GET /api/test-records` - 获取测试记录列表
- `POST /api/test-records` - 创建测试记录
- `GET /api/test-records/:id` - 获取测试记录详情
- `PUT /api/test-records/:id` - 更新测试记录

### 脑电波数据接口
- `POST /api/brainwave-data/upload` - 上传脑电波数据
- `GET /api/brainwave-data/:testRecordId` - 获取脑电波数据

## 🧪 测试与调试

### API测试
```bash
# 使用内置测试脚本
cd server
npm run test:login

# 或使用test-api.http文件测试
```

### 前端调试
- 使用Visual Studio调试器
- 查看输出窗口日志
- 使用Fiddler抓包分析网络请求

### 数据库调试
```sql
-- 查看表结构
DESCRIBE institutions;
DESCRIBE medical_staff;
DESCRIBE testers;

-- 查看数据
SELECT * FROM institutions LIMIT 10;
SELECT * FROM test_records ORDER BY created_at DESC LIMIT 10;
```

## 📈 性能优化

### 前端优化
- WPF UI虚拟化处理大量数据
- 异步数据加载避免界面冻结
- 内存管理优化大文件处理
- 响应式布局适配不同分辨率

### 后端优化
- 数据库连接池管理
- API请求缓存机制
- 文件上传分块处理
- PM2集群模式部署

### 数据库优化
- 索引优化查询性能
- 分页查询避免大数据量
- 定期清理历史数据
- 读写分离架构支持

## 🔧 维护与扩展

### 定期维护任务
- 数据库备份
- 日志文件清理
- 依赖包更新
- 安全补丁应用

### 扩展开发
- 新设备SDK集成
- 报告模板定制
- 多语言界面支持
- 数据导出功能增强

## 📞 技术支持

如需技术支持或遇到问题，请联系开发团队：
- 邮箱: support@brainmirror.com
- 文档: [在线文档链接]
- 社区: [社区论坛链接]

## 📄 开源协议

本项目采用 MIT 协议开源，详见 LICENSE 文件。

---

**脑镜BrainMirror** - 为医疗健康事业贡献力量，让科技守护认知健康！ 