"""Original forum diagrams; no historical photographs or third-party artwork.
Usage: python generate.py <Inter-Regular.ttf> <output directory>
"""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont

font_path, out = Path(sys.argv[1]), Path(sys.argv[2])
out.mkdir(parents=True, exist_ok=True)
S=2
def canvas():
    im=Image.new('RGB',(920*S,720*S),'#10232c')
    return im,ImageDraw.Draw(im)
def box(d,xy,fill,r=18,outline=None):
    d.rounded_rectangle(tuple(int(v*S) for v in xy),r*S,fill,outline,width=2*S)
def line(d,pts,color='#486976',w=4):
    d.line([(int(x*S),int(y*S)) for x,y in pts],fill=color,width=w*S,joint='curve')
def circle(d,x,y,r,color):
    d.ellipse(((x-r)*S,(y-r)*S,(x+r)*S,(y+r)*S),fill=color)
def label(d,x,y,t,size=22,color='#dcebed'):
    d.text((x*S,y*S),t,font=ImageFont.truetype(str(font_path),size*S),fill=color)
def arrow(d,x,y,color='#5fe0bb'):
    d.polygon([(x*S,y*S),((x-13)*S,(y-9)*S),((x-13)*S,(y+9)*S)],fill=color)
def save(im,name):
    im.resize((920,720),Image.Resampling.LANCZOS).save(out/(name+'.png'))

im,d=canvas()
label(d,65,55,'LEARNING FROM A CROWD',32)
label(d,65,105,'Different voices. Check the input first.',19,'#91aeb9')
for y,c in [(220,'#65d6bb'),(345,'#edaa6f'),(470,'#76b8e9')]:
    box(d,(75,y,280,y+86),'#243e4b')
    d.polygon([(105*S,(y+80)*S),(105*S,(y+108)*S),(140*S,(y+80)*S)],fill='#243e4b')
    for i in range(3): circle(d,125+50*i,y+43,9,c)
    line(d,[(295,y+43),(375,390),(440,390)])
box(d,(405,285,530,480),'#254e52',24, '#5fe0bb')
for y in range(320,451,32): line(d,[(430,y),(505,y)],'#5fe0bb',6)
line(d,[(530,390),(610,390)],'#5fe0bb',5);arrow(d,610,390)
for a in range(3):
    for b in range(3):
        for j in range(3):
            if a<2:line(d,[(650+a*80,300+b*90),(730+a*80,300+j*90)],'#375d69',2)
for a in range(3):
    for b in range(3):circle(d,650+a*80,300+b*90,15,'#8ce0cd' if a==2 else '#85adc4')
label(d,90,610,'INPUT',19,'#91aeb9');label(d,410,610,'REVIEW',19,'#91aeb9');label(d,660,610,'LEARN',19,'#91aeb9')
save(im,'era_lz2_t03_discussion')

im,d=canvas()
label(d,65,55,'A SHORTCUT THROUGH DEPTH',32)
label(d,65,105,'A residual path carries information forward.',19,'#91aeb9')
for y,col in [(285,'#7695a6'),(490,'#5fe0bb')]:
    line(d,[(85,y),(830,y)],col,5);arrow(d,840,y,col)
    for i in range(4):
        x=180+i*160
        box(d,(x-35,y-50,x+35,y+50),'#29434f',12,col)
        for off in [-22,0,22]:circle(d,x,y+off,5,col)
line(d,[(125,490),(125,380),(780,380),(780,490)],'#f0c783',6)
circle(d,780,490,22,'#f0c783');line(d,[(770,490),(790,490)],'#142731',3);line(d,[(780,480),(780,500)],'#142731',3)
label(d,65,190,'LAYER AFTER LAYER',19,'#91aeb9')
label(d,300,340,'RESIDUAL CONNECTION',18,'#f0c783')
label(d,65,610,'MORE LAYERS NEED A CLEAR PATH.',22,'#a7c2c9')
save(im,'era_lz2_t04_discussion')

im,d=canvas()
label(d,65,55,'LOOKING FOR MORE EXAMPLES',30)
label(d,65,105,'Pictures, categories, and room to store them.',19,'#91aeb9')
for row in range(2):
    for col in range(3):
        x,y=80+col*170,225+row*175
        box(d,(x,y,x+140,y+140),'#24424d',14)
        if col==0:
            circle(d,x+70,y+75,32,'#e6bc88')
            d.polygon([((x+40)*S,(y+60)*S),((x+38)*S,(y+30)*S),((x+63)*S,(y+50)*S)],fill='#e6bc88')
            d.polygon([((x+100)*S,(y+60)*S),((x+102)*S,(y+30)*S),((x+77)*S,(y+50)*S)],fill='#e6bc88')
            for off in [-12,12]:circle(d,x+70+off,y+70,4,'#19303a')
        elif col==1:
            d.ellipse(((x+35)*S,(y+30)*S,(x+105)*S,(y+105)*S),fill='#72c6a3')
            line(d,[(x+55,y+110),(x+82,y+52)],'#d4ecd9',4)
        else:
            box(d,(x+25,y+62,x+117,y+92),'#73b4d5',7)
            d.polygon([((x+42)*S,(y+63)*S),((x+55)*S,(y+40)*S),((x+88)*S,(y+40)*S),((x+104)*S,(y+63)*S)],fill='#73b4d5')
            for off in [45,98]:circle(d,x+off,y+93,11,'#10232c')
box(d,(665,255,830,520),'#304a56',20,'#8baab7')
for y in [295,350,405]:box(d,(690,y,805,y+34),'#192e38',5)
circle(d,747,475,10,'#5fe0bb')
line(d,[(575,370),(650,370)],'#5fe0bb',5);arrow(d,650,370)
label(d,95,610,'VARIED SAMPLES',20,'#91aeb9');label(d,650,610,'LOCAL STORAGE',20,'#91aeb9')
save(im,'thread_ask_imagenet')

im,d=canvas()
label(d,65,55,'SENTENCES THAT MATCH',32)
label(d,65,105,'Parallel text keeps each translation paired.',19,'#91aeb9')
for x,t in [(90,'SOURCE'),(570,'TRANSLATION')]:
    box(d,(x,200,x+260,560),'#233f4b',20)
    label(d,x+25,225,t,23)
    for i,width in enumerate([190,150,175,130]):
        line(d,[(x+30,315+i*58),(x+30+width,315+i*58)],'#a6cad4',7)
for i in range(4):
    y=315+i*58
    line(d,[(370,y),(550,y)],'#5fe0bb',4)
    circle(d,460,y,10,'#5fe0bb');arrow(d,550,y)
label(d,130,610,'ONE LINE. ONE PAIR. LESS NOISE.',25,'#a7c2c9')
save(im,'thread_ask_translate')
