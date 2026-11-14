import http from 'node:http';
import { readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { readProject, readWorking } from './reader.mjs';

const directory = dirname(fileURLToPath(import.meta.url));
export const projectRoot = resolve(directory, '../..');
export const readerId = 'mortalgame-document-reader';

// 只提供閱讀器自身資產及 Document 的讀取結果，不開放任意路徑或寫入。
const assets = new Map([
  ['/', ['index.html', 'text/html; charset=utf-8']],
  ['/index.html', ['index.html', 'text/html; charset=utf-8']],
  ['/style.css', ['style.css', 'text/css; charset=utf-8']],
  ['/app.js', ['app.js', 'text/javascript; charset=utf-8']],
]);

export function createReaderServer(root = projectRoot) {
  let lastActivity = Date.now();
  const server = http.createServer(async (request, response) => {
    const port = server.address()?.port;
    const allowedHosts = [`127.0.0.1:${port}`, `localhost:${port}`];
    const origin = request.headers.origin;
    if (!allowedHosts.includes(request.headers.host) || (origin && !allowedHosts.some(host => origin === `http://${host}`))) {
      response.writeHead(403); response.end(); return;
    }
    if (!['GET', 'HEAD'].includes(request.method)) {
      response.writeHead(405, { Allow: 'GET, HEAD' }); response.end(); return;
    }
    const pathname = new URL(request.url, 'http://localhost').pathname;
    const send = (status, type, content) => {
      response.writeHead(status, { 'Content-Type': type, 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff' });
      response.end(request.method === 'HEAD' ? undefined : content);
    };
    try {
      if (pathname === '/api/health') {
        lastActivity = Date.now();
        send(200, 'application/json; charset=utf-8', JSON.stringify({ app: readerId, projectRoot: root }));
      } else if (pathname === '/api/project') {
        lastActivity = Date.now();
        send(200, 'application/json; charset=utf-8', JSON.stringify(await readProject(root)));
      } else if (pathname === '/api/working') {
        lastActivity = Date.now();
        send(200, 'application/json; charset=utf-8', JSON.stringify(await readWorking(root)));
      } else if (assets.has(pathname)) {
        lastActivity = Date.now();
        const [name, type] = assets.get(pathname);
        send(200, type, await readFile(resolve(directory, 'dist', name)));
      } else send(404, 'text/plain; charset=utf-8', '找不到指定頁面。');
    } catch (error) {
      console.error(error.message);
      send(500, 'application/json; charset=utf-8', JSON.stringify({ error: '無法讀取 Document，請確認資料夾與 Markdown 檔案仍可存取。' }));
    }
  });
  // 分頁存續時定期確認連線；全部分頁關閉五分鐘後結束服務。
  const idleCheck = setInterval(() => {
    if (Date.now() - lastActivity > 5 * 60_000) server.close();
  }, 30_000);
  idleCheck.unref();
  server.on('close', () => clearInterval(idleCheck));
  return server;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const server = createReaderServer();
  server.listen(4317, '127.0.0.1', () => console.log('Local: http://127.0.0.1:4317'));
  server.on('error', error => { console.error(`閱讀器無法啟動：${error.message}`); process.exitCode = 1; });
}
