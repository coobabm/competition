"""《灵光一闪》最小参考模型（规则基准）
只实现三条底层规则与关键属性，用于验证涌现行为，不是游戏代码：
  规则一：电点亮灵光，到达阈值闪烁（属性：漏电 LEAK、疲劳 REFIRE）
  规则二：闪烁的灵光通过光丝把电传给其他灵光（下一拍到达；髓鞘化后当拍到达）
  规则三：不传电的光丝变细，传电的光丝变粗（对方闪烁才算一次导通；扰动 = 本轮内短期增强 STP）
未包含：涟漪、流光、记忆折叠、区域与沟、计分。电量为内部单位（显示值 ×2）。
棋盘：半径 4 的六角网格，轴向坐标。"""
import random, collections
DIRS=[(1,-1),(1,0),(0,1),(-1,1),(-1,0),(0,-1)]  # axial, clockwise from upper-right
SHAPES={'conduct':([0],2,5,2),'cone':([0,2,4],4,10,2),'conv':([0],6,20,4)}
LEAK=1; REFIRE=2; DECAY_N=6; MAXBEAT=200
STP_STEP=1; STP_CAP=2   # 扰动：本轮内短期增强（内部单位）
# 语义开关（与 C# GameConfig 同名项对应；默认值 = 开发会话建议、C# 默认）：
#   LEAK_STORED       上一次确认留下的电，在本次连锁里被再次点亮前是否流失（C#: LeakStoredCharge）
#   LEAK_LAST         连锁最后一拍结束后是否还流失一次（C#: LeakAfterLastBeat）
#   COUNT_WHOLE_BEAT  导通按“本拍向它送过电的全部光丝”计（规格原文），而不只是让它越过阈值的那一批
# 原版 sim.py 相当于 True / True / False；三种组合下 bench.py 都是 8/8。
LEAK_STORED=False; LEAK_LAST=False; COUNT_WHOLE_BEAT=True
def cells(R):
    return [(q,r) for q in range(-R,R+1) for r in range(-R,R+1) if abs(q+r)<=R]
class World:
    def __init__(s,seed,R=4,fill=0.7,weights=(5,3,1)):
        rnd=random.Random(seed); s.n={}
        for c in cells(R):
            if rnd.random()<fill:
                sh=rnd.choices(list(SHAPES),weights)[0]
                s.n[c]=dict(shape=sh,rot=rnd.randrange(6))
        s.edges={}  # (a,b)->dict(count,idle)
        for a,v in s.n.items():
            dirs,_,_,_=SHAPES[v['shape']]
            for d in dirs:
                dq,dr=DIRS[(d+v['rot'])%6]; b=(a[0]+dq,a[1]+dr)
                if b in s.n: s.edges[(a,b)]=dict(count=0,idle=0)
        s.out=collections.defaultdict(list)
        for (a,b) in s.edges: s.out[a].append(b)
    def round(s,starters,confirms=None):
        confirms=confirms or [starters]
        s.marks=[]
        charge=collections.defaultdict(int); fires=collections.Counter()
        used=set(); events=[]; pending=collections.defaultdict(list)
        stp=collections.Counter()  # 本轮内每条光丝的短期增强
        def fire(n,beat,starter=False):
            fires[n]+=1; events.append((beat,n)); charge[n]=0
            _,_,_,out=SHAPES[s.n[n]['shape']]
            for b in s.out[n]:
                e=s.edges[(n,b)]; amt=out+(2 if starter else 0)+(2 if e['count']>=3 else 0)+stp[(n,b)]
                stp[(n,b)]=min(STP_CAP,stp[(n,b)]+STP_STEP)
                delay=0 if e['count']>=6 else 1
                pending[beat+delay].append((n,b,amt))
        beat_total=0
        for starters in confirms:
            pending.clear()
            beat=0; lit=set(); beat_intake=collections.defaultdict(list)
            for st in starters: fire(st,0,True)
            while beat<MAXBEAT:
                fired_this_beat=set(starters) if beat==0 else set(); beat_intake.clear()
                while pending.get(beat):
                    batch=pending.pop(beat); contrib=collections.defaultdict(list)
                    for a,b,amt in batch: charge[b]+=amt; contrib[b].append(a); lit.add(b); beat_intake[b].append(a)
                    newly=[]
                    for b,srcs in contrib.items():
                        thr=SHAPES[s.n[b]['shape']][1]+REFIRE*fires[b]
                        if b not in fired_this_beat and charge[b]>=thr:
                            newly.append(b)
                            for a in (beat_intake[b] if COUNT_WHOLE_BEAT else srcs): s.edges[(a,b)]['count']+=1; used.add((a,b))
                    for b in newly: fired_this_beat.add(b); fire(b,beat)
                last=not any(pending.get(x) for x in pending if x>beat)
                if LEAK_LAST or not last:
                    for k in list(charge):
                        if LEAK_STORED or k in lit: charge[k]=max(0,charge[k]-LEAK)
                beat+=1
                if not any(pending.values()): break
            beat_total=max(beat_total,beat)
            s.marks.append(len(events))
        beat=beat_total
        for k,e in s.edges.items():
            if k in used: e['idle']=0
            else:
                e['idle']+=1
                if e['idle']>=DECAY_N: e['count']=max(0,e['count']-3); e['idle']=0
        return fires,beat,events
