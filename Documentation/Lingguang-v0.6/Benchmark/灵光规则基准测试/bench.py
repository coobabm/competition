"""《灵光一闪》规则基准测试：用最小参考模型检查三条底层规则是否仍能产生预期的涌现现象。
运行：python3 bench.py
每次修改规则或数值后运行一遍；某项从“通过”变成“未通过”，说明改动影响了涌现行为，需要人工判断是否接受。"""
import random, collections, sim
from sim import World
import os
for _k in ('LEAK_STORED','LEAK_LAST','COUNT_WHOLE_BEAT'):
    if _k in os.environ: setattr(sim,_k,os.environ[_k]=='1')

def build_two_paths(conv_shape):
    w=World(0,fill=0); w.n={}; w.edges={}; w.out=collections.defaultdict(list)
    nodes={'S':(0,0),'A':(1,0),'B1':(0,1),'B2':(1,1),'C':(2,0),'D':(3,0)}
    for k,sh in [('S','cone'),('A','conduct'),('B1','conduct'),('B2','conduct'),('C',conv_shape),('D','conduct')]:
        w.n[nodes[k]]=dict(shape=sh,rot=0)
    for a,b in [('S','A'),('S','B1'),('A','C'),('B1','B2'),('B2','C'),('C','D')]:
        w.edges[(nodes[a],nodes[b])]=dict(count=0,idle=0); w.out[nodes[a]].append(nodes[b])
    return w,nodes

def random_runs(fill,weights,nstart,seeds=60,rounds=30):
    st=dict(grew=0,recruit=0,drop=0,reverb=0,reverb_late=0,maxrep=0,maxbeat=0)
    for seed in range(seeds):
        w=World(seed,fill=fill,weights=weights); rnd=random.Random(seed+999)
        pool=sorted(w.n,key=lambda c:-len(w.out[c]))[:3]; hist=[]
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
        w=World(seed); s0=max(w.n,key=lambda c:len(w.out[c]))
        tot=[sum(w.round([s0])[0].values()) for _ in range(rounds)]
        grew+=sum(tot[-5:])>sum(tot[:5])
    return grew

def first_fire_round(conv_shape,confirms_per_round):
    w,nodes=build_two_paths(conv_shape)
    for r in range(15):
        f,_,_=w.round(None,confirms=[[nodes['S']]]*confirms_per_round)
        if nodes['C'] in f: return r+1
    return None

results=[]
def check(name,ok,detail): results.append((name,ok,detail)); print(('通过 ' if ok else '未通过'),name,'—',detail)

dense=random_runs(0.85,(6,3,1),2)
check('回荡是学会的',dense['reverb']>=30 and dense['reverb_late']>=0.7*dense['reverb'],
      f"{dense['reverb']}/60 个网络出现回荡，其中 {dense['reverb_late']} 个在第 3 轮后才出现")
check('多样经历带来成长',dense['grew']>=30,f"起点轮换时 {dense['grew']}/60 个网络活动量增长")
check('后期唤醒新灵光',dense['recruit']>=10,f"{dense['recruit']}/60 个网络后期出现早期没亮过的灵光")
sparse=random_runs(0.7,(5,3,1),1)
check('旧路径会被落下',sparse['drop']>=5,f"稀疏网络中 {sparse['drop']}/60 个出现前期常闪、后期不闪的灵光")
fg=fixed_start_growth()
check('只重复同一件事会停滞',fg<=5,f"固定起点时 {fg}/40 个网络增长")
check('没有失控',dense['maxrep']<=6 and dense['maxbeat']<=30,f"单个灵光一轮最多闪 {dense['maxrep']} 次，最长连锁 {dense['maxbeat']} 拍")
r=first_fire_round('cone',1)
check('时机对齐唤醒汇合点',r is not None and r>=4,f"两条长短路径在第 {r} 轮首次同拍点亮汇合点（期望在若干轮重复之后）")
r2=first_fire_round('conv',2)
check('高阈值汇合点不是死角',r2 is not None,f"每轮两次确认时，汇聚灵光在第 {r2} 轮首次闪烁")
print('\n汇总：',sum(1 for _,ok,_ in results if ok),'/',len(results),'项通过')
