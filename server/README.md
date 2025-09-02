# Brain Monitor Backend Server

脑电波监测系统后端服务，基于 Node.js + Express + MySQL 构建。

## 功能特性

### 🔐 认证系统
- **机构登录**: 支持机构ID和密码验证，也允许空输入直接登录
- **医护人员注册**: 完整的医护人员注册流程，包含密码加密
- **医护人员登录**: JWT token认证，支持会话管理
- **权限控制**: 基于角色的访问控制，确保数据安全

### 👥 测试者管理
- **测试者列表**: 获取当前医护人员的测试者列表
- **添加测试者**: 创建新的测试者记录
- **搜索功能**: 支持按ID或姓名搜索测试者
- **权限验证**: 确保医护人员只能访问自己的测试者

### 📊 测试记录管理
- **创建测试**: 为测试者创建新的测试记录
- **状态更新**: 支持测试状态、评分、握力等信息的更新
- **测试历史**: 查看测试者的完整测试历史记录
- **数据完整性**: 使用事务确保数据一致性

### 🧠 脑电波数据管理
- **数据上传**: 支持批量上传脑电波数据
- **数据存储**: 高效存储大量脑电波数据点
- **数据类型**: 支持睁眼、闭眼等不同测试类型的数据
- **实时处理**: 支持实时数据流处理

### 📈 测试结果管理
- **结果保存**: 保存各种脑电波分析结果
- **多类型结果**: 支持睁眼、闭眼、综合等不同类型的结果
- **数据导出**: 支持CSV格式的数据导出
- **统计分析**: 提供数据统计和分析功能

## 技术架构

### 后端技术栈
- **Node.js**: 运行环境
- **Express**: Web框架
- **MySQL**: 关系型数据库
- **JWT**: 身份认证
- **bcryptjs**: 密码加密
- **Helmet**: 安全中间件
- **CORS**: 跨域支持

### 数据库设计
- **institutions**: 机构信息表
- **medical_staff**: 医护人员表
- **testers**: 测试者表
- **test_records**: 测试记录表
- **brainwave_data**: 脑电波数据表
- **test_results**: 测试结果表

## 快速开始

### 环境要求
- Node.js >= 14.0.0
- MySQL >= 8.0.0
- npm 或 yarn

### 安装步骤

1. **克隆项目**
```bash
git clone <repository-url>
cd brain-monitor-server
```

2. **安装依赖**
```bash
npm install
```

3. **环境配置**
```bash
cp env.example .env
# 编辑 .env 文件，配置数据库连接信息
```

4. **数据库初始化**
```bash
# 使用 MySQL 客户端执行 database/schema.sql
mysql -u root -p < database/schema.sql
```

5. **启动服务**
```bash
# 开发模式
npm run dev

# 生产模式
npm start
```

### 环境变量配置

```env
# 数据库配置
DB_HOST=localhost
DB_PORT=3306
DB_USER=root
DB_PASSWORD=your_password
DB_NAME=brain_monitor

# JWT配置
JWT_SECRET=your_jwt_secret_key_here
JWT_EXPIRES_IN=24h

# 服务器配置
PORT=3000
NODE_ENV=development
```

## API 接口文档

### 认证接口

#### 机构登录
```
POST /api/auth/institution/login
Content-Type: application/json

{
  "institutionId": "机构ID",
  "password": "密码"
}
```

#### 医护人员注册
```
POST /api/auth/medical-staff/register
Content-Type: application/json

{
  "staffId": "工号",
  "name": "姓名",
  "account": "账号",
  "password": "密码",
  "phone": "电话",
  "department": "科室",
  "position": "职位",
  "institutionId": "机构ID"
}
```

#### 医护人员登录
```
POST /api/auth/medical-staff/login
Content-Type: application/json

{
  "account": "账号",
  "password": "密码"
}
```

### 测试者管理接口

#### 获取测试者列表
```
GET /api/testers/my-testers?search=关键词
Authorization: Bearer <token>
```

#### 添加测试者
```
POST /api/testers
Authorization: Bearer <token>
Content-Type: application/json

{
  "testerId": "测试者ID",
  "name": "姓名",
  "age": "年龄",
  "gender": "性别",
  "phone": "电话"
}
```

### 测试记录接口

#### 创建测试记录
```
POST /api/test-records
Authorization: Bearer <token>
Content-Type: application/json

{
  "testerId": "测试者ID"
}
```

#### 更新测试状态
```
PUT /api/test-records/:recordId/status
Authorization: Bearer <token>
Content-Type: application/json

{
  "testStatus": "已完成",
  "testEndTime": "2024-01-01T12:00:00Z",
  "mocaScore": 25,
  "mmseScore": 28,
  "gripStrength": 35.5
}
```

### 脑电波数据接口

#### 上传脑电波数据
```
POST /api/brainwave-data/upload
Authorization: Bearer <token>
Content-Type: application/json

{
  "testRecordId": "测试记录ID",
  "dataType": "睁眼",
  "dataPoints": [1.2, 1.5, 1.8, ...]
}
```

### 测试结果接口

#### 保存测试结果
```
POST /api/test-results
Authorization: Bearer <token>
Content-Type: application/json

{
  "testRecordId": "测试记录ID",
  "resultType": "睁眼",
  "alphaPower": 0.15,
  "betaPower": 0.25,
  "thetaPower": 0.10,
  "deltaPower": 0.05,
  "gammaPower": 0.30,
  "totalPower": 0.85,
  "dominantFrequency": 12.5,
  "coherenceScore": 0.75,
  "attentionScore": 0.80,
  "relaxationScore": 0.70
}
```

## 数据流程

### 测试流程
1. **机构登录** → 获取机构信息
2. **医护人员登录** → 获取JWT token
3. **选择测试者** → 从测试者列表中选择
4. **开始测试** → 创建测试记录
5. **数据采集** → 实时上传脑电波数据
6. **测试完成** → 更新测试状态和评分
7. **结果分析** → 保存测试分析结果

### 数据安全
- 所有敏感操作都需要JWT认证
- 医护人员只能访问自己的数据
- 使用事务确保数据一致性
- 密码使用bcrypt加密存储

## 部署说明

### 生产环境部署
1. 设置 `NODE_ENV=production`
2. 配置生产数据库连接
3. 使用PM2或Docker进行进程管理
4. 配置反向代理（Nginx）
5. 启用HTTPS

### 性能优化
- 数据库连接池优化
- 查询索引优化
- 数据分页处理
- 缓存策略

## 故障排除

### 常见问题
1. **数据库连接失败**: 检查数据库配置和网络连接
2. **JWT验证失败**: 检查token格式和密钥配置
3. **权限不足**: 确认用户身份和访问权限
4. **数据上传失败**: 检查数据格式和大小限制

### 日志查看
```bash
# 查看应用日志
npm run dev

# 查看数据库日志
tail -f /var/log/mysql/error.log
```

## 贡献指南

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 推送到分支
5. 创建 Pull Request

## 许可证

MIT License

## 联系方式

- 项目维护者: Brain Monitor Team
- 邮箱: support@brainmonitor.com
- 项目地址: https://github.com/brainmonitor/server

