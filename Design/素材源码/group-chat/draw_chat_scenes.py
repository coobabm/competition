"""Original project scene illustrations. No external image inputs or model calls.
All geometry and compositions authored for this task; reproducible with Pillow.
"""
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from pathlib import Path
import random, math, json, hashlib

ROOT=Path(__file__).resolve().parent
if ROOT.name=='source': ROOT=ROOT.parent
OUT=ROOT/'candidates'; OUT.mkdir(exist_ok=True)
S=2
rng=random.Random(20261019)

class Art:
    def __init__(self,w=800,h=600,top='#b6d2cf',bottom='#f2deba'):
        self.w,self.h=w,h
        self.im=Image.new('RGB',(w*S,h*S)); self.d=ImageDraw.Draw(self.im)
        a=tuple(bytes.fromhex(top.lstrip('#'))); b=tuple(bytes.fromhex(bottom.lstrip('#')))
        for y in range(h*S):
            t=y/(h*S-1); c=tuple(int(x*(1-t)+z*t) for x,z in zip(a,b)); self.d.line((0,y,w*S,y),fill=c)
    def p(self,ps,c): self.d.polygon([(int(x*S),int(y*S)) for x,y in ps],fill=c)
    def l(self,ps,c,width=2): self.d.line([(int(x*S),int(y*S)) for x,y in ps],fill=c,width=max(1,int(width*S)),joint='curve')
    def e(self,box,c,outline=None,width=1): self.d.ellipse(tuple(int(v*S) for v in box),fill=c,outline=outline,width=int(width*S))
    def r(self,box,c,rad=0,outline=None,width=1): self.d.rounded_rectangle(tuple(int(v*S) for v in box),radius=rad*S,fill=c,outline=outline,width=width*S)
    def glow(self,x,y,rx,ry,c,alpha=90):
        layer=Image.new('RGBA',self.im.size); d=ImageDraw.Draw(layer)
        d.ellipse(((x-rx)*S,(y-ry)*S,(x+rx)*S,(y+ry)*S),fill=(*tuple(bytes.fromhex(c.lstrip('#'))),alpha))
        layer=layer.filter(ImageFilter.GaussianBlur(35*S)); self.im=Image.alpha_composite(self.im.convert('RGBA'),layer).convert('RGB'); self.d=ImageDraw.Draw(self.im)
    def save(self,name):
        im=self.im.resize((self.w,self.h),Image.Resampling.LANCZOS)
        # Fine paper grain, deliberately illustration rather than fake photography.
        pix=im.load(); noise=random.Random(name)
        for y in range(self.h):
            for x in range(self.w):
                n=noise.randrange(-3,4); pix[x,y]=tuple(max(0,min(255,v+n)) for v in pix[x,y])
        im.save(OUT/(name+'.jpg'),quality=91,optimize=True,subsampling=0)

def tree(a,x,y,s=1):
    a.p([(x-5*s,y+90*s),(x+7*s,y+90*s),(x+4*s,y),(x-3*s,y)],'#76674c')
    for dx,dy,r in [(-25,0,39),(18,-27,44),(40,8,36),(-7,25,42)]: a.e((x+(dx-r)*s,y+(dy-r)*s,x+(dx+r)*s,y+(dy+r)*s),'#5e8b73')

def back(a,x,y,s=1,coat='#31516a',hair='#302e35',arms=False):
    # Back-of-head only: no eyes, noses, mouths or identifiable likeness.
    a.e((x-17*s,y-39*s,x+17*s,y+2*s),hair)
    a.p([(x-26*s,y),(x+25*s,y),(x+39*s,y+83*s),(x-38*s,y+83*s)],coat)
    a.l([(x-9*s,y+80*s),(x-12*s,y+122*s)],'#273344',12*s)
    a.l([(x+11*s,y+80*s),(x+16*s,y+122*s)],'#273344',12*s)
    if arms:
        a.l([(x-20*s,y+8*s),(x-43*s,y-17*s),(x-48*s,y-50*s)],coat,15*s)
        a.l([(x+20*s,y+8*s),(x+43*s,y-20*s),(x+48*s,y-53*s)],coat,15*s)
        a.e((x-54*s,y-62*s,x-42*s,y-45*s),'#dba98a')
        a.e((x+42*s,y-65*s,x+54*s,y-48*s),'#dba98a')
    a.l([(x-12*s,y+8*s),(x-8*s,y+70*s)],'#ffffff',1*s) if coat=='#d7d1ba' else None

