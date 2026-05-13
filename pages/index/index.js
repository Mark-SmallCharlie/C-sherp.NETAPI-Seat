const auth = require('../../utils/auth.js');
const config = require('../../utils/config.js');
const seatApi = require('../../utils/seatApi.js');
const api = require('../../utils/api.js');

const SEAT_STORAGE_KEY = 'seat_demo_slots';

const SEAT_DEFS = [
  { id: 'A1' },
  { id: 'A2' },
  { id: 'B1' },
  { id: 'B2' }
];

function normalizeSlot(entry) {
  if (entry && typeof entry === 'object' && 'reservedBy' in entry) {
    return {
      reservedBy: entry.reservedBy || null,
      inUseBy: entry.inUseBy || null
    };
  }
  if (typeof entry === 'string' && entry) {
    return { reservedBy: entry, inUseBy: null };
  }
  return { reservedBy: null, inUseBy: null };
}

function loadSlotsLocal() {
  try {
    const raw = wx.getStorageSync(SEAT_STORAGE_KEY);
    if (Array.isArray(raw) && raw.length === 4) {
      return raw.map(normalizeSlot);
    }
  } catch (e) {}
  return Array.from({ length: 4 }, () => ({
    reservedBy: null,
    inUseBy: null
  }));
}

function saveSlotsLocal(slots) {
  wx.setStorageSync(SEAT_STORAGE_KEY, slots);
}

function getMyReservedIndex(slots, username) {
  for (let i = 0; i < slots.length; i++) {
    if (slots[i].reservedBy === username) {
      return i;
    }
  }
  return -1;
}

