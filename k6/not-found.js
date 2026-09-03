import http from "k6/http";
import { check } from "k6";

const base = __ENV.BASE_URL || "http://api.localhost";

export const options = { vus: 10, duration: "30s" };

export default function() {
  const res = http.get(`${base}/_______`, {
    redirects: 0,
    tags: { name: "not-found" },
  });
  check(res, { "404": (r) => r.status === 404 });
}