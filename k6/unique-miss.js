import http from "k6/http";
import { check } from "k6";
const base = __ENV.BASE_URL || "http://localhost:5292";
const alphabet =
  "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
http.setResponseCallback(http.expectedStatuses(404));
export const options = {
  vus: 10,
  duration: "30s",
};
function base62(number) {
  let value = number;
  let result = "";
  do {
    result = alphabet[value % 62] + result;
    value = Math.floor(value / 62);
  } while (value > 0);
  return result.padStart(7, "0");
}
export default function() {
  const number = __VU * 10_000_000 + __ITER;
  const code = base62(number);
  const response = http.get(`${base}/${code}`, {
    redirects: 0,
    tags: { name: "unique-miss" },
  });
  check(response, {
    "unknown code returns 404": (result) => result.status === 404,
  });
}
