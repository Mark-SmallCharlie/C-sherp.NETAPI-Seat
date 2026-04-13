const config = require('./config.js');
const api = require('./api.js');

const SEAT_COUNT = 4;

function normalizeSlot(item) {
  if (!item || typeof item !== 'object') {
    return { reservedBy: null, inUseBy: null };
  }
  const r =
    item.reservedBy !== undefined ? item.reservedBy : item.ReservedBy;
  const u = item.inUseBy !== undefined ? item.inUseBy : item.InUseBy;
  return {
    reservedBy: r == null || r === '' ? null : String(r),
    inUseBy: u == null || u === '' ? null : String(u)
  };
}

function parseSeatsResponse(data) {
  let list = null;
  if (Array.isArray(data)) {
    list = data;
  } else if (data && Array.isArray(data.items)) {
    list = data.items;
  } else if (data && Array.isArray(data.Items)) {
    list = data.Items;
  } else if (data && Array.isArray(data.seats)) {
    list = data.seats;
  } else if (data && Array.isArray(data.Seats)) {
    list = data.Seats;
  }
  if (!list) {
    throw new Error('座位数据格式不正确，期望数组或 { items: [] }');
  }
  const slots = list.map(normalizeSlot);
  while (slots.length < SEAT_COUNT) {
    slots.push({ reservedBy: null, inUseBy: null });
  }
  return slots.slice(0, SEAT_COUNT);
}

async function fetchSlots() {
  const data = await api.request({
    path: config.PATHS.seats,
    method: 'GET'
  });
  return parseSeatsResponse(data);
}

async function saveSlots(slots) {
  const items = slots.map((s) => ({
    reservedBy: s.reservedBy,
    inUseBy: s.inUseBy
  }));
  await api.request({
    path: config.PATHS.seats,
    method: 'PUT',
    data: { items }
  });
}

module.exports = {
  fetchSlots,
  saveSlots,
  normalizeSlot,
  SEAT_COUNT
};
