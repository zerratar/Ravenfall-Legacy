"""The flat character bake's server (see bake-flat.html): serves RavenNest's wwwroot, with this folder at /tools/, and
writes what the bake page posts (POST /save?path=characters/flat/...) under wwwroot/characters/flat.

    python tools/characters/bake-server.py [port]     then open http://127.0.0.1:8099/tools/bake-flat.html
"""
import http.server, os, sys, urllib.parse

HERE = os.path.dirname(os.path.abspath(__file__))
WWW = os.path.realpath(os.path.join(HERE, "../../../RavenNest/src/RavenNest.Blazor/wwwroot"))
SAVE_ROOT = os.path.realpath(os.path.join(WWW, "characters", "flat"))


class Handler(http.server.SimpleHTTPRequestHandler):
    def translate_path(self, path):
        p = urllib.parse.urlparse(path).path
        if p.startswith("/tools/"):
            return os.path.join(HERE, p[len("/tools/"):])
        return os.path.join(WWW, p.lstrip("/"))

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def do_POST(self):
        q = urllib.parse.parse_qs(urllib.parse.urlparse(self.path).query)
        target = os.path.realpath(os.path.join(WWW, q.get("path", [""])[0]))
        if not target.startswith(SAVE_ROOT + os.sep):
            self.send_error(403, "only under characters/flat/")
            return
        os.makedirs(os.path.dirname(target), exist_ok=True)
        with open(target, "wb") as f:
            f.write(self.rfile.read(int(self.headers.get("Content-Length", 0))))
        self.send_response(200)
        self.end_headers()
        self.wfile.write(b"ok")

    def log_message(self, *a):
        pass


port = int(sys.argv[1]) if len(sys.argv) > 1 else 8099
print("serving", WWW, "on", port, flush=True)
http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
