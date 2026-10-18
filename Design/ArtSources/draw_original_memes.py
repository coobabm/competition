from PIL import Image,ImageDraw
from pathlib import Path
out=Path('.coordination/integration-20261018/period-memes/assets');S=3
for kind in ['couch','power']:
 im=Image.new('RGB',(384*S,256*S),'#f0f4fa');d=ImageDraw.Draw(im)
 def box(xy,fill,outline='#263347',width=3,r=0):
  coords=tuple(round(x*S) for x in xy)
  if r:d.rounded_rectangle(coords,r*S,fill,outline,width*S)
  else:d.rectangle(coords,fill,outline,width*S)
 def oval(xy,fill,outline='#263347',width=3):d.ellipse(tuple(round(x*S) for x in xy),fill,outline,width*S)
 def line(xy,fill='#263347',width=4):d.line(tuple(round(x*S) for x in xy),fill,width*S)
 if kind=='couch':
  box((20,180,364,230),'#d9e2ef',None,r=18)
  box((55,95,330,192),'#7894b0',r=20);box((47,135,338,210),'#9bb4cb',r=18)
  box((39,126,72,212),'#7395b5',r=12);box((316,126,350,212),'#7395b5',r=12)
  box((67,208,85,225),'#53667e',r=4);box((302,208,320,225),'#53667e',r=4)
  # Generic sleepy mascot, original; no celebrity likeness.
  oval((85,114,236,177),'#7db4aa');line((172,158,251,182,300,177),'#425a79',18)
  line((170,164,242,194,286,198),'#526d8d',18)
  oval((72,78,150,150),'#ffd55a');line((88,109,102,109));line((119,109,133,109))
  oval((103,123,118,138),'#6c482e',None);line((136,139,181,150,201,137),'#ffd55a',13)
  box((199,131,222,146),'#33425b',r=3)
  # Sleep symbols drawn as strokes, not font/translation.
  line((158,62,175,62,158,77,175,77),'#6a7fa4',3);line((188,40,212,40,188,60,212,60),'#6a7fa4',3)
 else:
  oval((84,25,300,241),'#ffe9a8',None)
  for xy in [(75,68,57,56),(309,68,327,56),(59,130,35,130),(325,130,349,130),(90,207,73,224),(294,207,311,224)]:line(xy,'#e9a43d',5)
  # Original mascot celebrating its effort; no sport/celebrity likeness.
  box((147,148,237,228),'#6ca69f',r=22)
  line((153,169,116,154,104,110),'#263347',25);line((153,169,116,154,104,110),'#ffd55a',19)
  line((231,169,268,154,280,110),'#263347',25);line((231,169,268,154,280,110),'#ffd55a',19)
  oval((89,92,119,119),'#ffd55a');oval((265,92,295,119),'#ffd55a')
  oval((136,56,248,163),'#ffd55a');line((157,98,169,91,181,98));line((203,98,215,91,227,98))
  d.arc((159*S,101*S,225*S,146*S),0,180,fill='#263347',width=4*S)
  oval((234,87,245,105),'#7bbfd9',None)
 im.resize((384,256),Image.Resampling.LANCZOS).save(out/f'teach_meme_{kind}_original.png')
