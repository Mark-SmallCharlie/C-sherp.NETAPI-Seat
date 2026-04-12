const config = require('./config.js');
const auth = require('./auth.js');

function joinUrl(base, path) {
  const b = base.replace(/\/$/, '');
  const p = path.startsWith('/') ? path : `/${path}`;
  return b + p;
}

function pickToken(data) {
  if (!data || typeof data !== 'object') return '';
  return (
    data.accessToken ||
    data.token ||
    data.Token ||
    data.access_token ||
    ''
  );
}

function pickUsername(data) {
  if (!data || typeof data !== 'object') return '';
  const u = data.user || data.User;
  return (
    data.username ||
    data.userName ||
    data.UserName ||
    u?.username ||
    u?.userName ||
    u?.UserName ||
    ''
  );
}

function pickRole(data) {
  if (!data || typeof data !== 'object') return 'user';
  const u = data.user || data.User;
  const r =
    data.role ||
    data.Role ||
    u?.role ||
    u?.Role ||
    'user';
  const s = String(r).toLowerCase();
  if (s === 'admin' || data.isAdmin === true || data.IsAdmin === true) {
    return 'admin';
  }
  return 'user';
}

function normalizeAuthPayload(data) {
  const token = pickToken(data);
  const username = pickUsername(data);
  let role = pickRole(data);
  if (!username && token) {
    return { token, username: '', role: 'user', raw: true };
  }
  if (username === 'admin' && !data.role && !data.Role) {
    role = 'admin';
  }
  return { token, username, role };
}

function request(options) {
  const {
    path,
    method = 'GET',
    data,
    needAuth = true,
    header: extraHeader = {}
  } = options;

  const header = Object.assign(
    { 'content-type': 'application/json' },
    extraHeader
  );

  if (needAuth !== false) {
    const t = auth.getToken();
    if (t) {
      header.Authorization = `Bearer ${t}`;
    }
  }

  const url = joinUrl(config.BASE_URL, path);

  return new Promise((resolve, reject) => {
    wx.request({
      url,
      method,
      data: data === undefined ? undefined : data,
      header,
      success(res) {
        const ok = res.statusCode >= 200 && res.statusCode < 300;
        if (ok) {
          resolve(res.data);
          return;
        }
        const body = res.data;
        const msg =
          (body && (body.message || body.Message || body.title || body.Title)) ||
          `请求失败 (${res.statusCode})`;
        reject({ statusCode: res.statusCode, message: msg, body: body });
      },
      fail(err) {
        reject({
          statusCode: 0,
          message: err.errMsg || '网络异常，请检查后端地址与本机/域名配置',
          err
        });
      }
    });
  });
}

function errToast(e, fallback) {
  const msg =
    (e && (e.message || (e.err && e.err.errMsg))) || fallback || '操作失败';
  wx.showToast({ title: String(msg).slice(0, 32), icon: 'none' });
}

async function login(username, password) {
  const data = await request({
    path: config.PATHS.login,
    method: 'POST',
    data: { username, password },
    needAuth: false
  });
  return normalizeAuthPayload(data);
}

async function register(username, password) {
  await request({
    path: config.PATHS.register,
    method: 'POST',
    data: { username, password },
    needAuth: false
  });
}

async function wechatLogin(code) {
  const data = await request({
    path: config.PATHS.wechatLogin,
    method: 'POST',
    data: { code },
    needAuth: false
  });
  return normalizeAuthPayload(data);
}

async function pingHealth() {
  await request({
    path: config.PATHS.health,
    method: 'GET',
    needAuth: false
  });
}

module.exports = {
  request,
  login,
  register,
  wechatLogin,
  pingHealth,
  normalizeAuthPayload,
  errToast
};
