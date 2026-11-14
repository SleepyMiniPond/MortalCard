import { readFile, readdir } from 'node:fs/promises';
import { resolve } from 'node:path';

const plain = text => text.replace(/\[([^\]]+)\]\([^)]+\)/g, '$1').replace(/[*`]/g, '').trim();

// 依既有文件辨識任務；不要求文件配合閱讀器新增欄位。
function tasks(markdown, source, archived = false) {
  const matches = [...markdown.matchAll(/^#{2,3} (T-\d+)：[ \t]*(.+)$/gm)];
  return matches.map((match, index) => {
    const body = markdown.slice(match.index + match[0].length, matches[index + 1]?.index ?? markdown.length).split(/^#{1,3} /m)[0].replace(/\n---\s*$/, '').trim();
    const fields = {};
    for (const field of body.matchAll(/^- \*\*([^*]+)\*\*[：:]?\s*(.+)$/gm)) fields[field[1]] = field[2].trim();
    const statusText = fields['狀態'] ?? '';
    if (archived && !/^✅/.test(statusText)) return null;
    const status = archived ? 'done' : /🔄|進行中/.test(statusText) ? 'active' : /🟡|部分|另行評估/.test(statusText) ? 'partial' : 'planned';
    const rows = [...body.matchAll(/^\| ([1-9]\d*\. [^|]+) \| ([^|]+) \|$/gm)];
    const steps = rows.map(row => ({ title: plain(row[1]), detail: plain(row[2]), status: /🔄|進行中/.test(row[2]) ? 'active' : /✅|已完成/.test(row[2]) ? 'done' : 'planned' }));
    const checks = [...body.matchAll(/^\d+\. (.+)$/gm)].map(row => plain(row[1]));
    const paragraph = body.split('\n\n').find(line => !/^[#>|-]/.test(line)) ?? '';
    return {
      id: match[1], title: plain(match[2]), body, fields, status, source,
      summary: plain(fields['目標與範圍'] ?? fields['目標'] ?? fields['現況'] ?? paragraph),
      steps, checks, dependencies: [...(fields['前置'] ?? '').matchAll(/T-\d+/g)].map(item => item[0]),
    };
  }).filter(Boolean);
}


export async function readProject(root) {
  const documents = {};
  for (const entry of (await readdir(resolve(root, 'Document'), { withFileTypes: true })).sort((a, b) => a.name.localeCompare(b.name))) {
    if (!entry.isFile() || !entry.name.endsWith('.md')) continue;
    const body = (await readFile(resolve(root, 'Document', entry.name), 'utf8')).replace(/^\uFEFF/, '').replace(/\r\n/g, '\n');
    documents[entry.name] = { title: plain(body.match(/^# (.+)$/m)?.[1] ?? entry.name), body };
  }
  const todo = documents['TODO.md']?.body ?? '';
  const pending = tasks(todo, 'TODO.md');
  const completed = tasks(documents['TODO_Archive.md']?.body ?? '', 'TODO_Archive.md', true).filter(task => !pending.some(item => item.id === task.id));
  return {
    sourceDate: todo.match(/最後更新：(\d{4}-\d{2}-\d{2})/)?.[1] ?? '未標示',
    readAt: new Date().toISOString(),
    tasks: [...pending, ...completed], documents, working: await readWorking(root),
    warnings: todo && pending.length ? [] : ['尚未辨識到待辦任務，仍可在專案筆記閱讀原始文件。'],
  };
}

function workingStatus(text) {
  if (/待使用者確認|待確認|待驗收/.test(text)) return 'review';
  if (/進行中/.test(text)) return 'active';
  if (/已完成/.test(text)) return 'done';
  return 'planned';
}

export async function readWorking(root) {
  const folder = resolve(root, '.agents/working');
  let entries;
  try { entries = await readdir(folder, { withFileTypes: true }); }
  catch (error) { if (error.code === 'ENOENT') return { items: [], warnings: [] }; throw error; }
  const names = new Set(entries.filter(entry => entry.isFile() && entry.name.endsWith('.md')).map(entry => entry.name));
  if (!names.has('目前工作.md')) return { items: [], warnings: [] };
  const index = await readFile(resolve(folder, '目前工作.md'), 'utf8');
  const warnings = [];
  const items = [];
  const seen = new Set();
  for (const match of index.matchAll(/\[[^\]]+\]\(([^)]+)\)/g)) {
    let filename;
    try { filename = decodeURIComponent(match[1]); } catch { continue; }
    if (!/^[^/\\]+\.md$/.test(filename) || filename === '目前工作.md' || seen.has(filename)) continue;
    seen.add(filename);
    if (!names.has(filename)) { warnings.push(`目前工作索引中的檔案不存在或不是一般文件：${filename}`); continue; }
    const body = (await readFile(resolve(folder, filename), 'utf8')).replace(/^\uFEFF/, '').replace(/\r\n/g, '\n');
    const fields = {};
    for (const field of body.matchAll(/^>\s*([^：:\n]+)[：:]\s*(.+)$/gm)) fields[field[1].trim()] = plain(field[2]);
    const steps = [...body.matchAll(/^\|\s*(\d+)\s*\|\s*([^|]+)\|\s*([^|]+)\|$/gm)].map(row => ({
      title: `${row[1]}. ${plain(row[2])}`, detail: plain(row[3]), status: workingStatus(row[3]),
    }));
    items.push({ filename, title: plain(body.match(/^# (.+)$/m)?.[1] ?? filename), body, fields, steps });
  }
  return { items, warnings };
}
