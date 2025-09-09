# 配置管理说明

## 开发/生产模式切换

本项目支持多种方式来设置开发/生产模式，优先级从高到低如下：

### 1. 环境变量（最高优先级）
在系统环境变量中设置：
- `ISDEVELOPMENT=true` - 设置为开发模式
- `ISDEVELOPMENT=false` - 设置为生产模式

### 2. 配置文件
修改 `appsettings.json` 文件：
```json
{
  "AppSettings": {
    "IsDevelopment": true,
    "ApiBaseUrl": "https://bm.miyinbot.com/api",
    "DevelopmentApiBaseUrl": "http://localhost:3000/api"
  }
}
```

### 3. 编译时定义（最低优先级）
- Debug 模式默认为开发模式
- Release 模式默认为生产模式

## 配置方式说明

### 方式1：修改配置文件（推荐用于开发）
1. 打开 `appsettings.json` 文件
2. 将 `IsDevelopment` 设置为 `true`（开发模式）或 `false`（生产模式）
3. 根据需要修改 `DevelopmentApiBaseUrl` 和 `ApiBaseUrl`

### 方式2：设置环境变量（推荐用于部署）
1. 在系统环境变量中添加 `ISDEVELOPMENT=true` 或 `ISDEVELOPMENT=false`
2. 重启应用程序

### 方式3：通过代码设置（高级用法）
```csharp
// 在程序启动时设置环境变量
Environment.SetEnvironmentVariable("ISDEVELOPMENT", "true");
```

## API地址配置

- **开发模式**：使用 `http://localhost:3000/api`（需要明确配置）
- **生产模式**：使用 `https://bm.miyinbot.com/api`（默认模式）

## 默认行为

- **无配置时**：默认使用生产环境地址 `https://bm.miyinbot.com/api`
- **用户端**：用户打开软件时默认连接生产服务器
- **开发时**：需要明确配置为开发模式才会使用本地地址

## 使用示例

```csharp
// 检查当前是否为开发模式
bool isDev = ConfigHelper.IsDevelopmentMode();

// 获取当前API基础URL
string apiUrl = ConfigHelper.GetApiBaseUrl();

// 获取特定配置值
string customValue = ConfigHelper.GetConfigValue("CUSTOM_KEY", "default_value");
```

## 注意事项

1. 配置文件 `appsettings.json` 会被复制到输出目录，确保在发布时包含此文件
2. 环境变量设置后需要重启应用程序才能生效
3. **默认行为**：用户端默认连接生产服务器，无需额外配置
4. **开发时**：需要明确设置 `IsDevelopment=true` 才会使用本地地址
5. 开发模式下，确保本地后端服务运行在 `http://localhost:3000/api`
6. 生产模式下，确保后端服务可访问 `https://bm.miyinbot.com`
