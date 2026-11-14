import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve, dirname, basename } from 'node:path';
import { once } from 'node:events';
import http from 'node:http';
import { createReaderServer, readerId } from './server.mjs';

test('文件與工作計畫直接讀取、更新及唯讀服務邊界', async t => {
  const tempBase = resolve(tmpdir());
  const root = await mkdtemp(join(tempBase, 'mortalgame-reader-test-'));
  const documents = join(root, 'Document');
  const working = join(root, '.agents/working');
  await mkdir(documents);
  await mkdir(working, { recursive: true });
  const todo = '# 待辦\n\n> 最後更新：2026-11-13\n\n### T-011：測試任務\n\n- **狀態**：🔄 進行中\n\n| 工作 | 狀態 |\n|---|---|\n| 1. 規則 | ✅ 已完成 |\n| 2. 驗收 | 🔄 文件已完成，實際畫面待驗收 |\n\n1. 檢查取消。\n';
  await writeFile(join(documents, 'TODO.md'), todo);
  await writeFile(join(documents, 'TODO_Archive.md'), '# 歷史\n\n### T-001：完成項目\n\n- **狀態**：✅ 已完成\n\n### T-011：舊階段\n\n- **狀態**：🔄 進行中\n');
  await writeFile(join(working, '目前工作.md'), '# 目前工作\n\n- [計畫](測試計畫.md)\n- [非法路徑](../memory.md)\n- [重複](測試計畫.md)\n');
  const plan = '# 測試計畫\n\n> 目前步驟：第 2 個工作\n> 最近完成：規則\n> 下一步：驗證\n\n| 項次 | 範圍 | 狀態 |\n|---|---|---|\n| 1 | 規則 | 待使用者確認 |\n| 2 | 讀取 | 進行中 |\n';
  await writeFile(join(working, '測試計畫.md'), plan);
  await writeFile(join(working, '舊草稿.md'), '# 不應自動列為目前工作\n');
  const server = createReaderServer(root);
  try {
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    const url = `http://127.0.0.1:${server.address().port}`;
    const read = async path => (await fetch(url + path)).json();

    await t.test('保留進行中狀態，不把局部完成誤當整步完成', async () => {
      const data = await read('/api/project');
      assert.equal(data.tasks.length, 2);
      assert.equal(data.tasks[0].status, 'active');
      assert.deepEqual(data.tasks[0].steps.map(step => step.status), ['done', 'active']);
      assert.equal(data.tasks[0].checks.length, 1);
      assert.equal(data.tasks.filter(task => task.id === 'T-011').length, 1);
      assert.equal(data.tasks[1].status, 'done');
    });
    await t.test('封存任務支援二級標題，不沿用後方階段歷史的狀態', async () => {
      const archivePath = join(documents, 'TODO_Archive.md');
      const archive = '# 歷史\n\n### T-001：完成項目\n\n- **狀態**：✅ 已完成\n\n### T-011：舊階段\n\n- **狀態**：🔄 進行中\n';
      try {
        await writeFile(join(documents, 'TODO.md'), '# 待辦\n\n### T-012：下一項\n\n- **狀態**：⬜ 未開始\n');
        await writeFile(archivePath, '# 歷史\n\n## T-011：已封存任務\n\n- **狀態**：✅ 已完成\n\n### 先前階段紀錄\n\n- **狀態**：🔄 尚未結案\n');
        const data = await read('/api/project');
        assert.equal(data.tasks.find(task => task.id === 'T-011')?.status, 'done');
        assert.equal(data.tasks.find(task => task.id === 'T-012')?.status, 'planned');
        assert.doesNotMatch(data.tasks.find(task => task.id === 'T-011').body, /尚未結案/);
      } finally {
        await writeFile(join(documents, 'TODO.md'), todo);
        await writeFile(archivePath, archive);
      }
    });
    await t.test('每次讀取目前磁碟內容，包含新增及刪除文件', async () => {
      await writeFile(join(documents, '新增文件.md'), '# 新增文件\n\n更新後的內容。\n');
      let data = await read('/api/project');
      assert.equal(data.documents['新增文件.md'].body, '# 新增文件\n\n更新後的內容。\n');
      await writeFile(join(documents, '新增文件.md'), '# 新增文件\n\n第二版。\n');
      data = await read('/api/project');
      assert.match(data.documents['新增文件.md'].body, /第二版/);
      await rm(join(documents, '新增文件.md'));
      assert.equal((await read('/api/project')).documents['新增文件.md'], undefined);
    });
    await t.test('只讀目前工作索引，保留待確認與進行中的差異', async () => {
      const data = await read('/api/working');
      assert.equal(data.items.length, 1);
      assert.equal(data.items[0].fields['目前步驟'], '第 2 個工作');
      assert.deepEqual(data.items[0].steps.map(step => step.status), ['review', 'active']);
      await writeFile(join(working, '測試計畫.md'), plan.replace('第 2 個工作', '第 3 個工作'));
      assert.equal((await read('/api/working')).items[0].fields['目前步驟'], '第 3 個工作');
      await writeFile(join(working, '目前工作.md'), '# 目前沒有工作\n');
      assert.equal((await read('/api/working')).items.length, 0);
    });
    await t.test('不要求文件一定包含可解析任務', async () => {
      await writeFile(join(documents, 'TODO.md'), '# 自由格式的待辦\n\n先保留原文。\n');
      const data = await read('/api/project');
      assert.match(data.documents['TODO.md'].body, /保留原文/);
      assert.ok(data.warnings.length);
    });
    await t.test('服務標識可辨認，資產有正確型別且不使用快取', async () => {
      assert.deepEqual(await read('/api/health'), { app: readerId, projectRoot: root });
      const response = await fetch(url + '/');
      assert.equal(response.status, 200);
      assert.match(response.headers.get('content-type'), /text\/html/);
      assert.equal(response.headers.get('cache-control'), 'no-store');
      const head = await fetch(url + '/api/health', { method: 'HEAD' });
      assert.equal(await head.text(), '');
    });
    await t.test('不提供寫入、任意檔案或其他網站讀取', async () => {
      assert.equal((await fetch(url + '/api/project', { method: 'POST' })).status, 405);
      assert.equal((await fetch(url + '/AGENTS.md')).status, 404);
      assert.equal((await fetch(url + '/.agents/memory.md')).status, 404);
      assert.equal((await fetch(url + '/api/project', { headers: { Origin: 'https://example.com' } })).status, 403);
      const foreignHostStatus = await new Promise((resolve, reject) => {
        http.get(url + '/api/project', { headers: { Host: 'example.com' } }, response => {
          response.resume(); resolve(response.statusCode);
        }).on('error', reject);
      });
      assert.equal(foreignHostStatus, 403);
    });
  } finally {
    server.closeAllConnections();
    await new Promise(resolve => server.close(resolve));
    if (dirname(root) !== tempBase || !basename(root).startsWith('mortalgame-reader-test-')) throw new Error('測試暫存路徑不符合清理範圍。');
    await rm(root, { recursive: true, force: true });
  }
});
