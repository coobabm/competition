require 'json';require 'pathname';require 'fileutils';require 'open3';require 'digest'
commit='65ccf0752df19c9cf97da4884e41bfd4e8c8ec69'
base='Assets/StreamingAssets/HongmengOS-Aero2010/Oxygen'
FileUtils.mkdir_p(base+'/licenses');FileUtils.mkdir_p(base+'/source')
tree=JSON.parse(File.read('/tmp/oxygen-tree.json'))['tree'].to_h{|x|[x['path'],x]}
def get(url)
 out,err,status=Open3.capture3('curl','--fail','-L','--max-time','40','-sS',url);raise "Download failed #{url}: #{err}" unless status.success?;out.b
end
raw="https://raw.githubusercontent.com/KDE/oxygen-icons/#{commit}/"
fetch=lambda{|path| get(raw+path.split('/').map{|s|s.gsub('+','%2B')}.join('/'))}
resolve=lambda do |path|
 8.times do
  entry=tree.fetch(path);return [path,fetch.call(path)] unless entry['mode']=='120000'
  dest=fetch.call(path).strip;path=Pathname.new(File.join(File.dirname(path),dest)).cleanpath.to_s
  raise 'Out of allowed tree' unless path.start_with?('128x128/','scalable/')
 end
 raise 'Symlink chain too long'
end
mapping={
 'Calculator'=>'128x128/apps/accessories-calculator.png','Commander'=>'128x128/apps/utilities-terminal.png',
 'Game Hub'=>'128x128/categories/applications-games.png','Mail'=>'128x128/apps/internet-mail.png',
 'Messaging'=>'128x128/apps/kopete.png','Music Player'=>'128x128/mimetypes/audio-x-generic.png',
 'Notepad'=>'128x128/apps/accessories-text-editor.png','Photo Gallery'=>'128x128/mimetypes/image-x-generic.png',
 'Reminder'=>'128x128/apps/office-calendar.png','Settings'=>'128x128/categories/preferences-system.png',
 'Video Player'=>'128x128/mimetypes/video-x-generic.png','Web Browser'=>'128x128/apps/internet-web-browser.png',
 'Widget Library'=>'128x128/categories/applications-other.png'}
manifest=[]
mapping.each do |key,path|
 resolved,bytes=resolve.call(path);raise "Not PNG #{path}" unless bytes.start_with?("\x89PNG\r\n\x1a\n".b)
 File.binwrite(base+'/'+key+'.png',bytes)
 # Keep preferred editable forms for both alias artwork and resolved raster name.
 sources=[]
 [path,resolved].uniq.each do |pn|
  stem=pn.sub('128x128/','scalable/').sub(/\.png$/,'')
  ['.svg','.svgz'].each do |ext|
   sp=stem+ext;next unless tree.key?(sp)
   sr,data=resolve.call(sp);dest=base+'/source/'+sr;FileUtils.mkdir_p(File.dirname(dest));File.binwrite(dest,data);sources<<sr
  end
 end
 manifest<<{app:key,file:key+'.png',upstream_path:path,resolved_png:resolved,upstream_url:raw+resolved,sha256:Digest::SHA256.hexdigest(bytes),source_files:sources.uniq,license:'LGPL-3.0-or-later with Oxygen artwork clarification',modified:false}
 puts "#{key}: #{bytes.bytesize} bytes, #{sources.uniq.size} SVG sources"
end
%w[COPYING COPYING.LIB AUTHORS REUSE.toml LICENSES/LGPL-3.0-or-later.txt].each{|p|File.binwrite(base+'/licenses/'+File.basename(p),fetch.call(p))}
File.binwrite(base+'/licenses/GPL-3.0.txt',get('https://www.gnu.org/licenses/gpl-3.0.txt'))
File.write(base+'/manifest.json',JSON.pretty_generate({project:'KDE Oxygen Icon Theme',repository:'https://github.com/KDE/oxygen-icons',commit:commit,assets:manifest}))
File.write(base+'/NOTICE.txt',<<~NOTE)
Oxygen Icon Theme — selected unmodified icons
Copyright (C) 2007 Oxygen Team and contributors, see licenses/AUTHORS and licenses/COPYING.
License: LGPL-3.0-or-later, with the artwork clarification in licenses/COPYING.
This is not Microsoft Windows artwork and is not affiliated with Microsoft.

These PNG files are loaded at runtime as separate, replaceable files; they are not packed into a Unity sprite atlas.
Keep this NOTICE, licenses/, source/, and manifest.json with the distributed desktop.
Preferred editable SVG sources and complete license texts are included locally; no source-code offer or external download is necessary.
To replace an icon, preserve the corresponding application filename and supply a valid PNG.
The parent application must not prohibit modification of these LGPL library files or reverse engineering for debugging such modifications.
No artwork pixel changes were made; files have only been given application-specific runtime filenames. Exact upstream paths and SHA-256 values are in manifest.json.
NOTE
