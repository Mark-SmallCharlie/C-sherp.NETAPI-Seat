const SESSION_KEY = 'seat_session';
const REGISTERED_KEY = 'seat_registered_users';
const TOKEN_KEY = 'seat_access_token';

function getSession() {
  try {
    return wx.getStorageSync(SESSION_KEY) || null;
  } catch (e) {
    return null;
  }
}

function setSession(session) {
  wx.setStorageSync(SESSION_KEY, session);
}

function getToken() {
  try {
    return wx.getStorageSync(TOKEN_KEY) || '';
  } catch (e) {
    return '';
  }
}

function setToken(token) {
  if (token) {
    wx.setStorageSync(TOKEN_KEY, token);
  } else {
    try {
      wx.removeStorageSync(TOKEN_KEY);
    } catch (e) {}
  }
}

function clearSession() {
  try {
    wx.removeStorageSync(SESSION_KEY);
  } catch (e) {}
  try {
    wx.removeStorageSync(TOKEN_KEY);
  } catch (e) {}
}

function getRegisteredUsers() {
  try {
    const list = wx.getStorageSync(REGISTERED_KEY);
    return Array.isArray(list) ? list : [];
  } catch (e) {
    return [];
  }
}

function saveRegisteredUsers(list) {
  wx.setStorageSync(REGISTERED_KEY, list);
}

module.exports = {
  SESSION_KEY,
  REGISTERED_KEY,
  TOKEN_KEY,
  getSession,
  setSession,
  clearSession,
  getToken,
  setToken,
  getRegisteredUsers,
  saveRegisteredUsers
};
