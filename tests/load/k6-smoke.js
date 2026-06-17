import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  vus: 5,
  duration: "1m",
  thresholds: {
    http_req_failed: ["rate<0.05"],
    http_req_duration: ["p(95)<750"],
  },
};

const baseUrl = __ENV.BASE_URL || "https://staging.blockticket.example.com";

export default function () {
  const health = http.get(`${baseUrl}/health`);
  check(health, {
    "health is 200": (response) => response.status === 200,
  });

  const events = http.get(`${baseUrl}/api/events`);
  check(events, {
    "events is not 5xx": (response) => response.status < 500,
  });

  sleep(1);
}
