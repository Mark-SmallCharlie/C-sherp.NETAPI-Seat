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
    isAdmin: false,
    seats: [],
    toastVisible: false,
    toastMsg: '',
    toastIcon: '',
    utilizationPercent: 0,
    usedSeats: 0,
    totalSeats: 4
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
    if (!session) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    this._session = session;
    this.setData({
      displayName: session.username,
      isAdmin: session.role === 'admin'
    });
    this.refreshSeats();
  },

  async refreshSeats(slots) {
    const session = this._session;
    if (!session) {
      return;
    }
    let list = slots;
    if (list === undefined || list === null) {
      try {
        list = config.USE_REMOTE_API
          ? await seatApi.fetchSlots()
          : loadSlotsLocal();
      } catch (e) {
        api.errToast(e, '座位数据加载失败');
        return;
      }
    }
    const isAdmin = session.role === 'admin';
    const totalSeats = SEAT_DEFS.length;
    const usedSeats = list.filter((s) => s.reservedBy || s.inUseBy).length;
    const utilizationPercent = totalSeats
      ? Math.round((usedSeats / totalSeats) * 100)
      : 0;

    const seats = SEAT_DEFS.map((def, index) => {
      const slot = list[index];
      const { reservedBy, inUseBy } = slot;
      let statusClass = 'seat-free';
      let stateText = '空闲';
      let showStartUse = false;
      let showEndUse = false;

      if (inUseBy) {
        statusClass = 'seat-in-use';
        if (inUseBy === session.username) {
          stateText = '我使用中';
        } else if (isAdmin) {
          stateText = `使用中（${inUseBy}）`;
        } else {
          stateText = '使用中';
        }
      } else if (reservedBy) {
        if (reservedBy === session.username) {
          statusClass = 'seat-mine';
          stateText = '我的预约';
          showStartUse = true;
        } else if (isAdmin) {
          statusClass = 'seat-other';
          stateText = `已预约（${reservedBy}）`;
        } else {
          statusClass = 'seat-other';
          stateText = '已预约';
        }
      }

      if (inUseBy === session.username) {
        showStartUse = false;
        showEndUse = true;
      }

      return {
        id: def.id,
        statusClass,
        stateText,
        showStartUse,
        showEndUse
      };
    });
    this.setData({
      seats,
      utilizationPercent,
      usedSeats,
      totalSeats
    });
    this._slots = list;
  },

  onSeatTap(e) {
    (async () => {
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
      const isAdmin = session.role === 'admin';

      if (isAdmin) {
        if (slot.reservedBy || slot.inUseBy) {
          slot.reservedBy = null;
          slot.inUseBy = null;
          slots[index] = slot;
          try {
            await this.commitSlots(slots);
            this.showInlineTip('已释放该座位', 'cross');
          } catch (err) {
            api.errToast(err, '同步失败');
          }
          return;
        }
        slot.reservedBy = session.username;
        slot.inUseBy = null;
        slots[index] = slot;
        try {
          await this.commitSlots(slots);
          wx.showToast({ title: '预约成功', icon: 'success' });
        } catch (err) {
          api.errToast(err, '同步失败');
        }
        return;
      }

      if (slot.inUseBy && slot.inUseBy !== session.username) {
        this.showInlineTip('该座位使用中');
        return;
      }

      if (slot.inUseBy === session.username) {
        this.showInlineTip('请先点击「结束使用」再释放座位');
        return;
      }

      if (slot.reservedBy === session.username) {
        slot.reservedBy = null;
        slot.inUseBy = null;
        slots[index] = slot;
        try {
          await this.commitSlots(slots);
          this.showInlineTip('已取消预约', 'cross');
        } catch (err) {
          api.errToast(err, '同步失败');
        }
        return;
      }

      if (slot.reservedBy) {
        wx.showToast({ title: '该座位已被预约', icon: 'none' });
        return;
      }

      if (getMyReservedIndex(slots, session.username) !== -1) {
        this.showInlineTip('每人仅限预约1个座位，请先取消当前预约');
        return;
      }

      slot.reservedBy = session.username;
      slots[index] = slot;
      try {
        await this.commitSlots(slots);
        wx.showToast({ title: '预约成功', icon: 'success' });
      } catch (err) {
        api.errToast(err, '同步失败');
      }
    })();
  },

  onStartUse(e) {
    (async () => {
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
  },

  onLogout() {
    auth.clearSession();
    wx.reLaunch({ url: '/pages/login/login' });
  }
});
