module.exports = {
  apps: [{
    name: 'brain-monitor-server',
    script: 'server.js',
    instances: 'max', // 使用所有CPU核心
    exec_mode: 'cluster', // 集群模式
    env: {
      NODE_ENV: 'development',
      PORT: 3111
    },
    env_production: {
      NODE_ENV: 'production',
      PORT: 3111
    },
    // 日志配置
    log_file: './logs/combined.log',
    out_file: './logs/out.log',
    error_file: './logs/error.log',
    log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
    
    // 自动重启配置
    watch: false, // 生产环境不监听文件变化
    max_memory_restart: '1G', // 内存超过1G时重启
    
    // 进程管理
    min_uptime: '10s', // 最小运行时间
    max_restarts: 10, // 最大重启次数
    
    // 健康检查
    health_check_grace_period: 3000,
    
    // 其他配置
    kill_timeout: 5000,
    listen_timeout: 3000,
    
    // 环境变量
    env_file: '.env'
  }]
};
