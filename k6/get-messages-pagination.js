import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 25,
  duration: '45s',
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5189';
const ROOM_ID = __ENV.ROOM_ID;
const USER_ID = __ENV.USER_ID;

if (!ROOM_ID || !USER_ID) {
  throw new Error('Set ROOM_ID and USER_ID environment variables');
}

export default function () {
  let cursor = '';

  for (let i = 0; i < 5; i++) {
    const suffix = cursor ? `?cursor=${cursor}&limit=50` : '?limit=50';
    const response = http.get(`${BASE_URL}/api/rooms/${ROOM_ID}/messages${suffix}`, {
      headers: {
        'X-User-Id': USER_ID,
      },
    });

    check(response, {
      'status is 200': (r) => r.status === 200,
      'contains items': (r) => {
        const body = JSON.parse(r.body);
        return Array.isArray(body.items);
      },
    });

    const body = JSON.parse(response.body);
    cursor = body.nextCursor || '';
    if (!cursor) {
      break;
    }
  }

  sleep(1);
}
