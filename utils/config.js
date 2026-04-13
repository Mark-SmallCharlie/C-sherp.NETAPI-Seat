/**
 * 后端联调：请在微信开发者工具中开启
 * 「详情」→「本地设置」→ 勾选「不校验合法域名、web-view（业务域名）、TLS 版本以及 HTTPS 证书」
 * 否则 localhost / 自签证书请求会被拦截。
 *
 * 真机调试时手机无法访问你电脑上的 localhost，需改为局域网 IP（如 http://192.168.1.10:5090）或使用内网穿透。
 * 此处因为无线网络IP地址并不固定，
 */

/**
 * 是否走远程 API
 * 与本地 ASP.NET 联调时请改为 true；未启动后端时用 false 可继续本地演示。
 */
const USE_REMOTE_API = false;

/**
 * 与 launchSettings / Properties/launchSettings.json 中地址一致
 * HTTPS 开发证书：若失败可改用下面 HTTP 端口
 */
const BASE_URL = 'https://192.168.107.103:7005';

// const BASE_URL = 'http://localhost:5090';

const API_PREFIX = '/api';

/** 按你 C# 项目里的 Controller/Minimal API 路由修改下列路径 */
const PATHS = {
  login: `${API_PREFIX}/auth/login`,
  register: `${API_PREFIX}/auth/register`,
  wechatLogin: `${API_PREFIX}/auth/wechat`,
  seats: `${API_PREFIX}/seats`,
  health: `${API_PREFIX}/health`
};

module.exports = {
  USE_REMOTE_API,
  BASE_URL,
  API_PREFIX,
  PATHS
};
