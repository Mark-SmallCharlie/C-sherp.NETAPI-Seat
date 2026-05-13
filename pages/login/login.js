const auth = require('../../utils/auth.js');
const config = require('../../utils/config.js');
const api = require('../../utils/api.js');

Page({
  data: {
    showRegister: false,
    username: '',
    password: '',
    regUsername: '',
    regPassword: '',
    regPassword2: ''
  },

  onShow() {
    const session = auth.getSession();
    if (session) {
      wx.reLaunch({ url: '/pages/index/index' });
    }
  },

  onUsernameInput(e) {
    this.setData({ username: e.detail.value.trim() });
  },

  onPasswordInput(e) {
    this.setData({ password: e.detail.value });
  },

  onRegUsernameInput(e) {
    this.setData({ regUsername: e.detail.value.trim() });
  },

  onRegPasswordInput(e) {
    this.setData({ regPassword: e.detail.value });
  },

  onRegPassword2Input(e) {
    this.setData({ regPassword2: e.detail.value });
  },

  onToggleRegister() {
    this.setData({
      showRegister: !this.data.showRegister,
      password: '',
      regPassword: '',
      regPassword2: ''
    });
  },

  gotoHome() {
    wx.showToast({ title: '登录成功', icon: 'success' });
    setTimeout(() => {
      wx.reLaunch({ url: '/pages/index/index' });
    }, 400);
  },

  async onLogin() {
    const { username, password } = this.data;
    if (!username || !password) {
      wx.showToast({ title: '请填写用户名和密码', icon: 'none' });
      return;
    }

    if (config.USE_REMOTE_API) {
      wx.showLoading({ title: '登录中', mask: true });
      try {
        const norm = await api.login(username, password);
        if (norm.token) {
          auth.setToken(norm.token);   // 必须调用
        }
        const uname = norm.username || username;
        const role =
          norm.role === 'admin' || uname === 'admin' ? 'admin' : 'user';
        auth.setSession({
          username: uname,
          role,
          loginType: 'password',
          loginAt: Date.now()
        });
        wx.hideLoading();
        this.gotoHome();
      } catch (e) {
        wx.hideLoading();
        api.errToast(e, '登录失败');
      }
      return;
    }

    if (username === 'admin' && password === 'admin') {
      auth.setSession({
        username: 'admin',
        role: 'admin',
        loginType: 'password',
        loginAt: Date.now()
      });
      this.gotoHome();
      return;
    }

    const users = auth.getRegisteredUsers();
    const found = users.find(
      (u) => (u.username || u.Username) === username && u.password === password
    );
    if (!found) {
      wx.showToast({ title: '账号或密码错误', icon: 'none' });
      return;
    }

    auth.setSession({
      username,
      role: 'user',
      loginType: 'password',
      loginAt: Date.now()
    });
    this.gotoHome();
  },

  async onRegister() {
    const { regUsername, regPassword, regPassword2 } = this.data;
    if (!regUsername || !regPassword) {
      wx.showToast({ title: '请填写用户名和密码', icon: 'none' });
      return;
    }
    if (regPassword !== regPassword2) {
      wx.showToast({ title: '两次密码不一致', icon: 'none' });
      return;
    }
    if (regUsername.toLowerCase() === 'admin') {
      wx.showToast({ title: '该用户名不可用', icon: 'none' });
      return;
    }

    if (config.USE_REMOTE_API) {
      wx.showLoading({ title: '提交中', mask: true });
      try {
        const result = await api.register(regUsername, regPassword);   // 接收返回值
        wx.hideLoading();
        if (result && result.success) {
          wx.showToast({ title: result.message || '注册成功，请登录', icon: 'success' });
          this.setData({
            showRegister: false,
            username: regUsername,
            password: '',
            regUsername: '',
            regPassword: '',
            regPassword2: ''
          });
        } else{
          wx.showToast({title: result.message ||'注册成功，请登录',icon :'none'});  //有错误
        }
      } catch (e) {
        wx.hideLoading();
        console.error('注册请求异常', e );
        api.errToast(e, '后端暂未开放注册接口');
      }
      return;
    }

    const users = auth.getRegisteredUsers();
    if (users.some((u) => (u.username || u.Username) === regUsername)) {
      wx.showToast({ title: '用户名已存在', icon: 'none' });
      return;
    }

    users.push({ username: regUsername, password: regPassword });
    auth.saveRegisteredUsers(users);
    wx.showToast({ title: '注册成功，请登录', icon: 'success' });
    this.setData({
      showRegister: false,
      username: regUsername,
      password: '',
      regUsername: '',
      regPassword: '',
      regPassword2: ''
    });
  },

  onWechatLogin() {
    wx.showLoading({ title: '登录中', mask: true });
    //获取用户信息（需要用户授权）
    wx.getUserProfile({
      desc: '用于完善用户资料',
      success: (profile) => {
        const nickName = profile.userInfo.nickName;
        const avatarUrl = profile.userInfo.avatarUrl;
        wx.login({
          success: async (res) => {
            if (!res.code) {
              wx.hideLoading();
              wx.showToast({ title: '获取登录态失败', icon: 'none' });
              return;
            }
            if (config.USE_REMOTE_API) {
              try {
                const norm = await api.wechatLogin(res.code, nickName, avatarUrl);
                auth.setToken(norm.token || '');
                auth.setSession({
                  username: norm.username || nickName,
                  role: norm.role === 'admin' ? 'admin' : 'user',
                  loginType: 'wechat',
                  loginAt: Date.now()
                });
                wx.hideLoading();
                this.gotoHome();
              } catch (e) {
                wx.hideLoading();
                api.errToast(e, '微信登录失败');
              }
            } else {
              // 本地演示模式...
              wx.hideLoading();
              auth.setSession({
                username: '微信用户',
                role: 'user',
                loginType: 'wechat',
                code: res.code,
                loginAt: Date.now()
              });
              this.gotoHome();
            }
          },
          fail: () => { /* ... */ }
        });
      },
      fail: () => {
        wx.hideLoading();
        wx.showToast({ title: '需要授权用户信息', icon: 'none' });
      }
    });
  }
});
