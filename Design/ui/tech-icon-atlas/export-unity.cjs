const fs=require('fs'),path=require('path'),crypto=require('crypto');
const {Resvg}=require('@resvg/resvg-js');
const {svgPathProperties}=require('svg-path-properties');
const root=path.resolve(__dirname,'../../..'), design=path.join(root,'Design/ui/tech-icon-atlas');
const out=path.join(root,'Assets/LingGuangV05/Resources/LingGuangV05/Icons/tech');
const manifest=JSON.parse(fs.readFileSync(path.join(design,'manifest.json'),'utf8'));
const meta=fs.readFileSync(path.join(out,'perceptron.png.meta'),'utf8');
let made=0;const seen=new Set();
for(const n of manifest.nodes){if(!n.base.endsWith('.svg')||seen.has(n.entry))continue;seen.add(n.entry);const name=n.entry.replace(/[./ ]/g,'_'),dest=path.join(out,name+'.png');if(fs.existsSync(dest)){const expected=new Resvg(fs.readFileSync(path.join(design,n.base)),{fitTo:{mode:'width',value:256}}).render().asPng();if(!fs.readFileSync(dest).equals(expected))throw Error('Existing icon differs: '+dest);continue;}fs.writeFileSync(dest,new Resvg(fs.readFileSync(path.join(design,n.base)),{fitTo:{mode:'width',value:256}}).render().asPng());fs.writeFileSync(dest+'.meta',meta.replace(/^guid:.*$/m,'guid: '+crypto.createHash('md5').update('game-tech-atlas:'+n.entry).digest('hex')).replace('enableMipMap: 1','enableMipMap: 0'));made++;}
const nodes=manifest.nodes.map(n=>{const pts=[];for(const part of n.phasePath.match(/M[^M]+/g)){const p=new svgPathProperties(part),len=p.getTotalLength(),steps=Math.max(2,Math.ceil(len/3));for(let i=0;i<=steps;i++){let v=p.getPointAtLength(len*i/steps);pts.push({x:+v.x.toFixed(3),y:+v.y.toFixed(3),start:i===0});}}return {id:n.id,points:pts,activeMs:n.activeMs,restMs:n.restMs};});
fs.writeFileSync(path.join(out,'signal_paths.json'),JSON.stringify({nodes}));
console.log(JSON.stringify({rasterized:made,signals:nodes.length,entries:new Set(manifest.nodes.map(n=>n.entry)).size}));