def monitor(a,x,y,w=150,h=100,mode='code'):
    a.r((x,y,x+w,y+h),'#101e2b',5)
    a.r((x+6,y+6,x+w-6,y+h-9),'#263c4d',2)
    if mode=='code':
        for j in range(9):
            yy=y+15+j*(h-28)/9
            for k in range(3):
                xx=x+15+k*w*.24+rng.randrange(0,8)
                a.l([(xx,yy),(min(x+w-15,xx+rng.randrange(12,30)),yy)],['#62aaad','#c0af7a','#899ac0'][k],2)
    elif mode=='map':
        a.p([(x+8,y+9),(x+w*.75,y+8),(x+w*.55,y+h*.4),(x+w*.8,y+h*.7),(x+w*.35,y+h-12),(x+8,y+h-12)],'#779a83')
        a.p([(x+45,y+15),(x+w*.63,y+22),(x+w*.45,y+h*.65),(x+22,y+h-18)],'#c2b887')
        for k in range(13):
            xx=x+rng.randrange(16,int(w)-16); yy=y+rng.randrange(15,int(h)-18)
            a.r((xx,yy,xx+3,yy+3),'#d5b66d')
        for row in range(5):
            for col in range(9):
                xx=x+18+col*(w-36)/9; yy=y+18+row*(h-40)/5
                a.l([(xx-6,yy),(xx-3,yy-5),(xx+3,yy-5),(xx+6,yy),(xx+3,yy+5),(xx-3,yy+5),(xx-6,yy)],'#94a99b',.5)
        a.r((x+9,y+h-23,x+w-9,y+h-11),'#314852',1)
        for k in range(6): a.r((x+16+k*12,y+h-20,x+23+k*12,y+h-14),'#9dac9b',1)
    a.r((x+w*.45,y+h,x+w*.55,y+h+19),'#182934')
    a.p([(x+w*.3,y+h+20),(x+w*.69,y+h+20),(x+w*.76,y+h+24),(x+w*.24,y+h+24)],'#16232f')

def keyboard(a,x,y,w=180):
    a.p([(x+10,y),(x+w-8,y),(x+w,y+34),(x,y+34)],'#273442')
    for row in range(4):
        for col in range(12): a.r((x+13+col*(w-24)/12-row,y+4+row*7,x+20+col*(w-24)/12-row,y+8+row*7),'#607480',1)

def grad():
    a=Art(600,800,'#9cc6cd','#e4e8ce')
    a.glow(440,90,150,100,'#fff6d4',110)
    a.e((450,55,510,115),'#fff1bf')
    a.p([(0,345),(600,320),(600,800),(0,800)],'#a7b981')
    a.r((74,237,529,392),'#d8d0b2'); a.p([(48,239),(301,179),(554,237)],'#9f7e6a')
    a.r((251,244,350,392),'#e9dfc6')
    for x in range(91,525,44):
        for y in [262,315]: a.r((x,y,x+24,y+34),'#718c8e'); a.l([(x+12,y),(x+12,y+34)],'#b9c2ac',2)
    tree(a,30,290,.85); tree(a,573,290,.85)
    a.p([(0,700),(600,530),(600,800),(0,800)],'#c1c692')
    for i,(x,y) in enumerate([(82,505),(180,482),(282,503),(386,480),(490,511)]):
        a.e((x-39,y+138,x+67,y+157),'#9da478')
        back(a,x,y,1.15,'#283744',['#362e30','#463531','#282d35'][i%3],True)
        a.p([(x-22,y+6),(x,y+31),(x+23,y+5),(x+14,y+35),(x,y+47),(x-15,y+33)],'#c06460')
        a.p([(x-25,y-30),(x-18,y-4),(x+17,y-4),(x+22,y-35)],'#342f35')
    for x,y,tilt in [(75,327,4),(185,350,-8),(288,285,6),(406,322,-9),(504,374,4)]:
        a.p([(x-27,y),(x,y-12),(x+30,y+tilt),(x+1,y+18)],'#263640'); a.r((x-9,y+5,x+12,y+16),'#29333b'); a.l([(x+23,y+6),(x+29,y+38)],'#d4b66b',2)
    for _ in range(75):
        x=rng.randrange(600); y=rng.randrange(720,800); a.l([(x,y),(x+3,y-8)],'#a3b078')
    a.save('chat_lili_grad')

