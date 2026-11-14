import { spawn } from 'node:child_process';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { projectRoot, readerId } from './server.mjs';

const url = 'http://127.0.0.1:4317';
async function health() {
  let response;
  try { response = await fetch(`${url}/api/health`, { signal: AbortSignal.timeout(1200) }); }
  catch (error) {
    if (error.cause?.code === 'ECONNREFUSED') return null;
    throw new Error('本機埠 4317 無法連線，請確認是否已被其他程式使用。');
  }
  let info;
  try { info = await response.json(); } catch { throw new Error('本機埠 4317 已由其他服務使用，未開啟網頁。'); }
  if (!response.ok || info.app !== readerId || resolve(info.projectRoot ?? '') !== projectRoot) {
    throw new Error('本機埠 4317 已由其他服務或另一份專案使用，未開啟網頁。');
  }
  return info;
}

try {
  if (!await health()) {
    const processHandle = spawn(process.execPath, [fileURLToPath(new URL('./server.mjs', import.meta.url))], {
      detached: true, windowsHide: true, stdio: 'ignore',
    });
    processHandle.unref();
    let ready = false;
    for (let attempt = 0; attempt < 30; attempt++) {
      await new Promise(resolve => setTimeout(resolve, 200));
      if (await health()) { ready = true; break; }
    }
    if (!ready) throw new Error('服務啟動逾時，請執行 node Tools/ProjectBoard/server.mjs 查看錯誤。');
  }
  if (!process.argv.includes('--no-open')) {
    const command = process.platform === 'win32' ? 'rundll32.exe' : process.platform === 'darwin' ? 'open' : 'xdg-open';
    const args = process.platform === 'win32' ? ['url.dll,FileProtocolHandler', url] : [url];
    const browserProcess = spawn(command, args, { detached: true, windowsHide: true, stdio: 'ignore' });
    browserProcess.on('error', () => console.error(`請手動開啟 ${url}`));
    browserProcess.unref();
  }
  console.log(`閱讀器已啟動：${url}`);
} catch (error) { console.error(error.message); process.exitCode = 1; }
