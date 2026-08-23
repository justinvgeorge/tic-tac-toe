const https = require('https');

module.exports = {
  '/api': {
    target: 'https://localhost:7221',
    secure: false,
    changeOrigin: true,
    agent: new https.Agent({ keepAlive: true }),
  },
};