def gulangyu():
    a=Art(top='#b2cdcb',bottom='#d8b998')
    a.p([(0,100),(310,218),(310,395),(0,514)],'#bd7e62'); a.p([(800,80),(479,223),(479,395),(800,500)],'#c88469')
    a.p([(310,395),(479,395),(800,600),(0,600)],'#baac96')
    for side in [0,1]:
        for i in range(5):
            x=20+i*58 if side==0 else 521+i*58
            top=165+i*14 if side==0 else 224-i*14
            a.r((x,top,x+39,top+118),'#775c52',19); a.r((x+7,top+8,x+32,top+113),'#405f62',13)
            a.r((x-4,top+125,x+45,top+223),'#e6c8a3'); a.r((x+4,top+135,x+36,top+222),'#806d59',15)
    for y in range(120,365,18): a.l([(0,y),(285,y+80)],'#cb9577',1)
    for i in range(9): back(a,480+i*27,335+i*13,.42+i*.018,['#4a6364','#b3977e','#756e76'][i%3])
    # Hand and bitten pastry are foreground focal point.
    a.p([(175,600),(187,495),(218,422),(243,402),(287,414),(347,448),(380,507),(417,600)],'#d9a17b')
    a.p([(215,600),(230,487),(265,454),(317,474),(344,529),(347,600)],'#e8b28b')
    pastry_background=a.im.copy()
    a.e((217,301,399,475),'#a85f37'); a.e((222,298,397,459),'#ddaa63'); a.e((236,311,384,449),'#edc57b')
    for i in range(36):
        x=rng.randrange(245,378); y=rng.randrange(323,435)
        if ((x-310)/70)**2+((y-378)/62)**2<1: a.l([(x,y),(x+4,y-2)],'#c58d48',2)
    # Bite contour exposes the green-bean filling on the upper right.
    bite=Image.new('L',a.im.size); bd=ImageDraw.Draw(bite)
    bd.polygon([(x*S,y*S) for x,y in [(347,285),(414,285),(414,362),(377,358),(367,341),(347,336)]],fill=255)
    a.im.paste(pastry_background,(0,0),bite); a.d=ImageDraw.Draw(a.im)
    a.p([(343,310),(361,313),(365,327),(379,330),(386,350),(373,354),(357,338),(342,335)],'#b3ae6e')
    a.l([(343,310),(341,330),(357,339),(372,356),(385,353)],'#f3dba3',5)
    a.r((209,426,260,512),'#efbc95',22); a.l([(224,452),(243,460)],'#c88d6b',2)
    a.save('chat_lili_gulangyu')

def westlake():
    a=Art(top='#c4d7d1',bottom='#d5b9a5')
    a.p([(0,231),(100,178),(201,213),(318,166),(445,225),(577,191),(800,222),(800,350),(0,350)],'#8daba4')
    a.r((0,284,800,440),'#93bfc0')
    for y in range(308,430,18): a.l([(0,y),(250,y)],'#b5d1c9',2)
    a.p([(137,420),(531,258),(800,307),(800,600),(0,600)],'#b4a28c')
    a.l([(116,424),(531,260),(800,309)],'#e8ddc2',11)
    for i in range(11):
        x=140+i*36; y=418-i*14; a.l([(x,y),(x,y+43)],'#d4cbb4',7)
    for row in range(5):
        for col in range(5+row*2):
            x=495-row*138+col*62; y=310+row*68+rng.randrange(-8,9); s=.35+row*.2
            back(a,x,y,s,['#6d7c89','#bd9381','#c7bba2','#566c73','#9c776c'][(row+col)%5])
    for x,y,s in [(35,550,1.35),(124,578,1.5),(225,596,1.6)]:
        back(a,x,y,s,['#677c87','#b39180','#c7baa2'][int(x)%3])
    for x,y,r,c in [(610,321,53,'#bf7664'),(384,381,66,'#e2bb72'),(710,456,85,'#8eaaa8'),(112,489,86,'#aaa1b3')]:
        a.l([(x,y),(x,y+90)],'#776d64',3)
        a.p([(x-r,y),(x-r*.65,y-r*.45),(x,y-r*.65),(x+r*.7,y-r*.4),(x+r,y),(x+r*.45,y-4),(x,y+8),(x-r*.5,y-3)],c)
        a.l([(x-r,y),(x,y-r*.65),(x+r,y)],'#ecdbb3',1)
    a.save('chat_lili_westlake')

