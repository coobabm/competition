"""在真实棋盘（42 格、沟与窄桥）上跑 bench.py 的前 6 项涌现检查。
bench.py 用的是半径 4 的 61 格六角网格；这里换成游戏里真正的大脑形棋盘，看规则在真实地形上是否仍成立。
运行：python3 board_bench.py"""
import random, collections
from board_ref import board_world, random_layout

def random_runs(fill,weights,nstart,seeds=60,rounds=30):
    st=dict(grew=0,recruit=0,drop=0,reverb=0,reverb_late=0,maxrep=0,maxbeat=0)
    for seed in range(seeds):
        rnd=random.Random(seed); w=board_world(random_layout(rnd,fill,weights)); rnd=random.Random(seed+999)
        pool=sorted(w.n,key=lambda c:(-len(w.out[c]),c[1],c[0]))[:3]; hist=[]
        for r in range(rounds):
            f,b,_=w.round(rnd.sample(pool,nstart)); hist.append(f)
            st['maxrep']=max(st['maxrep'],max(f.values())); st['maxbeat']=max(st['maxbeat'],b)
        early=set().union(*hist[:5]); late=set().union(*hist[-5:]); tot=[sum(f.values()) for f in hist]
        st['grew']+=sum(tot[-5:])>sum(tot[:5]); st['recruit']+=bool(late-early); st['drop']+=bool(early-late)
        rev=[r for r,f in enumerate(hist) if max(f.values())>1]
        if rev: st['reverb']+=1; st['reverb_late']+=rev[0]>=3
    return st

def fixed_start_growth(seeds=40,rounds=30):
    grew=0
    for seed in range(seeds):
        w=board_world(random_layout(random.Random(seed),0.7,(5,3,1)))
        s0=sorted(w.n,key=lambda c:(-len(w.out[c]),c[1],c[0]))[0]
        tot=[sum(w.round([s0])[0].values()) for _ in range(rounds)]
        grew+=sum(tot[-5:])>sum(tot[:5])
    return grew

if __name__=='__main__':
    dense=random_runs(0.85,(6,3,1),2); sparse=random_runs(0.7,(5,3,1),1); fg=fixed_start_growth()
    rows=[('回荡是学会的',dense['reverb']>=30 and dense['reverb_late']>=0.7*dense['reverb'],f"{dense['reverb']}/60 出现回荡，其中 {dense['reverb_late']} 个在第 3 轮后"),
          ('多样经历带来成长',dense['grew']>=30,f"{dense['grew']}/60 增长"),
          ('后期唤醒新灵光',dense['recruit']>=10,f"{dense['recruit']}/60"),
          ('旧路径会被落下',sparse['drop']>=5,f"{sparse['drop']}/60"),
          ('只重复同一件事会停滞',fg<=5,f"固定起点 {fg}/40 增长"),
          ('没有失控',dense['maxrep']<=6 and dense['maxbeat']<=30,f"单灵光一轮最多闪 {dense['maxrep']} 次，最长 {dense['maxbeat']} 拍")]
    for n,ok,d in rows: print('通过 ' if ok else '未通过',n,'—',d)
    print('汇总：',sum(ok for _,ok,_ in rows),'/',len(rows))
