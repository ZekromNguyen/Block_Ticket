import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    read_path: {
      executor: "ramping-vus",
      stages: [
        { duration: "2m", target: 20 },
        { duration: "5m", target: 20 },
        { duration: "2m", target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.02"],
    http_req_duration: ["p(95)<1000", "p(99)<2000"],
  },
};

const baseUrl = __ENV.BASE_URL || "https://staging.blockticket.example.com";

export default function () {
  const responses = http.batch([
    ["GET", `${baseUrl}/health`],
    ["GET", `${baseUrl}/api/events`],
    ["GET", `${baseUrl}/api/tickets/health`],
  ]);

  check(responses[0], { "health ok": (response) => response.status === 200 });
  check(responses[1], { "events below 500": (response) => response.status < 500 });
  check(responses[2], { "ticketing below 500": (response) => response.status < 500 });

  sleep(1);
}