Page({
  data: {
    displayName: '',
    seats: [],
    isAdmin: false,
    toastVisible: false,
    toastMsg: '',
    toastIcon: '',
    utilizationPercent: 0,
    usedSeats: 0,
    totalSeats: 4,
    showAuditModal: false,      // 是否显示审核弹窗
    pendingUsers: [],           // 待审核用户列表
    // 预约弹窗相关
    showReserveModal: false,
    selectedSeat: null,
    startTime: '',
    endTime: '',
    // 我的预约列表
    showMyReservations: false,
    myReservations: []
  },
  // 获取待审核用户列表
  async loadPendingUsers() {
    try {
      const res = await api.request({
        path: '/api/User/pending-users',
        method: 'GET'
      });
      this.setData({ pendingUsers: res.data || [] });
    } catch (e) {
      console.error('获取待审核用户失败', e);
    }
  },

// 打开审核弹窗
onAuditTap() {
  this.loadPendingUsers();
  this.setData({ showAuditModal: true });
},

// 跳转数据统计页
onStatsTap() {
  wx.navigateTo({ url: '/pages/statistics/statistics' });
},

// 关闭弹窗
onCloseModal() {
  this.setData({ showAuditModal: false });
},

// 审核用户
async onApproveUser(e) {
  const userId = e.currentTarget.dataset.id;
  const approve = e.currentTarget.dataset.approve === 'true';
  try {
    await api.request({
      path: `/api/User/approve-user/${userId}`,
      method: 'POST',
      data: { approve, note: '管理员审核' }
    });
    wx.showToast({ title: approve ? '已通过' : '已拒绝', icon: 'success' });
    this.loadPendingUsers(); // 刷新列表
  } catch (e) {
    wx.showToast({ title: '操作失败', icon: 'none' });
  }
},
  showInlineTip(msg, icon) {
    if (this._tipTimer) {
      clearTimeout(this._tipTimer);
    }
    const toastIcon = icon === 'cross' ? 'cross' : '';
    this.setData({ toastVisible: true, toastMsg: msg, toastIcon });
    this._tipTimer = setTimeout(() => {
      this.setData({ toastVisible: false, toastMsg: '', toastIcon: '' });
      this._tipTimer = null;
    }, 2200);
  },

  async getSlotsForEdit() {
    if (config.USE_REMOTE_API) {
      return seatApi.fetchSlots();
    }
    return loadSlotsLocal();
  },

  async commitSlots(slots) {
    if (config.USE_REMOTE_API) {
      await seatApi.saveSlots(slots);
      await this.refreshSeats();
    } else {
      saveSlotsLocal(slots);
      await this.refreshSeats(slots);
    }
  },

  onShow() {
    const session = auth.getSession();
    if (session && session.role === 'admin') {
      this.setData({ isAdmin: true });
    } else {
      this.setData({ isAdmin: false });
    }
    if (!session) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    const normalizedSession = {
      ...session,
      username: session.username || session.Username || '',
      role: session.role || session.Role || 'user'
    };
    this._session = normalizedSession;
    this.setData({
      displayName: normalizedSession.username,
      isAdmin: normalizedSession.role === 'admin'
    });

    // 新增：初始化本地座位数据（若格式错误则重置）
  const rawSlots = wx.getStorageSync(SEAT_STORAGE_KEY);
  if (!Array.isArray(rawSlots) || rawSlots.length !== 4) {
    saveSlotsLocal([
      { reservedBy: null, inUseBy: null },
      { reservedBy: null, inUseBy: null },
      { reservedBy: null, inUseBy: null },
      { reservedBy: null, inUseBy: null }
    ]);
  }
    this.refreshSeats();
    if (config.USE_REMOTE_API && !this._pollTimer) {
      this._pollTimer = setInterval(() => {
        this.refreshSeats();
      }, 3000);
    }
  },

  async refreshSeats() {
    const session = this._session;
    if (!session) return;

    let list = [];
    try {
      // 这里的 fetchSlots 已经在之前的修改中合并了后端的“预约记录”和“硬件占用状态”
      list = await seatApi.fetchSlots();
    } catch (e) {
      api.errToast(e, '座位数据加载失败');
      return;
    }

    const isAdmin = session.role === 'admin';
    const totalSeats = 4; // 固定4个座位演示
    // 统计被预约或正在被使用的座位数量
    const usedSeats = list.filter((s) => s.reservedBy || s.inUseBy).length;
    const utilizationPercent = totalSeats ? Math.round((usedSeats / totalSeats) * 100) : 0;

    // 固定的四个座位编号
    const SEAT_DEFS = [{ id: 'A1' }, { id: 'A2' }, { id: 'B1' }, { id: 'B2' }];

    const seats = SEAT_DEFS.map((def, index) => {
      const slot = list[index];
      const { reservedBy, inUseBy } = slot;
      
      let statusClass = 'seat-free';
      let stateText = '空闲';
      
      // 判定是否是我自己的预约（兼容管理员用户名为admin，但后端返回昵称为'系统管理员'的情况）
      const isMyReservation = reservedBy === session.username || (isAdmin && reservedBy === '系统管理员');
      const isMyInUse = inUseBy === session.username || (isAdmin && inUseBy === '系统管理员');

      
      if (inUseBy) {
        statusClass = 'seat-in-use'; // 红色：正在使用 (云平台雷达检测)
        if (isMyInUse) {
          stateText = '我使用中';
        } else if (isAdmin) {
          stateText = `使用中(${inUseBy})`;
        } else {
          stateText = '使用中';
        }
      } else if (reservedBy) {
        if (isMyReservation) {
          statusClass = 'seat-mine'; // 绿色：我的预约
          stateText = '我的预约';
        } else if (isAdmin) {
          statusClass = 'seat-other'; // 橙色：他人预约
          stateText = `已预约(${reservedBy})`;
        } else {
          statusClass = 'seat-other'; // 橙色：他人预约
          stateText = '已预约';
        }
      }

      return {
        id: def.id,
        statusClass,
        stateText,
        // 连接后端后，“开始使用”和“结束使用”按钮交由硬件自动化，不再显示前端按钮
        showStartUse: false, 
        showEndUse: false    
      };
    });

    this.setData({ seats, utilizationPercent, usedSeats, totalSeats });
    this._slots = list;
  },
  
  //Deepseek生成
  // onSeatTap(e) {
  //   (async () => {
  //     // if (config.USE_REMOTE_API) {
  //     //   this.showInlineTip('当前为硬件同步模式，座位状态由雷达数据决定');
  //     //   return;
  //     // }
  //     const index = Number(e.currentTarget.dataset.index);
  //     const session = this._session;
  //     if (!session || Number.isNaN(index)) {
  //       return;
  //     }
  //     let slots;
  //     try {
  //       slots = await this.getSlotsForEdit();
  //     } catch (err) {
  //       api.errToast(err, '加载座位失败');
  //       return;
  //     }
  //     const slot = slots[index];
  //     const isAdmin = session.role === 'admin';

  //     if (isAdmin) {
  //       if (slot.reservedBy || slot.inUseBy) {
  //         slot.reservedBy = null;
  //         slot.inUseBy = null;
  //         slots[index] = slot;
  //         try {
  //           await this.commitSlots(slots);
  //           this.showInlineTip('已释放该座位', 'cross');
  //         } catch (err) {
  //           api.errToast(err, '同步失败');
  //         }
  //         return;
  //       }
  //       slot.reservedBy = session.username;
  //       slot.inUseBy = null;
  //       slots[index] = slot;
  //       try {
  //         await this.commitSlots(slots);
  //         wx.showToast({ title: '预约成功', icon: 'success' });
  //       } catch (err) {
  //         api.errToast(err, '同步失败');
  //       }
  //       return;
  //     }

  //     if (slot.inUseBy && slot.inUseBy !== session.username) {
  //       this.showInlineTip('该座位使用中');
  //       return;
  //     }

  //     if (slot.inUseBy === session.username) {
  //       this.showInlineTip('请先点击「结束使用」再释放座位');
  //       return;
  //     }

  //     if (slot.reservedBy === session.username) {
  //       slot.reservedBy = null;
  //       slot.inUseBy = null;
  //       slots[index] = slot;
  //       try {
  //         await this.commitSlots(slots);
  //         this.showInlineTip('已取消预约', 'cross');
  //       } catch (err) {
  //         api.errToast(err, '同步失败');
  //       }
  //       return;
  //     }

  //     if (slot.reservedBy) {
  //       wx.showToast({ title: '该座位已被预约', icon: 'none' });
  //       return;
  //     }

  //     if (getMyReservedIndex(slots, session.username) !== -1) {
  //       this.showInlineTip('每人仅限预约1个座位，请先取消当前预约');
  //       return;
  //     }

  //     slot.reservedBy = session.username;
  //     slots[index] = slot;
  //     try {
  //       await this.commitSlots(slots);
  //       wx.showToast({ title: '预约成功', icon: 'success' });
  //     } catch (err) {
  //       api.errToast(err, '同步失败');
  //     }
  //   })();
  // }, 

  onSeatTap(e) {
    (async () => {
      const index = Number(e.currentTarget.dataset.index);
      const session = this._session;
      if (!session || Number.isNaN(index)) return;

      const slots = this._slots || [];
      const slot = slots[index];
      const seatNumber = index + 1; // 数组下标0对应座位号1
      const isAdmin = session.role === 'admin';

      // 判定是否是我预约的座位
      const isMyReservation = slot.reservedBy === session.username || (isAdmin && slot.reservedBy === '系统管理员');

      // --- 逻辑 A：管理员特权操作 ---
      if (isAdmin) {
        if (slot.reservedBy || slot.inUseBy) {
          if (slot.reservationId) {
            wx.showLoading({ title: '释放中...', mask: true });
            try {
              await api.request({
                path: `${config.PATHS.cancelReservation}/${slot.reservationId}`,
                method: 'POST',
                data: { adminNote: "管理员强制释放" }
              });
              this.showInlineTip('已释放该座位', 'cross');
              await this.refreshSeats();
            } catch (err) {
              api.errToast(err, '同步失败');
            }
            wx.hideLoading();
          } else {
            this.showInlineTip('当前座位仅由硬件判定占用，无预约记录可释放');
          }
          return;
        }
        // 空闲座位，管理员直接预约
        this._doReserve(seatNumber);
        return;
      }

      // --- 逻辑 B：普通用户不可操作的拦截 ---
      if (slot.inUseBy && !isMyReservation) {
        this.showInlineTip('该座位使用中');
        return;
      }

      if (slot.reservedBy && !isMyReservation) {
        wx.showToast({ title: '该座位已被预约', icon: 'none' });
        return;
      }

      // --- 逻辑 C：取消我的预约 ---
      if (isMyReservation) {
        wx.showLoading({ title: '取消中...', mask: true });
        try {
          await api.request({
            path: `${config.PATHS.cancelReservation}/${slot.reservationId}`,
            method: 'POST'
          });
          this.showInlineTip('已取消预约', 'cross');
          await this.refreshSeats();
        } catch (err) {
          api.errToast(err, '取消失败');
        }
        wx.hideLoading();
        return;
      }

      // --- 逻辑 D：一人一座拦截 ---
      // 检查当前用户是否已经在列表中有名字
      const hasOtherReservation = slots.some(s => s.reservedBy === session.username);
      if (hasOtherReservation) {
        this.showInlineTip('每人仅限预约1个座位，请先取消当前预约');
        return;
      }

      // --- 逻辑 E：正式发起预约 ---
      this._doReserve(seatNumber);
    })();
  },
// ================= 3. 辅助预约方法 =================
async _doReserve(seatNumber) {
  wx.showLoading({ title: '预约中...', mask: true });
  try {
    const now = new Date();
    const startTime = new Date(now.getTime() + 60000); // 增加1分钟缓冲，防止C#后端报“过去时间”
    const tzOffset = now.getTimezoneOffset() * 60000; 
    
    // 生成本地时间的 ISO 格式（剥离时区字母 Z）
    const localStartTime = new Date(startTime.getTime() - tzOffset).toISOString().slice(0, 19);
    const localEndTime = new Date(startTime.getTime() - tzOffset + 2 * 60 * 60 * 1000).toISOString().slice(0, 19);

    await api.request({
      path: config.PATHS.createReservation,
      method: 'POST',
      data: {
        seatNumber: seatNumber,
        startTime: localStartTime,
        endTime: localEndTime
      }
    });

    wx.hideLoading();
    wx.showToast({ title: '预约成功', icon: 'success' });
    await this.refreshSeats();
  } catch (err) {
    wx.hideLoading();
    api.errToast(err, '同步失败');
  }
},

  onStartUse(e) {
    (async () => {
      // if (config.USE_REMOTE_API) {
      //   this.showInlineTip('当前为硬件同步模式，座位状态由雷达数据决定');
      //   return;
      // }
      const index = Number(e.currentTarget.dataset.index);
      const session = this._session;
      if (!session || Number.isNaN(index)) {
        return;
      }
      let slots;
      try {
        slots = await this.getSlotsForEdit();
      } catch (err) {
        api.errToast(err, '加载座位失败');
        return;
      }
      const slot = slots[index];
      if (slot.reservedBy !== session.username || slot.inUseBy) {
        return;
      }
      slot.inUseBy = session.username;
      slots[index] = slot;
      try {
        await this.commitSlots(slots);
        wx.showToast({ title: '已开始使用', icon: 'success' });
      } catch (err) {
        api.errToast(err, '同步失败');
      }
    })();
  },

  onEndUse(e) {
    (async () => {
      // if (config.USE_REMOTE_API) {
      //   this.showInlineTip('当前为硬件同步模式，座位状态由雷达数据决定');
      //   return;
      // }
      const index = Number(e.currentTarget.dataset.index);
      const session = this._session;
      if (!session || Number.isNaN(index)) {
        return;
      }
      let slots;
      try {
        slots = await this.getSlotsForEdit();
      } catch (err) {
        api.errToast(err, '加载座位失败');
        return;
      }
      const slot = slots[index];
      if (slot.inUseBy !== session.username) {
        return;
      }
      slot.inUseBy = null;
      slots[index] = slot;
      try {
        await this.commitSlots(slots);
        this.showInlineTip('已结束使用');
      } catch (err) {
        api.errToast(err, '同步失败');
      }
    })();
  },

  onUnload() {
    if (this._tipTimer) {
      clearTimeout(this._tipTimer);
    }
    if (this._pollTimer) {
      clearInterval(this._pollTimer);
      this._pollTimer = null;
    }
  },

  onLogout() {
    auth.clearSession();
    wx.reLaunch({ url: '/pages/login/login' });
  }
});
