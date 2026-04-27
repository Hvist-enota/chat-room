import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 1,
  iterations: 3,
  thresholds: {
    http_req_duration: ['p(95)<600'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5203';
const ROOM_ID = __ENV.ROOM_ID;
const USER_ID = __ENV.USER_ID;

if (!ROOM_ID || !USER_ID) {
  throw new Error('Set ROOM_ID and USER_ID environment variables');
}

export default function () {
  const healthResponse = http.get(`${BASE_URL}/health`);
  check(healthResponse, {
    'health status is 200': (r) => r.status === 200,
  });

  const roomsResponse = http.get(`${BASE_URL}/api/rooms`);
  check(roomsResponse, {
    'rooms status is 200': (r) => r.status === 200,
  });

  const messagesResponse = http.get(`${BASE_URL}/api/rooms/${ROOM_ID}/messages?limit=10`, {
    headers: {
      'X-User-Id': USER_ID,
    },
  });

  check(messagesResponse, {
    'messages status is 200': (r) => r.status === 200,
  });
}
