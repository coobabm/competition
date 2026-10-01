"""把参考模型 sim.py 放到《灵光一闪》真实棋盘上（odd-r、42 格、中线与外侧沟），生成跨实现一致性用例。
C# 的 ReferenceConsistencyTests 用同样的布局与起点重放，逐拍比较每次闪光。
运行：python3 board_ref.py  → 生成 ReferenceCases.txt（并打印摘要）"""
import random, collections, json
import sim

ROWS=[". . . F F F F . . .",". . F F F F F F . .","T T C C C C C C T T","T T K C C C K . T T",
      ". . K K K K K . . .",". . I I I I I I . .",". . . . I I . . . ."]
GRID={}
for r,row in enumerate(ROWS):
    for c,ch in enumerate(row.split()):
        if ch!='.': GRID[(c,r)]=ch
EVEN=[(0,-1),(1,0),(0,1),(-1,1),(-1,0),(-1,-1)]
ODD=[(1,-1),(1,0),(1,1),(0,1),(-1,0),(0,-1)]
def nb(c,d):
    dc,dr=(ODD if c[1]%2 else EVEN)[d]; return (c[0]+dc,c[1]+dr)
GAPS={frozenset([(1,2),(2,2)]),frozenset([(7,2),(8,2)])}
def groove(a,b):
    """中年阶段：中线只在行 5/3/1 有窄桥；T 区只在行 2 两处缺口与外界相连。"""
    if {a[0],b[0]}=={4,5} and not (a[1]==b[1] and a[1] in (5,3,1)): return True
    if (GRID[a]=='T')!=(GRID[b]=='T') and frozenset([a,b]) not in GAPS: return True
    return False

def board_world(layout):
    """layout: {cell:(shape,rot)}；光丝按真实棋盘的几何生成（与 C# EdgeGeometry 相同）。"""
    w=sim.World(0,fill=0); w.n={c:dict(shape=sh,rot=o) for c,(sh,o) in layout.items()}
    w.edges={}; w.out=collections.defaultdict(list)
    for a,v in w.n.items():
        dirs=sim.SHAPES[v['shape']][0]
        for d in dirs:
            b=nb(a,(d+v['rot'])%6)
            if b in w.n and not groove(a,b):
                w.edges[(a,b)]=dict(count=0,idle=0); w.out[a].append(b)
    return w

def random_layout(rnd,fill,weights):
    out={}
    for c in sorted(GRID,key=lambda c:(c[1],c[0])):
        if rnd.random()<fill: out[c]=(rnd.choices(list(sim.SHAPES),weights)[0],rnd.randrange(6))
    return out

def make_cases(nseeds=24,rounds=20):
    cases=[]
    for seed in range(nseeds):
        rnd=random.Random(1000+seed)
        layout=random_layout(rnd,0.8 if seed%2==0 else 0.65,(6,3,1))
        w=board_world(layout)
        pool=sorted(w.n,key=lambda c:(-len(w.out[c]),c[1],c[0]))[:3]
        rec=[]
        for r in range(rounds):
            if r%4==3:   # 每 4 轮里有 1 轮做两次确认，检验确认之间的电量语义
                confirms=[[rnd.choice(pool)],[rnd.choice(pool)]]
            else:
                confirms=[rnd.sample(pool,rnd.choice([1,2]))]
            f,b,ev=w.round(None,confirms=confirms)
            fires=[];start=0
            for ci,end in enumerate(w.marks):
                fires+= [[ci,beat,c[0],c[1]] for beat,c in ev[start:end]]; start=end
            fires.sort()
            counts=sorted([a[0],a[1],b_[0],b_[1],e['count']] for (a,b_),e in w.edges.items() if e['count']>0)
            rec.append(dict(confirms=[[list(c) for c in cf] for cf in confirms],fires=fires,counts=counts))
        cases.append(dict(seed=seed,layout=[[c[0],c[1],sh,o] for c,(sh,o) in sorted(layout.items(),key=lambda kv:(kv[0][1],kv[0][0]))],rounds=rec))
    return cases


def write_text(cases,path):
    """C# 测试读取的纯文本格式（不依赖 JSON 库，Unity 与 dotnet 都能直接解析）。"""
    L=['# 由 board_ref.py 生成，勿手改。flags LEAK_STORED=%d LEAK_LAST=%d COUNT_WHOLE_BEAT=%d'%(sim.LEAK_STORED,sim.LEAK_LAST,sim.COUNT_WHOLE_BEAT)]
    for c in cases:
        L.append('case %d'%c['seed'])
        for col,row,sh,o in c['layout']: L.append('glow %d %d %s %d'%(col,row,sh,o))
        for r in c['rounds']:
            L.append('round')
            for cf in r['confirms']: L.append('confirm '+' '.join('%d %d'%(x,y) for x,y in cf))
            for ci,beat,col,row in r['fires']: L.append('fire %d %d %d %d'%(ci,beat,col,row))
            for ac,ar,bc,br,n in r['counts']: L.append('count %d %d %d %d %d'%(ac,ar,bc,br,n))
    open(path,'w').write('\n'.join(L)+'\n')

if __name__=='__main__':
    cases=make_cases()
    write_text(cases,'ReferenceCases.txt')
    nf=sum(len(r['fires']) for c in cases for r in c['rounds'])
    rep=sum(1 for c in cases for r in c['rounds'] if len({(x[2],x[3]) for x in r['fires']})<len(r['fires']))
    print(len(cases),'个布局，',sum(len(c['rounds']) for c in cases),'轮，',nf,'次闪光；其中',rep,'轮出现再次闪光（回荡）')
    print('已写出 ReferenceCases.txt：复制到 Assets/Lingguang/Tests/EditMode/ 后运行 C# 的 ReferenceConsistencyTests')