def tv():
    a=Art(top='#182938',bottom='#263845')
    a.r((0,391,800,600),'#182935'); a.r((48,92,568,368),'#14212c',8)
    a.glow(310,239,237,140,'#74a494',95)
    a.r((70,111,547,343),'#365e57',2)
    a.p([(88,324),(162,143),(471,143),(530,324)],'#54846a')
    a.p([(160,143),(235,143),(186,324),(88,324)],'#5c8d70'); a.p([(314,143),(390,143),(429,324),(312,324)],'#5c8d70')
    a.l([(100,312),(167,154),(467,154),(518,312),(100,312)],'#a6bfa3',2); a.l([(316,153),(313,313)],'#a6bfa3',2)
    a.e((279,215,350,252),None,'#a6bfa3',2)
    for i in range(14):
        x=rng.randrange(170,465); y=rng.randrange(176,300); a.e((x,y,x+4,y+6),['#eadfc4','#d4a48d'][i%2])
    a.r((279,369,337,395),'#101b25'); a.r((208,395,411,404),'#111e29',3)
    a.e((648,75,744,171),'#c4c9bd'); a.e((655,82,737,164),'#263c49')
    for j in range(12):
        t=j*math.pi/6; a.l([(696+34*math.sin(t),123-34*math.cos(t)),(696+38*math.sin(t),123-38*math.cos(t))],'#c7ceba',2)
    a.l([(696,123),(721,123)],'#e3dcc0',4); a.l([(696,123),(696,91)],'#e3dcc0',3)
    a.p([(90,454),(626,421),(767,573),(24,600)],'#715c4d'); a.l([(90,454),(626,421)],'#9e8065',3)
    a.r((224,416,290,513),'#ab8d55',9); a.e((224,410,290,431),'#c4c1ac'); a.e((235,415,279,425),'#6d7165'); a.r((230,446,284,483),'#bdab77',2)
    a.p([(367,466),(511,450),(561,524),(397,546)],'#a68b64'); a.p([(373,470),(502,459),(522,482),(397,504)],'#d2b687')
    for x,y in [(414,481),(435,478),(449,487),(473,479),(466,503),(430,512)]: a.e((x,y,x+19,y+10),'#ddbd81', '#b29161')
    a.save('chat_xg_tv')

def desk():
    a=Art(top='#202c3e',bottom='#293b49')
    a.r((30,39,775,236),'#121f30')
    for x in range(42,775,24):
        h=rng.randrange(20,80); a.r((x,236-h,x+20,236),'#26374b')
        for yy in range(240-h,226,13):
            if rng.random()<.45: a.r((x+5,yy,x+9,yy+3),'#b8a477')
    for x in [30,273,516,775]: a.r((x,38,x+7,240),'#43515d')
    a.l([(29,241),(779,241)],'#657078',5)
    for row in range(2):
        y=290+row*72
        for col in range(4):
            x=25+col*203; a.p([(x,y),(x+163,y),(x+188,y+37),(x-8,y+37)],'#45545c'); a.r((x+43,y-51,x+115,y-7),'#1d2d3b',3)
            a.r((x+63,y+20,x+132,y+87),'#283c49',13)
    a.glow(460,400,240,140,'#e3c48b',105)
    a.p([(241,436),(674,425),(785,540),(157,560)],'#a98b68'); a.p([(157,560),(785,540),(785,558),(157,580)],'#5c534a')
    monitor(a,278,312,165,112); monitor(a,461,306,164,112)
    a.l([(671,444),(696,329),(650,295)],'#d6ba87',8); a.p([(620,294),(669,282),(689,312),(627,323)],'#c9ae78')
    keyboard(a,354,463,199)
    a.e((596,465,622,485),'#d7c6a3'); a.r((592,438,620,469),'#d2c6aa',3); a.e((592,435,620,445),'#dcd4b8'); a.e((596,438,616,443),'#564238')
    a.r((335,523,526,644),'#253843',30); a.l([(359,537),(507,537)],'#5b7075',3)
    a.save('chat_dw_desk')

