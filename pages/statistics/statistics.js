const api = require('../../utils/api.js');
const config = require('../../utils/config.js');
const auth = require('../../utils/auth.js');

Page({
  data: {
    loading: true,
    dashboard: null,
    utilization: null,
    popularSeats: []
  },

  onLoad() {
    this.loadAllData();
  },

  onShow() {
    const session = auth.getSession();
    if (!session) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    if (session.role !== 'admin' && session.Role !== 'admin') {
      wx.showToast({ title: '仅限管理员访问', icon: 'none' });
      wx.navigateBack({ delta: 1 });
    }
  },

  async loadAllData() {
    this.setData({ loading: true });

    // 独立请求三个接口，互不阻塞
    const tasks = [
      this.fetchDashboard(),
      this.fetchSeatUtilization(),
      this.fetchPopularSeats()
    ];

    const settle = typeof Promise.allSettled === 'function'
      ? Promise.allSettled.bind(Promise)
      : this._fallbackAllSettled.bind(this);
    const [dashboardR, utilR, popularR] = await settle(tasks);

    const dashboard = this._unwrapSettled(dashboardR);
    const utilization = this._unwrapSettled(utilR);
    const popularSeats = this._unwrapSettled(popularR) || [];

    // 计算热门座位横向柱的百分比
    if (popularSeats.length > 0) {
      const maxCount = Math.max(...popularSeats.map(s => s.reservationCount || 0));
      popularSeats.forEach(s => {
        const count = s.reservationCount || 0;
        s._percent = maxCount > 0 ? Math.round((count / maxCount) * 100) : 0;
      });
    }

    this.setData({ dashboard, utilization, popularSeats, loading: false });

    if (utilization) {
      // 等 DOM 渲染完成后绘制 canvas 图表
      setTimeout(() => this.drawUtilizationChart(), 200);
    }
  },

  _unwrapSettled(result) {
    if (!result) return null;
    if (result.status === 'fulfilled') return result.value;
    console.warn('API 请求失败:', result.reason);
    return null;
  },

  _fallbackAllSettled(tasks) {
    return Promise.all(
      tasks.map(p =>
        p.then(
          value => ({ status: 'fulfilled', value }),
          reason => ({ status: 'rejected', reason })
        )
      )
    );
  },

  // ===================== API 请求 =====================
  // 后端 Response 统一信封: { success: bool, message: string, data: T }
  // api.request() 返回的是整个信封 JSON，实际数据在 .data 字段中

  async fetchDashboard() {
    const res = await api.request({
      path: config.PATHS.dashboard,
      method: 'GET'
    });
    const data = res.data || res;
    // Dashboard 聚合对象: { monthlyStatistics, seatUtilization, topSeats, weeklyUserActivity }
    const monthly = data.monthlyStatistics || {};
    const weekly = data.weeklyUserActivity || {};
    return {
      monthlyReservations: monthly.totalReservations || 0,
      weeklyActiveUsers: weekly.totalActiveUsers || 0
    };
  },

  async fetchSeatUtilization() {
    const res = await api.request({
      path: config.PATHS.seatUtilization,
      method: 'GET'
    });
    const data = res.data || res;
    // utilizationRates: 预约利用率, actualUtilizationRates: 硬件实际使用率
    const rates = data.utilizationRates || {};
    const actualRates = data.actualUtilizationRates || {};

    // 合并两个字典的座位号
    const allSeatNums = new Set([
      ...Object.keys(rates),
      ...Object.keys(actualRates)
    ]);

    const seats = Array.from(allSeatNums).map(key => ({
      seatNumber: parseInt(key) || key,
      utilization: rates[key] || 0,       // 预约利用率
      actualUtilization: actualRates[key] || 0  // 实际使用率
    }));

    seats.sort((a, b) => {
      const na = typeof a.seatNumber === 'number' ? a.seatNumber : parseInt(a.seatNumber) || 0;
      const nb = typeof b.seatNumber === 'number' ? b.seatNumber : parseInt(b.seatNumber) || 0;
      return na - nb;
    });

    return {
      overallUtilization: data.overallUtilization || 0,
      overallActualUtilization: data.overallActualUtilization || 0,
      totalSeats: data.totalSeats || 0,
      analysisDays: data.analyzedDays || 30,
      totalReservations: data.totalReservations || 0,
      seats
    };
  },

  async fetchPopularSeats() {
    const res = await api.request({
      path: config.PATHS.popularSeats(10),
      method: 'GET'
    });
    const data = res.data || res;
    // popularSeats 是数组，每项: { seatNumber, reservationCount, totalHours }
    const list = data.popularSeats || [];
    if (Array.isArray(list)) {
      return list.map(s => ({
        seatNumber: s.seatNumber,
        reservationCount: s.reservationCount,
        totalHours: s.totalHours
      }));
    }
    return [];
  },

  // ===================== Canvas 图表绘制 =====================

  drawUtilizationChart() {
    const query = wx.createSelectorQuery();
    query.select('#utilChart')
      .fields({ node: true, size: true })
      .exec((res) => {
        if (!res || !res[0]) {
          console.warn('Canvas 节点未找到，延迟重试');
          setTimeout(() => this.drawUtilizationChart(), 300);
          return;
        }
        this._renderChart(res[0]);
      });
  },

  _renderChart(nodeInfo) {
    const canvas = nodeInfo.node;
    const ctx = canvas.getContext('2d');
    const dpr = wx.getSystemInfoSync().pixelRatio;

    const width = nodeInfo.width;
    const height = nodeInfo.height;

    canvas.width = width * dpr;
    canvas.height = height * dpr;
    ctx.scale(dpr, dpr);

    ctx.clearRect(0, 0, width, height);

    const utilization = this.data.utilization;
    if (!utilization || !utilization.seats || utilization.seats.length === 0) return;

    const seats = utilization.seats;
    const overallReserve = utilization.overallUtilization || 0;
    const overallActual = utilization.overallActualUtilization || 0;

    // 绘图区域边距
    const pad = { top: 24, right: 16, bottom: 40, left: 52 };
    const chartW = width - pad.left - pad.right;
    const chartH = height - pad.top - pad.bottom;

    // Y轴刻度与网格线
    const ySteps = [0, 25, 50, 75, 100];
    ctx.font = `${Math.round(11 * dpr) / dpr}px sans-serif`;
    ctx.fillStyle = '#8e8e93';
    ctx.textAlign = 'right';
    ctx.textBaseline = 'middle';

    ySteps.forEach(step => {
      const y = pad.top + chartH - (step / 100) * chartH;
      ctx.beginPath();
      ctx.setLineDash([4, 4]);
      ctx.strokeStyle = '#e5e5ea';
      ctx.lineWidth = 0.5;
      ctx.moveTo(pad.left, y);
      ctx.lineTo(pad.left + chartW, y);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillText(step + '%', pad.left - 8, y);
    });

    // 绘制两条整体参考线
    this._drawRefLine(ctx, pad, chartW, chartH, overallReserve, '#e65100', '预约 ' + Math.round(overallReserve) + '%');
    this._drawRefLine(ctx, pad, chartW, chartH, overallActual, '#2979ff', '实际 ' + Math.round(overallActual) + '%');

    // 分组柱状图参数
    const groupCount = seats.length;
    const groupGap = Math.max(12, Math.min(28, chartW / groupCount * 0.3));
    const groupW = (chartW - groupGap * (groupCount + 1)) / groupCount;
    const barGapInner = Math.max(2, groupW * 0.12);
    const barW = Math.max(6, (groupW - barGapInner) / 2);

    ctx.textAlign = 'center';

    seats.forEach((seat, i) => {
      const groupX = pad.left + groupGap + i * (groupW + groupGap);
      const baseY = pad.top + chartH;

      // --- 左侧柱：预约利用率（绿色） ---
      const x1 = groupX;
      const h1 = Math.max(2, (seat.utilization / 100) * chartH);
      const y1 = baseY - h1;
      const grad1 = ctx.createLinearGradient(x1, y1, x1, baseY);
      grad1.addColorStop(0, '#1a6b5c');
      grad1.addColorStop(1, '#3dbaa0');
      ctx.fillStyle = grad1;
      this._drawRoundRect(ctx, x1, y1, barW, h1, Math.min(3, barW / 2));
      ctx.fill();

      // 预约率数值标签
      if (seat.utilization > 0) {
        ctx.font = `${Math.round(9 * dpr) / dpr}px sans-serif`;
        ctx.fillStyle = '#1a6b5c';
        ctx.fillText(Math.round(seat.utilization) + '%', x1 + barW / 2, y1 - 4);
      }

      // --- 右侧柱：实际使用率（蓝色） ---
      const x2 = groupX + barW + barGapInner;
      const actualVal = seat.actualUtilization || 0;
      const h2 = Math.max(2, (actualVal / 100) * chartH);
      const y2 = baseY - h2;
      const grad2 = ctx.createLinearGradient(x2, y2, x2, baseY);
      grad2.addColorStop(0, '#2979ff');
      grad2.addColorStop(1, '#82b1ff');
      ctx.fillStyle = grad2;
      this._drawRoundRect(ctx, x2, y2, barW, h2, Math.min(3, barW / 2));
      ctx.fill();

      // 实际率数值标签
      if (actualVal > 0) {
        ctx.font = `${Math.round(9 * dpr) / dpr}px sans-serif`;
        ctx.fillStyle = '#2979ff';
        ctx.fillText(Math.round(actualVal) + '%', x2 + barW / 2, y2 - 4);
      }

      // X轴座位标签（居中于整组）
      ctx.font = `${Math.round(11 * dpr) / dpr}px sans-serif`;
      ctx.fillStyle = '#6b6b70';
      const label = String(seat.seatNumber || '?').replace(/座位/i, '');
      ctx.fillText(label, groupX + groupW / 2, baseY + 16);
    });

    // X轴底线
    ctx.beginPath();
    ctx.strokeStyle = '#c7c7cc';
    ctx.lineWidth = 0.5;
    ctx.moveTo(pad.left, pad.top + chartH);
    ctx.lineTo(pad.left + chartW, pad.top + chartH);
    ctx.stroke();
  },

  _drawRefLine(ctx, pad, chartW, chartH, value, color, label) {
    if (!value) return;
    const refY = pad.top + chartH - (value / 100) * chartH;
    ctx.beginPath();
    ctx.setLineDash([6, 3]);
    ctx.strokeStyle = color;
    ctx.lineWidth = 1;
    ctx.moveTo(pad.left, refY);
    ctx.lineTo(pad.left + chartW, refY);
    ctx.stroke();
    ctx.setLineDash([]);

    ctx.font = '10px sans-serif';
    ctx.fillStyle = color;
    ctx.textAlign = 'left';
    ctx.fillText(label, pad.left + 4, refY - 6);
  },

  _drawRoundRect(ctx, x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.arcTo(x + w, y, x + w, y + r, r);
    ctx.lineTo(x + w, y + h);
    ctx.lineTo(x, y + h);
    ctx.lineTo(x, y + r);
    ctx.arcTo(x, y, x + r, y, r);
    ctx.closePath();
  }
});
