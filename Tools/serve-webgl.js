/*
 * Serves a WebGL build from Builds/ over localhost so it can be played before upload.
 *
 *     node Tools/serve-webgl.js [port] [folder]      (defaults: 8099, WebGL-YT)
 *
 * The YouTube build is Brotli with the decompressor built into the loader
 * (decompressionFallback), exactly like on McPlay: the files are sent as they are, with no
 * Content-Encoding header, and the loader unpacks them itself. So this tests the same path
 * the portal will take. Cache is off: a stale .data from the previous build is a confusing bug.
 * Based on Car Simulator's Tools/serve-webgl.js.
 */

const fs = require('fs');
const http = require('http');
const path = require('path');

const ROOT = path.join(__dirname, '..', 'Builds', process.argv[3] || 'WebGL-YT');
const PORT = Number(process.argv[2] || 8099);

const TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.wasm': 'application/wasm',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.css': 'text/css; charset=utf-8',
};

const server = http.createServer((req, res) => {
  let rel = decodeURIComponent(req.url.split('?')[0]);
  if (rel === '/') rel = '/index.html';

  const file = path.join(ROOT, rel);
  if (!file.startsWith(ROOT)) {
    res.writeHead(403).end('no');
    return;
  }

  fs.stat(file, (err, stat) => {
    if (err || !stat.isFile()) {
      console.log('404 ' + rel);
      res.writeHead(404).end('not found');
      return;
    }
    res.setHeader('Content-Type', TYPES[path.extname(file).toLowerCase()] || 'application/octet-stream');
    res.setHeader('Content-Length', stat.size);
    res.setHeader('Cache-Control', 'no-store');
    res.writeHead(200);
    fs.createReadStream(file).pipe(res);
  });
});

if (!fs.existsSync(path.join(ROOT, 'index.html'))) {
  console.error('no build at ' + ROOT);
  process.exit(1);
}

server.listen(PORT, '127.0.0.1', () => {
  console.log('serving ' + ROOT + ' at http://127.0.0.1:' + PORT + '/');
});