def cafe():
    a=Art(top='#1b293e',bottom='#29425b')
    a.p([(0,0),(800,0),(654,175),(167,175)],'#25344a')
    for y in [35,92,143]: a.l([(0,y),(800,y)],'#3e4d61',3)
    a.glow(406,199,245,130,'#6e98ce',90)
    a.r((219,92,609,293),'#132739',4); a.r((230,103,598,281),'#557a82')
    a.p([(233,274),(316,142),(424,234),(503,121),(594,217),(594,277)],'#6f967e'); a.p([(330,277),(369,233),(431,229),(485,277)],'#aeaf84')
    for i,(x,y) in enumerate([(311,195),(459,210),(512,174),(390,248)]):
        a.e((x-7,y+9,x+17,y+17),'#405d68'); a.p([(x+5,y-4),(x+11,y+12),(x-1,y+12)],'#d6bb84' if i%2 else '#c69d91')
        a.l([(x-3,y-10),(x+13,y-10)],'#8fd2b0' if i%2 else '#dd9b95',3)
    a.r((235,254,288,277),'#344955',1)
    for i in range(5): a.r((431+i*27,261,450+i*27,275),'#b6b38e',1)
    for side in [0,1]:
        for i in range(3):
            x=10+i*27 if side==0 else 676-i*31; y=288+i*66
            a.p([(x-20,y+84),(x+123,y+74),(x+153,y+119),(x-15,y+139)],'#263341')
            monitor(a,x,y,104,65,'map')
    for i,x in enumerate([153,255,358,466,573,687]): back(a,x,384+(i%2)*30,1.05,['#526177','#756e78','#4a687c'][i%3],arms=True)
    for x,y in [(50,530),(219,537),(442,552),(749,539)]: back(a,x,y,1.45,'#263c51')
    a.l([(31,18),(31,259)],'#6d9cbe',4); a.l([(770,18),(770,259)],'#6d9cbe',4)
    a.save('chat_dw_cafe')

def iphone():
    a=Art(top='#273745',bottom='#685c5a')
    a.r((387,72,800,412),'#172b36'); a.r((405,91,774,366),'#a8ae9e')
    a.r((432,130,752,340),'#9a9e94'); a.r((421,90,763,115),'#e2d5b5')
    for x in [466,580,695]: a.l([(x,119),(x,365)],'#d4c8b2',6)
    a.glow(634,190,150,108,'#e9dbb1',60)
    for i in range(9): back(a,451+i*39,326+i*20,.48+i*.035,['#8e8d87','#4d626b','#b09b8b'][i%3])
    a.p([(115,600),(183,458),(267,382),(341,384),(415,438),(451,503),(458,600)],'#c88f77')
    a.p([(181,600),(220,461),(279,431),(356,447),(402,514),(408,600)],'#dfa58a')
    # Glossy generic 2016 single-camera phone, no logos or trade dress text.
    phone=Image.new('RGBA',(230*S,370*S)); d=ImageDraw.Draw(phone)
    d.rounded_rectangle((5*S,5*S,225*S,365*S),radius=27*S,fill='#101b25',outline='#70818a',width=3*S)
    d.rounded_rectangle((15*S,15*S,215*S,355*S),radius=22*S,fill='#19232c')
    d.polygon([(29*S,19*S),(93*S,18*S),(204*S,345*S),(155*S,350*S)],fill='#293741')
    d.line([(24*S,41*S),(204*S,41*S)],fill='#62717a',width=S)
    d.ellipse((28*S,27*S,52*S,51*S),fill='#080f17',outline='#677b88',width=2*S); d.ellipse((35*S,34*S,45*S,44*S),fill='#294151')
    d.ellipse((64*S,33*S,73*S,42*S),fill='#c9bc9f')
    phone=phone.rotate(-14,resample=Image.Resampling.BICUBIC,expand=True)
    a.im.paste(phone,(165*S,163*S),phone); a.d=ImageDraw.Draw(a.im)
    a.r((176,415,226,532),'#e7ae90',24); a.l([(189,442),(214,450)],'#bd826a',2)
    a.save('chat_dw_iphone')

