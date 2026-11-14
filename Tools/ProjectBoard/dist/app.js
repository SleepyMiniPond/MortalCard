'use strict';
(() => {
  let data = null;
  const main = document.querySelector('#main');
  const escape = value => String(value ?? '').replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);
  const clean = value => String(value ?? '').replace(/\[([^\]]+)\]\([^)]+\)/g, '$1').replace(/[*`]/g, '');
  const labels = { active: '進行中', partial: '部分完成', planned: '待安排', done: '已完成', review: '待使用者確認' };
  let pending = [], completed = [], current, next;
  let loading = false;
  const dialog = document.querySelector('#detail');
  let lastFocus;
  const badge = status => `<span class="badge ${status}">${status === 'done' ? '✓ ' : status === 'active' ? '● ' : ''}${labels[status]}</span>`;
  const taskButton = (task, className, body) => `<button class="${className}" data-task="${task.id}">${body}</button>`;
  const heading = (title, description) => `<div class="page-intro"><div><h1>${title}</h1><p>${description}</p></div></div>`;
  const intro = task => task.summary || clean(task.fields['工作'] || task.fields['剩餘工作'] || '查看任務的範圍、決策與完成條件。');

  function link(label, reference, source) {
    const title = escape(clean(label));
    if (/^https?:\/\//i.test(reference)) return `<a href="${escape(reference)}" target="_blank" rel="noopener noreferrer">${title} ↗</a>`;
    let url;
    try { url = new URL(reference, new URL(`../../../Document/${source}`, location.href)); } catch { return title; }
    const filename = decodeURIComponent(url.pathname.split('/').pop());
    const workItem = data.working?.items.find(item => item.filename === filename);
    if (workItem && (data.working?.items.some(item => item.filename === source) || reference.includes('.agents/working/'))) return '<a href="#work/' + encodeURIComponent(filename) + (url.hash ? '/' + encodeURIComponent(decodeURIComponent(url.hash.slice(1))) : '') + '" data-document-link>' + title + '</a>';
    if (data.documents[filename] && url.pathname.includes('/Document/')) return `<a href="#notes/${encodeURIComponent(filename)}${url.hash ? '/' + encodeURIComponent(decodeURIComponent(url.hash.slice(1))) : ''}" data-document-link>${title}</a>`;
    return `<span class="file-link" title="${escape(reference)}">${title}</span>`;
  }

  function inline(text, source) {
    const tokens = [];
    const token = html => { tokens.push(html); return `\u0000${tokens.length - 1}\u0000`; };
    let output = text.replace(/`([^`]+)`/g, (_, code) => token(`<code>${escape(code)}</code>`));
    output = output.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_, label, reference) => token(link(label, reference, source)));
    output = escape(output).replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    return output.replace(/\u0000(\d+)\u0000/g, (_, index) => tokens[Number(index)]);
  }

  const slug = text => clean(text).toLowerCase().replace(/[\p{P}\p{S}]/gu, character => /[-_]/.test(character) ? character : '').replace(/ /g, '-');
  function markdown(text, source = 'TODO.md') {
    const lines = text.split('\n');
    const output = [];
    for (let index = 0; index < lines.length;) {
      const line = lines[index];
      if (!line.trim()) { index++; continue; }
      if (/^```/.test(line)) {
        const code = [];
        index++;
        while (index < lines.length && !/^```/.test(lines[index])) code.push(lines[index++]);
        output.push(`<pre><code>${escape(code.join('\n'))}</code></pre>`); index++; continue;
      }
      const title = line.match(/^(#{1,6}) (.+)$/);
      if (title) { const level = title[1].length; output.push(`<h${level} id="${escape(slug(title[2]))}">${inline(title[2], source)}</h${level}>`); index++; continue; }
      if (/^\|/.test(line)) {
        const rows = [];
        while (index < lines.length && /^\|/.test(lines[index])) rows.push(lines[index++]);
        const cells = row => row.replace(/^\||\|$/g, '').split('|').map(cell => cell.trim());
        const table = rows.filter(row => !/^\|[\s:|-]+\|$/.test(row));
        output.push(`<div class="table-wrap"><table>${table.map((row, rowIndex) => `<tr>${cells(row).map(cell => `<${rowIndex ? 'td' : 'th'}>${inline(cell, source)}</${rowIndex ? 'td' : 'th'}>`).join('')}</tr>`).join('')}</table></div>`); continue;
      }
      if (/^>/.test(line)) {
        const quotes = [];
        while (index < lines.length && /^>/.test(lines[index])) quotes.push(lines[index++].replace(/^>\s?/, ''));
        output.push(`<blockquote>${quotes.map(quote => inline(quote, source)).join('<br>')}</blockquote>`); continue;
      }
      if (/^(?:- |\d+\. )/.test(line)) {
        const ordered = /^\d/.test(line); const items = []; const pattern = ordered ? /^\d+\. (.+)$/ : /^- (.+)$/;
        while (index < lines.length && pattern.test(lines[index])) items.push(lines[index++].replace(pattern, '$1'));
        output.push(`<${ordered ? 'ol' : 'ul'}>${items.map(item => `<li>${inline(item, source)}</li>`).join('')}</${ordered ? 'ol' : 'ul'}>`); continue;
      }
      if (/^---\s*$/.test(line)) { output.push('<hr>'); index++; continue; }
      const paragraph = [line]; index++;
      while (index < lines.length && lines[index].trim() && !/^(?:#|\||>|- |\d+\. |```)/.test(lines[index])) paragraph.push(lines[index++]);
      output.push(`<p>${inline(paragraph.join(' '), source)}</p>`);
    }
    return output.join('');
  }

  function stepList(task) {
    return `<ol class="steps">${task.steps.map((step, index) => `<li class="step ${step.status}"><span class="step-marker">${step.status === 'done' ? '✓' : index + 1}</span><div><strong>${escape(step.title.replace(/^\d+\. /, ''))}</strong><small>${escape(step.detail)}</small></div></li>`).join('')}</ol>`;
  }
  function showTask(id) {
    const task = data.tasks.find(task => task.id === id);
    if (!task) return;
    lastFocus = document.activeElement;
    document.querySelector('#detail-eyebrow').textContent = `${task.id} / 任務詳細`;
    const dependencies = task.dependencies.map(id => {
      const dependency = data.tasks.find(item => item.id === id);
      return dependency ? taskButton(dependency, 'button secondary-button', `${escape(id)} · ${escape(dependency.title)} ${dependency.status === 'done' ? '✓' : ''}`) : escape(id);
    }).join(' ');
    document.querySelector('#detail-body').innerHTML = `${badge(task.status)}<h2 id="detail-title">${escape(task.title)}</h2><p class="summary">${escape(intro(task))}</p>${task.status === 'done' ? '<div class="info-strip">這是當時完成範圍與驗證紀錄，並非本次重新執行測試的結果。</div>' : ''}${dependencies ? `<div class="details-block"><h3>開始前需要</h3><div class="hero-actions">${dependencies}</div></div>` : ''}${task.steps.length ? `<div class="details-block"><h3>工作步驟</h3>${stepList(task)}</div>` : ''}${task.checks.length ? `<div class="details-block"><h3>剩餘驗收 · ${task.checks.length} 個檢查</h3><p class="note">這些項目尚待實際驗收；看板不會將點閱視為完成。</p><ol class="checklist">${task.checks.map(check => `<li>${escape(check)}</li>`).join('')}</ol></div>` : ''}<details class="details-block" ${!task.steps.length && !task.checks.length ? 'open' : ''}><summary>範圍、決策與原始紀錄</summary><div class="markdown">${markdown(task.body, task.source)}</div></details>`;
    if (!dialog.open) dialog.showModal();
    dialog.scrollTop = 0;
  }
  function closeDialog() { dialog.close(); lastFocus?.focus(); }
  document.querySelector('#close-detail').addEventListener('click', closeDialog);
  dialog.addEventListener('click', event => { if (event.target === dialog) { const box = dialog.getBoundingClientRect(); if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) closeDialog(); } });

  function home() {
    if (!current) return heading('進度總覽', '目前沒有待辦，可從已完成任務查看歷史紀錄。');
    const recent = completed.slice(-2);
    const route = [...recent, current, ...(next ? [next] : [])];
    const other = pending.filter(task => ![current.id, next?.id].includes(task.id));
    return `${heading('進度總覽', '目前主線、工作步驟與後續待辦。')}
      <section class="hero" aria-label="目前工作"><div><div class="hero-label"><span class="local-dot"></span>目前主線 <span class="task-id">${current.id}</span>${badge(current.status)}</div><h2>${escape(current.title)}</h2><p>${escape(clean(current.fields['狀態'] ?? intro(current)))}</p><div class="hero-actions">${taskButton(current, 'button', '查看步驟與驗收 <span aria-hidden="true">↗</span>')}<a href="#notes/SystemArchitecture.md" class="quiet-link">閱讀架構總覽 →</a></div></div></section>
      <div class="stats"><div class="stat"><span class="stat-icon">✓</span><div><strong>${completed.length}</strong><small>項任務已完成</small></div></div><div class="stat"><span class="stat-icon">◉</span><div><strong>${pending.filter(task => task.status === 'active').length}</strong><small>項正在進行</small></div></div><div class="stat"><span class="stat-icon">▤</span><div><strong>${pending.filter(task => task.status !== 'active').length}</strong><small>項待安排</small></div></div></div>
      <section><div class="section-head"><h2>主線進度</h2><a href="#history" class="quiet-link">查看已完成任務 →</a></div><div class="roadmap">${route.map(task => taskButton(task, `route-card ${task.id === current.id ? 'current' : ''}`, `<div class="route-top"><span class="task-id">${task.id}</span>${badge(task.status)}</div><h3>${escape(task.title)}</h3><span class="route-caption">${task.id === current.id ? '目前工作' : task.id === next?.id ? '下一主線 · 前置完成後開始' : '已確認完成'}</span>`)).join('')}</div></section>
      <div class="lower-grid"><section class="panel"><div class="section-head"><h2>目前任務步驟</h2><span class="task-id">${current.id}</span></div>${current.steps.length ? stepList(current) : `<p class="summary">${escape(intro(current))}</p>`}<p class="note">${current.checks.length ? `目前還有 ${current.checks.length} 個驗收檢查。` : ''}完成實作、通過驗證與你的確認，分開記錄。</p></section><section class="panel"><div class="section-head"><h2>後續待辦</h2><a href="#tasks" class="quiet-link">全部 →</a></div>${[next, ...other].filter(Boolean).slice(0, 3).map((task, index) => taskButton(task, 'next-task', `<span class="task-id">${task.id} · ${index === 0 ? '下一主線' : '獨立待辦'}</span><span class="arrow">↗</span><h3>${escape(task.title)}</h3><p>${escape(intro(task))}</p>`)).join('')}</section></div>`;
  }

  const taskCard = task => taskButton(task, 'task-card', `<div class="route-top"><span class="task-id">${task.id}</span>${badge(task.status)}</div><h3>${escape(task.title)}</h3><p>${escape(intro(task))}</p>${task.steps.length ? `<div class="mini-progress" aria-label="${task.steps.filter(step => step.status === 'done').length} 個工作已完成，共 ${task.steps.length} 個">${task.steps.map(step => `<span class="${step.status}"></span>`).join('')}</div>` : ''}<div class="card-meta"><span>${task.steps.length ? `${task.steps.length} 個工作步驟` : task.dependencies.length ? `前置：${task.dependencies.join('、')}` : task.status === 'done' ? '已封存 · 查看完成範圍' : '獨立排期'}</span><span>查看細節 ↗</span></div>`);
  function listing(history = false) {
    main.innerHTML = heading(history ? '已完成任務' : '任務與步驟', history ? '各任務的完成範圍與歷史驗證紀錄。' : '依狀態或關鍵字篩選，點選任務查看步驟與完成條件。') + `<div class="toolbar"><input class="search" id="task-search" type="search" placeholder="搜尋任務、關鍵字或 T 編號…" aria-label="搜尋任務">${history ? '' : '<select id="status-filter" aria-label="依任務狀態篩選"><option value="all">全部狀態</option><option value="active">進行中</option><option value="partial">部分完成</option><option value="planned">待安排</option></select>'}</div><p id="result-count" class="result-count" aria-live="polite"></p><div id="task-results" class="task-grid"></div>`;
    const render = () => {
      const query = document.querySelector('#task-search').value.trim().toLocaleLowerCase();
      const status = document.querySelector('#status-filter')?.value ?? 'all';
      const results = (history ? [...completed].reverse() : pending).filter(task => (status === 'all' || task.status === status) && `${task.id} ${task.title} ${task.body}`.toLocaleLowerCase().includes(query));
      document.querySelector('#result-count').textContent = `${results.length} 個${history ? '已完成項目' : '任務'}${query ? ` · 搜尋「${query}」` : ''}`;
      document.querySelector('#task-results').innerHTML = results.length ? results.map(taskCard).join('') : '<div class="empty">暫時找不到符合的任務。試試其他關鍵字或狀態。</div>';
    };
    document.querySelector('#task-search').addEventListener('input', render);
    document.querySelector('#status-filter')?.addEventListener('change', render);
    render();
  }

  const systems = [
    ['01 / CONTENT', '內容與配置', '卡牌、Buff、目標、數值和條件，組成遊戲規則的積木。', 'GameData.md'],
    ['02 / STATE', '卡片與戰鬥狀態', 'Data → Instance → Entity，分開配置、跨戰鬥狀態與當場實體。', 'Entity.md'],
    ['03 / RULES', '戰鬥與效果', '回合、出牌、效果佇列與反應；遊戲真正改變狀態的地方。', 'GameModel.md'],
    ['04 / COORDINATION', '操作與協調', '接收玩家命令、收集選取，監督戰鬥與 UI 的生命週期。', 'Presenter.md'],
    ['05 / PRESENTATION', '畫面與互動', '手牌拖曳、角色動畫、提示與彈窗，把結果呈現在畫面。', 'GameView.md'],
    ['06 / SCENES', '場景流程', '從選單到地圖與戰鬥；勝利離場、重試和戰後資料還待接齊。', 'Scene.md'],
  ];
  function systemPage() {
    return heading('系統地圖', '系統職責與資料流向。點選系統閱讀詳細文件。') + '<div class="flow">內容配置 → 戰鬥模型 → 效果結算 → 遊戲事件 → 畫面呈現<br>玩家操作 → Presenter → 選取與驗證 → 正式執行</div><div class="system-grid">' + systems.map(([number, title, summary, file]) => `<a class="system-card" href="#notes/${file}"><span>${number}</span><h2>${title}</h2><p>${summary}</p><p class="note">閱讀系統說明 ↗</p></a>`).join('') + '</div>';
  }
  function notesPage(filename, anchor) {
    if (filename) {
      const doc = data.documents[filename];
      main.innerHTML = doc ? `<article class="reading"><a class="quiet-link" href="#notes">← 回到專案筆記</a><div class="markdown">${markdown(doc.body, filename)}</div></article>` : '<p class="empty">找不到這份文件。<a href="#notes">回到文件列表</a></p>';
      if (anchor) main.querySelector(`[id="${CSS.escape(anchor)}"]`)?.scrollIntoView({ block: 'start' });
      return;
    }
    main.innerHTML = heading('專案筆記', '搜尋架構、遊戲規則與協作文件。') + '<div class="toolbar"><input class="search" id="doc-search" type="search" placeholder="搜尋筆記名稱或內文…" aria-label="搜尋筆記"></div><p class="result-count" id="doc-count" aria-live="polite"></p><div class="document-list" id="doc-results"></div>';
    const render = () => {
      const query = document.querySelector('#doc-search').value.trim().toLocaleLowerCase();
      const files = Object.entries(data.documents).filter(([name, doc]) => `${name} ${doc.title} ${doc.body}`.toLocaleLowerCase().includes(query));
      document.querySelector('#doc-count').textContent = `${files.length} 份筆記`;
      document.querySelector('#doc-results').innerHTML = files.map(([name, doc]) => `<a class="doc-link" href="#notes/${encodeURIComponent(name)}"><span>${escape(doc.title)}<small>${escape(name)}</small></span><span aria-hidden="true">↗</span></a>`).join('') || '<div class="empty">沒有找到符合的筆記，換個關鍵字試試。</div>';
    };
    document.querySelector('#doc-search').addEventListener('input', render); render();
  }

  function workingPage(filename, anchor) {
    const items = data.working?.items ?? [];
    if (filename) {
      const item = items.find(item => item.filename === filename);
      main.innerHTML = item ? '<article class="reading"><a class="quiet-link" href="#work">← 目前工作</a><div class="markdown">' + markdown(item.body, item.filename) + '</div></article>' : '<p class="empty">目前工作索引中沒有這份文件。</p>';
      if (anchor) main.querySelector('[id="' + CSS.escape(anchor) + '"]')?.scrollIntoView({ block: 'start' });
      return;
    }
    main.innerHTML = heading('目前工作', '目前正在處理的草稿與計畫。進度直接讀取原始工作計畫。') +
      (data.working?.warnings ?? []).map(warning => '<p class="info-strip">' + escape(warning) + '</p>').join('') +
      (items.length ? items.map(item => '<section class="panel working-plan"><h2>' + escape(item.title) + '</h2><p class="note">.agents/working/' + escape(item.filename) + '</p><dl class="work-summary">' + ['目前步驟', '最近完成', '下一步', '驗證結果', '待確認事項'].map(key => '<div><dt>' + key + '</dt><dd>' + escape(item.fields[key] ?? (key === '目前步驟' ? item.fields['目前'] : undefined) ?? '未記錄') + '</dd></div>').join('') + '</dl>' + (item.steps.length ? stepList(item) : '') + '<a class="button secondary-button plan-link" href="#work/' + encodeURIComponent(item.filename) + '">閱讀完整計畫</a></section>').join('') : '<div class="empty">目前沒有登記中的草稿或計畫。來源：.agents/working/目前工作.md</div>');
  }

  async function loadProject() {
    if (loading) return;
    const reload = document.querySelector('#reload-project');
    const status = document.querySelector('#reader-status');
    if (location.protocol === 'file:') {
      main.innerHTML = '<p class="error">請雙擊專案根目錄的「啟動專案閱讀器.cmd」。此版本需要本機服務直接讀取文件。</p>';
      return;
    }
    loading = true; reload.disabled = true; reload.textContent = '讀取中…';
    status.className = ''; status.textContent = '正在讀取磁碟上的文件…';
    try {
      const response = await fetch('/api/project', { cache: 'no-store', signal: AbortSignal.timeout(15000) });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error ?? '無法讀取文件。');
      if (!Array.isArray(result.tasks) || !result.documents) throw new Error('服務傳回的文件資料格式不正確。');
      data = result;
      pending = data.tasks.filter(task => task.status !== 'done');
      completed = data.tasks.filter(task => task.status === 'done');
      current = pending.find(task => task.status === 'active') ?? pending[0];
      next = pending.find(task => task.id !== current?.id && task.dependencies.includes(current?.id)) ?? pending.find(task => task.id !== current?.id);
      document.querySelector('#pending-count').textContent = pending.length;
      document.querySelector('#work-count').textContent = data.working?.items.length ?? 0;
      document.querySelector('#source-date').textContent = 'TODO 更新 ' + data.sourceDate;
      const time = new Date(data.readAt).toLocaleTimeString('zh-TW', { hour12: false, timeZone: 'Asia/Taipei' });
      status.textContent = '已讀取 ' + Object.keys(data.documents).length + ' 份文件 · ' + time + ((data.warnings?.length) ? ' · ' + data.warnings.join('；') : '');
      route();
    } catch (error) {
      status.className = 'read-error';
      status.textContent = '讀取失敗：' + error.message + (data ? ' 目前顯示上次成功讀取的內容。' : ' 請確認本機服務已啟動。');
      if (!data) main.innerHTML = '<div class="error">尚未載入文件。<button class="button secondary-button" data-reload>重新讀取</button></div>';
    } finally { loading = false; reload.disabled = false; reload.textContent = '重新讀取'; }
  }


  async function refreshWorking() {
    if (loading || !data || document.hidden || !location.hash.startsWith('#work')) return;
    try {
      const response = await fetch('/api/working', { cache: 'no-store', signal: AbortSignal.timeout(4000) });
      if (!response.ok) throw new Error('無法同步工作計畫。');
      const working = await response.json();
      if (JSON.stringify(working) === JSON.stringify(data.working)) return;
      data.working = working;
      document.querySelector('#work-count').textContent = working.items.length;
      const parts = location.hash.slice(1).split('/').map(part => decodeURIComponent(part));
      const scroll = window.scrollY;
      workingPage(parts[1], parts[2]);
      if (!parts[2]) window.scrollTo(0, scroll);
      const status = document.querySelector('#reader-status');
      status.className = '';
      status.textContent = '工作計畫已重新讀取 · ' + new Date().toLocaleTimeString('zh-TW', { hour12: false, timeZone: 'Asia/Taipei' });
    } catch {
      const status = document.querySelector('#reader-status');
      status.className = 'read-error';
      status.textContent = '工作計畫同步失敗，目前顯示上次讀取的內容。請確認服務仍在執行。';
    }
  }

  function route() {
    if (dialog.open) dialog.close();
    const parts = location.hash.slice(1).split('/').map(part => { try { return decodeURIComponent(part); } catch { return ''; } });
    const page = ['tasks', 'systems', 'history', 'notes', 'work'].includes(parts[0]) ? parts[0] : 'home';
    document.querySelectorAll('[data-page]').forEach(link => { const selected = link.dataset.page === page; link.classList.toggle('active', selected); selected ? link.setAttribute('aria-current', 'page') : link.removeAttribute('aria-current'); });
    const names = { home: '進度總覽', tasks: '任務與步驟', systems: '系統地圖', history: '已完成任務', notes: '專案筆記', work: '目前工作' };
    document.querySelector('#breadcrumb').textContent = names[page];
    document.title = `${names[page]} · MortalGame 專案進度`;
    window.scrollTo(0, 0);
    if (!data) return;
    if (page === 'work') workingPage(parts[1], parts[2]);
    else if (page === 'tasks' || page === 'history') listing(page === 'history');
    else if (page === 'notes') notesPage(parts[1], parts[2]);
    else main.innerHTML = page === 'systems' ? systemPage() : home();
  }
  document.addEventListener('click', event => {
    const button = event.target.closest('[data-task]');
    if (button) showTask(button.dataset.task);
    if (event.target.closest('[data-document-link]') && dialog.open) dialog.close();
  });
  document.querySelector('#reload-project').addEventListener('click', loadProject);
  document.addEventListener('click', event => { if (event.target.closest('[data-reload]')) loadProject(); });
  window.addEventListener('hashchange', route);
  route();
  loadProject();
  setInterval(refreshWorking, 5000);
  setInterval(() => { if (location.protocol !== 'file:') fetch('/api/health', { cache: 'no-store' }).catch(() => {}); }, 60000);
})();
