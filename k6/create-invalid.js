import http from "k6/http";
import { check } from "k6";

const base = __ENV.BASE_URL || "http://api.localhost";

export const options = { vus: 10, duration: "30s" };

export default function() {
  const res = http.post(
    `${base}/api/links`,
    JSON.stringify({ url: "not-a-url" }),
    { headers: { "Content-Type": "application/json" } },
  );

  check(res, { "400": (r) => r.status === 400 });
}