def dawn():
    a=Art(top='#829cab',bottom='#374958')
    a.r((32,25,403,328),'#426177'); a.r((43,36,392,316),'#b6d0d6')
    a.e((287,123,326,163),'#f0dfbc')
    for x in range(44,390,33):
        y=240+rng.randrange(-40,30); a.r((x,y,x+31,317),'#8da7b5')
        for xx in [x+6,x+17]: a.r((xx,y+11,xx+5,y+18),'#a9c0c8')
    a.r((207,27,217,324),'#617c8b'); a.r((34,192,401,201),'#617c8b')
    a.p([(0,0),(84,0),(67,284),(27,350),(0,351)],'#8397a7'); a.p([(368,0),(433,0),(452,351),(406,335),(390,286)],'#637c90')
    a.glow(205,293,225,173,'#c2dae3',50)
    a.p([(176,422),(736,395),(800,538),(111,600)],'#7b7f79'); a.p([(111,586),(800,527),(800,549),(111,600)],'#4a5557')
    monitor(a,418,252,278,177,'map'); keyboard(a,394,470,236)
    for x,y,c in [(180,474,'#b18e72'),(238,513,'#9b9b87'),(305,453,'#a79370'),(706,468,'#908ba2')]:
        a.p([(x,y),(x+53,y-8),(x+67,y+40),(x+3,y+45)],c); a.l([(x+5,y+5),(x+48,y-1)],'#d0bda1',3); a.p([(x+12,y+13),(x+42,y+9),(x+49,y+31),(x+18,y+35)],'#c5b59a')
    a.r((327,418,367,482),'#b1c5c2',6); a.e((327,413,367,427),'#c7d3ca'); a.e((334,417,360,423),'#52676a')
    a.r((533,560,743,650),'#2c4352',30)
    a.save('chat_xg_dawn')

def box(a,x,y,w,h):
    a.p([(x,y),(x+w,y-12),(x+w+26,y+4),(x+23,y+17)],'#d6b385')
    a.p([(x,y),(x+23,y+17),(x+23,y+h),(x,y+h-11)],'#a17d56')
    a.p([(x+23,y+17),(x+w+26,y+4),(x+w+26,y+h-14),(x+23,y+h)],'#be9768')
    a.p([(x+w*.5,y-6),(x+w*.5+15,y-8),(x+w*.5+40,y+10),(x+w*.5+40,y+h-5),(x+w*.5+25,y+h-4),(x+w*.5+25,y+11)],'#d8bd8d')
    a.p([(x+37,y+32),(x+70,y+29),(x+70,y+54),(x+37,y+57)],'#e1d2ae')
    for k in range(6): a.l([(x+41+k*4,y+39),(x+41+k*4,y+49)],'#988d76',1)

