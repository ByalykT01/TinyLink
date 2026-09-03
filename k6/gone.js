import http from "k6/http";
import { check, sleep } from "k6";

const base = __ENV.BASE_URL || "http://api.localhost";

const headers = { headers: { "Content-Type": "application/json" } };

export const options = { vus: 10, duration: "15s" };

export function setup() {
  const expiresAt = new Date(Date.now() + 1500).toISOString();

  const res = http.post(
    `${base}/api/links`,
    JSON.stringify({ url: "https://example.com/expired", expiresAt }),
    headers,
  );

  if (res.status !== 201) throw new Error(`setup: ${res.status} ${res.body}`);

  sleep(2);

  return { code: res.json("shortCode") };
}

export default function(data) {
  const res = http.get(`${base}/${data.code}`, {
    redirects: 0,
    tags: { name: "gone" },
  });
  check(res, { "410": (r) => r.status === 410 });
}