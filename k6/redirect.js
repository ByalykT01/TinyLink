import http from "k6/http";
import { check } from "k6";

const base = __ENV.BASE_URL || "http://api.localhost";
const code = __ENV.CODE;

if (!code) {
  throw new Error("CODE environment variable is required");
}

export const options = {
  vus: 10,
  duration: "30s",
};

export default function() {
  const res = http.get(`${base}/${code}`, {
    redirects: 0,
    tags: { name: "redirect" },
  });
  check(res, {
    "redirects": (r) => r.status === 302,
  });
}