// Shared runtime for the 摆渡众包 game prototypes.
// Every game page calls LG.mount({...}) once; it builds the window chrome (title bar, header with the HUD, side panel,
// status bar with the debug skill slider) and returns the elements the game fills in.
// Money, samples and 灵光's skill are shared by every page through localStorage, so the hub shows the totals.
(function(){
  const KEY='lg-minigames';
  const load=()=>{ try{ return Object.assign({money:0,samples:0,manual:false,manualSkill:.25,played:{}},JSON.parse(localStorage.getItem(KEY)||'{}')); }catch(e){ return {money:0,samples:0,manual:false,manualSkill:.25,played:{}}; } };
  const G=load();
  const save=()=>localStorage.setItem(KEY,JSON.stringify(G));
  const $=s=>document.querySelector(s);
  const SPIDER='<div class="spider"><svg viewBox="0 0 30 30"><g stroke="#2F6F5E" stroke-width="1.2" fill="none"><path d="M11 12 L4 7 L1 11"/><path d="M11 15 L3 15 L0 18"/><path d="M11 18 L4 23 L2 27"/><path d="M19 12 L26 7 L29 11"/><path d="M19 15 L27 15 L30 18"/><path d="M19 18 L26 23 L28 27"/></g></svg><div class="b"></div></div>';

  const LG={
    G, SPIDER,
    rnd:n=>Math.floor(Math.random()*n),
    pick:a=>a[Math.floor(Math.random()*a.length)],
    shuffle:a=>{ a=a.slice(); for(let i=a.length-1;i>0;i--){ const j=Math.floor(Math.random()*(i+1)); [a[i],a[j]]=[a[j],a[i]]; } return a; },
    /** 灵光's skill 0..1: rises with the samples it was given (from 25%), or the debug slider's value. */
    skill(){ return G.manual?G.manualSkill:Math.min(.97,.25+.72*(1-Math.exp(-G.samples/90))); },
    /** True with probability skill(): use it for "does 灵光 get this one right". */
    right(){ return Math.random()<LG.skill(); },
    /** Credit samples and money (money may be negative); optionally float the amount over an element. */
    give(samples,money,el,color){
      G.samples=Math.max(0,G.samples+(samples||0)); G.money+=money||0; save(); LG.hud();
      if(el&&money) LG.float(el,(money>0?'+':'−')+'¥'+Math.abs(money).toFixed(1),color||(money>0?'var(--red)':'#999'));
    },
    hud(){
      if(!$('#lgMoney')) return;
      $('#lgMoney').textContent='¥'+G.money.toFixed(1); $('#lgSamp').textContent=G.samples;
      const s=LG.skill(); $('#lgSkill').style.width=s*100+'%'; $('#lgSkillT').textContent=Math.round(s*100)+'%';
    },
    /** Floating text over an element, or at window coordinates {x,y} (in the 1236×693 window space). */
    float(target,text,color){
      const w=$('#win'), f=document.createElement('div'); f.className='float'; f.textContent=text; f.style.color=color||'var(--red)';
      if(target&&target.getBoundingClientRect){ const r=target.getBoundingClientRect(), wr=w.getBoundingClientRect(), k=wr.width/1236; f.style.left=((r.left-wr.left)/k+r.width/k/2-24)+'px'; f.style.top=((r.top-wr.top)/k)+'px'; }
      else { f.style.left=(target?target.x:600)+'px'; f.style.top=(target?target.y:300)+'px'; }
      w.appendChild(f); setTimeout(()=>f.remove(),1100);
    },
    /** Convert a pointer event to coordinates inside an element, in that element's unscaled CSS pixels. */
    local(e,el){ const r=el.getBoundingClientRect(), k=r.width/el.offsetWidth; return {x:(e.clientX-r.left)/k, y:(e.clientY-r.top)/k}; },
    /** 灵光's speech bubble in the side panel. */
    say(text){ const b=$('#lgSay'); if(b) b.textContent=text; },
    shake(el){ el.classList.remove('shake'); void el.offsetWidth; el.classList.add('shake'); },
    /**
     * Build the page. opts: {id, title, subtitle, icon, dataset, rule (html), stats: [[label,id]], sideExtra (html)}
     * Returns {stage, side}. The stage is an empty .box filling the left column.
     */
    mount(o){
      G.played[o.id]=1; save();
      document.title='摆渡众包 · '+o.title;
      const win=document.createElement('div'); win.className='win'; win.id='win';
      win.innerHTML=`
        <div class="title"><span class="ico">摆</span>摆渡众包 - 游戏标注 · ${o.title}<span class="sp"></span><i>—</i><i>□</i><i class="x">✕</i></div>
        <div class="hd">
          <div class="logo"><div class="paw">${o.icon||'摆'}</div><div><b>${o.title}</b><small>${o.subtitle||''}</small></div></div>
          <a class="back" href="hub.html">← 全部游戏</a>
          <div class="hud">
            <span class="chip">标的是：<b>${o.dataset||''}</b></span>
            <span class="chip money">余额 <b id="lgMoney">¥0</b></span>
            <span class="chip">样本 <b id="lgSamp">0</b></span>
            <span class="skill">灵光水平 <span class="bar"><i id="lgSkill"></i></span><b id="lgSkillT">25%</b></span>
          </div>
        </div>
        <div class="bd">
          <div class="box stage" id="stage"></div>
          <div class="side" id="side">
            ${o.stats?`<div class="box"><div class="bh">这一局 <small>${o.statsNote||''}</small></div><div class="kv">${o.stats.map(s=>`<span>${s[0]}</span><b id="${s[1]}">${s[2]!=null?s[2]:'0'}</b>`).join('')}</div><div class="hist" id="lgHist"></div></div>`:''}
            <div class="box ai"><div class="who">${SPIDER}<div>${o.aiName||'灵光'}</div></div><div class="bubble" id="lgSay">${o.aiHello||'……'}</div></div>
            ${o.rule?`<div class="box rule">${o.rule}</div>`:''}
            ${o.sideExtra||''}
          </div>
        </div>
        <div class="st"><span>● ${o.footer||'游戏标注 · 玩着玩着就标完了'}</span><span class="sp"></span>
          <label><input type="checkbox" id="lgDbg"> 调试：手动设定灵光水平</label><input type="range" id="lgDbgR" min="0" max="100"> <span id="lgDbgV"></span>
          <a href="#" id="lgReset" style="color:var(--mute)">清空进度</a></div>`;
      document.body.appendChild(win);
      $('#lgDbg').checked=G.manual; $('#lgDbgR').disabled=!G.manual; $('#lgDbgR').value=Math.round(G.manualSkill*100); $('#lgDbgV').textContent=Math.round(G.manualSkill*100)+'%';
      $('#lgDbg').onchange=e=>{ G.manual=e.target.checked; $('#lgDbgR').disabled=!G.manual; save(); LG.hud(); LG.onSkill&&LG.onSkill(); };
      $('#lgDbgR').oninput=e=>{ G.manualSkill=e.target.value/100; $('#lgDbgV').textContent=e.target.value+'%'; save(); LG.hud(); LG.onSkill&&LG.onSkill(); };
      $('#lgReset').onclick=e=>{ e.preventDefault(); if(confirm('清空所有小游戏的余额和样本？')){ localStorage.removeItem(KEY); location.reload(); } };
      const fit=()=>{ const k=Math.min(innerWidth/1260,innerHeight/715,1.4); win.style.transform=`translate(-50%,-50%) scale(${k})`; LG.onResize&&LG.onResize(); };
      addEventListener('resize',fit); fit(); LG.hud();
      return {stage:$('#stage'), side:$('#side')};
    },
    /** Append a result square to the side history (cls: ok | no | ai). */
    hist(cls,label){ const h=$('#lgHist'); if(!h) return; const i=document.createElement('i'); i.className=cls; i.textContent=label||(cls==='ok'?'✓':cls==='no'?'✗':'灵'); h.appendChild(i); while(h.children.length>24) h.firstChild.remove(); },
    set(id,v){ const e=document.getElementById(id); if(e) e.textContent=v; },
  };
  window.LG=LG;
})();
