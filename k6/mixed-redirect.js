import http from "k6/http";
import { check } from "k6";

const base = __ENV.BASE_URL || "http://api.localhost";

const headers = { headers: { "Content-Type": "application/json" } };

export const options = { vus: 10, duration: "30s" };

export function setup() {
  const codes = [];

  for (let i = 0; i < 5; i++) {
    const res = http.post(
      `${base}/api/links`,
      JSON.stringify({ url: `https://example.com/mix-${i}` }),
      headers,
    );

    if (res.status !== 201) throw new Error(`setup: ${res.status} ${res.body}`);
    codes.push(res.json("shortCode"));
  }
  return { codes };
}

export default function(data) {
  const code = data.codes[Math.floor(Math.random() * data.codes.length)];
  const res = http.get(`${base}/${code}`, {
    redirects: 0,
    tags: { name: "mixed-redirect" },
  });
  check(res, { "302": (r) => r.status === 302 });
}