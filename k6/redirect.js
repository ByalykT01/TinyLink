import http from "k6/http";
import { check } from "k6";
export const options = {
  vus: 10,
  duration: "30s",
};
export default function() {
  const res = http.get("http://localhost:5292/FGgAFjC", { redirects: 0 });
  check(res, {
    "redirects": (r) => r.status === 302,
  });
}
