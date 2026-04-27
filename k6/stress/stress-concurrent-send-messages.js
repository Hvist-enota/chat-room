import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '10s', target: 20 },
    { duration: '30s', target: 120 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<1200'],
    http_req_failed: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5203';
const ROOM_ID = __ENV.ROOM_ID;
const USER_ID = __ENV.USER_ID;

if (!ROOM_ID || !USER_ID) {
  throw new Error('Set ROOM_ID and USER_ID environment variables');
}

export default function () {
  const payload = JSON.stringify({
    content: `k6 message ${__VU}-${__ITER}-${Date.now()}`,
  });

  const response = http.post(`${BASE_URL}/api/rooms/${ROOM_ID}/messages`, payload, {
    headers: {
      'Content-Type': 'application/json',
      'X-User-Id': USER_ID,
    },
  });

  check(response, {
    'status is 200': (r) => r.status === 200,
  });

  sleep(0.2);
}
