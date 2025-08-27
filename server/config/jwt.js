// JWT配置
module.exports = {
    JWT_SECRET: process.env.JWT_SECRET || 'your_jwt_secret_key_here_change_in_production',
    JWT_EXPIRES_IN: process.env.JWT_EXPIRES_IN || '24h'
};