def parcels():
    a=Art(top='#d3c9af',bottom='#b8a58d')
    a.r((118,0,600,534),'#8f8170'); a.r((143,0,578,534),'#4d6869'); a.r((163,22,555,511),'#607a77',2)
    a.r((192,56,526,275),'#6c8580',4); a.r((192,299,526,475),'#59736f',4)
    a.e((493,246,511,264),'#d3b67d'); a.r((493,253,535,263),'#d3b67d',4)
    a.p([(0,532),(800,519),(800,600),(0,600)],'#a99c88')
    for x in range(0,800,120): a.l([(400,519),(x,600)],'#938976',1)
    box(a,125,410,155,134); box(a,321,400,190,150); box(a,532,428,133,115)
    box(a,211,315,158,106); box(a,400,306,146,110); box(a,299,218,167,105)
    a.p([(76,458),(132,447),(156,474),(147,547),(76,551),(58,505)],'#d6d3bf'); a.l([(82,461),(130,455)],'#8f938b',4)
    a.p([(562,342),(633,334),(657,364),(638,438),(569,430),(547,387)],'#838f91'); a.r((583,360,621,393),'#e4dac0',2)
    # Orange cat comfortably sprawled on the uppermost parcel.
    a.e((316,163,465,232),'#b17e50'); a.e((339,167,462,222),'#cb9863')
    a.l([(442,198),(478,206),(482,230),(454,237)],'#b17e50',17)
    a.e((287,171,351,225),'#d1a16e'); a.p([(291,181),(292,151),(314,174)],'#bf8d5d'); a.p([(326,173),(347,153),(347,190)],'#bf8d5d')
    a.p([(296,172),(297,159),(306,173)],'#dbb290'); a.p([(336,173),(342,161),(342,179)],'#dbb290')
    a.l([(299,202),(306,205),(313,202)],'#625244',2); a.l([(325,202),(332,205),(339,202)],'#625244',2)
    a.e((316,209,322,213),'#916753'); a.l([(290,213),(276,211)],'#6e6351',1); a.l([(340,213),(354,209)],'#6e6351',1)
    a.e((335,211,382,233),'#dfb17c'); a.e((389,212,431,232),'#dfb17c')
    for x in [362,390,418]: a.l([(x,172),(x+7,187)],'#a7774f',5)
    a.save('chat_lili_parcels')

def ticket():
    a=Art(top='#422f3f',bottom='#806255')
    a.p([(0,0),(800,0),(531,238),(264,238)],'#3c2939'); a.p([(264,238),(531,238),(800,600),(0,600)],'#a17c66')
    for x in [50,192,603,744]:
        a.r((x,84,x+37,294),'#e0bd8b',2); a.r((x+7,93,x+30,285),'#7b4a50')
    a.r((315,123,490,276),'#352b38'); a.l([(325,127),(481,127)],'#e2bd84',4)
    for x in [283,523]: a.l([(x,13),(x,211)],'#e3bf88',3)
    a.glow(408,218,200,95,'#d2a778',70)
    # Popcorn in a generic striped tub, with no cinema/film branding.
    a.p([(591,338),(730,338),(707,497),(612,497)],'#e1c5a0')
    for x in [611,650,689]: a.p([(x,344),(x+18,344),(x+13,492),(x-1,492)],'#aa6861')
    a.e((582,319,739,361),'#efdaad')
    for _ in range(38):
        x=rng.randrange(592,729); y=rng.randrange(293,345); a.e((x,y,x+17,y+14),rng.choice(['#f2deb4','#e4c993','#f7e6c4']))
    a.p([(98,600),(153,462),(213,401),(265,396),(354,434),(425,504),(443,600)],'#cc977a')
    a.p([(173,600),(217,458),(285,440),(356,490),(392,600)],'#e4b18d')
    # Ticket pair with abstract ink bars: no invented film title or actual ticket data.
    for x,y,angle,c in [(181,319,-12,'#d8c09a'),(215,360,8,'#efd7aa')]:
        im=Image.new('RGBA',(294*S,118*S)); d=ImageDraw.Draw(im)
        d.rounded_rectangle((0,0,292*S,116*S),radius=4*S,fill=c)
        for j in range(8): d.line((229*S,(8+j*14)*S,229*S,(13+j*14)*S),fill='#b39b79',width=S)
        d.rectangle((18*S,17*S,157*S,22*S),fill='#9d866e'); d.rectangle((18*S,36*S,112*S,40*S),fill='#b29c7f')
        d.rectangle((18*S,59*S,183*S,63*S),fill='#b09b7e'); d.rectangle((18*S,81*S,83*S,86*S),fill='#b09b7e')
        for j in range(12): d.rectangle(((241+j*3)*S,24*S,(242+j*3)*S,83*S),fill='#9a866b')
        im=im.rotate(angle,resample=Image.Resampling.BICUBIC,expand=True)
        a.im.paste(im,(x*S,y*S),im); a.d=ImageDraw.Draw(a.im)
    a.r((179,435,228,523),'#e9b694',24)
    a.save('chat_lili_ticket')

if __name__=='__main__':
    for f in [grad,gulangyu,westlake,tv,desk,cafe,iphone,dawn,parcels,ticket]:
        f(); print(f.__name__,flush=True)
