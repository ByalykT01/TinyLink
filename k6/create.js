import http from "k6/http";
import { check } from "k6";
import { Counter } from "k6/metrics";

const base = __ENV.BASE_URL || "http://api.localhost";

const created = new Counter("links_created");

const limited = new Counter("links_rate_limited");

export const options = { vus: 5, duration: "30s" };

export default function() {
  const res = http.post(
    `${base}/api/links`,
    JSON.stringify({ url: `https://example.com/k6-${__VU}-${__ITER}` }),
    {
      headers: { "Content-Type": "application/json" },
      tags: { name: "create" },
    },
  );

  check(res, { "201 or 429": (r) => r.status === 201 || r.status === 429 });

  if (res.status === 201) created.add(1);
  if (res.status === 429) limited.add(1);
}