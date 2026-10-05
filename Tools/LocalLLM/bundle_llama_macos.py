#!/usr/bin/env python3
"""Bundle Homebrew's llama-server (and every non-system dylib it needs, plus the ggml backends that are
loaded at run time) into one self-contained folder for Unity StreamingAssets.

Usage: python3 bundle_llama_macos.py <out_dir>
Re-runnable: wipes <out_dir> first. Rewrites install names to @loader_path and ad-hoc signs every file."""
import os, shutil, subprocess, sys, glob

OUT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else "llama")
SERVER = os.path.realpath("/opt/homebrew/bin/llama-server")
BACKENDS = sorted(glob.glob(os.path.realpath("/opt/homebrew/opt/ggml/libexec") + "/*.so"))
SYSTEM = ("/usr/lib/", "/System/")

def deps(path):
    out = subprocess.run(["otool", "-L", path], capture_output=True, text=True, check=True).stdout.splitlines()[1:]
    return [line.strip().split(" (")[0] for line in out]

def rpaths(path):
    out = subprocess.run(["otool", "-l", path], capture_output=True, text=True, check=True).stdout.splitlines()
    return [out[i + 2].split("path ")[1].split(" (")[0] for i, l in enumerate(out) if "LC_RPATH" in l]

def resolve(ref, owner):
    if ref.startswith("@rpath/"):
        name = ref[len("@rpath/"):]
        for rp in rpaths(owner) + [os.path.dirname(owner), "/opt/homebrew/lib", "/opt/homebrew/opt/ggml/lib"]:
            rp = rp.replace("@loader_path", os.path.dirname(owner)).replace("@executable_path", os.path.dirname(SERVER))
            cand = os.path.join(rp, name)
            if os.path.exists(cand): return os.path.realpath(cand)
        raise SystemExit("cannot resolve %s for %s" % (ref, owner))
    if ref.startswith("@loader_path/"): return os.path.realpath(os.path.join(os.path.dirname(owner), ref[len("@loader_path/"):]))
    return os.path.realpath(ref)

shutil.rmtree(OUT, ignore_errors=True); os.makedirs(OUT)
queue = [SERVER] + BACKENDS
seen = {}        # real path -> bundled file name
refs = {}        # real path -> [(original ref, dependency real path)]
while queue:
    path = queue.pop()
    if path in seen: continue
    seen[path] = "llama-server" if path == SERVER else (os.path.basename(path) if path in BACKENDS else None)
    refs[path] = []
    for ref in deps(path):
        if ref.startswith(SYSTEM): continue
        real = resolve(ref, path)
        if real == path: continue  # own install id
        refs[path].append((ref, real))
        queue.append(real)

# Library file names: keep the name binaries ask for (e.g. libllama.0.dylib), not the versioned real file.
for path, items in refs.items():
    for ref, real in items:
        if seen.get(real) is None: seen[real] = os.path.basename(ref)

for path, name in seen.items():
    dst = os.path.join(OUT, name)
    shutil.copy2(path, dst); os.chmod(dst, 0o755)
    subprocess.run(["install_name_tool", "-id", "@loader_path/" + name, dst], check=False, capture_output=True)
    for ref, real in refs[path]:
        subprocess.run(["install_name_tool", "-change", ref, "@loader_path/" + seen[real], dst], check=True, capture_output=True)
    for rp in rpaths(dst):
        subprocess.run(["install_name_tool", "-delete_rpath", rp, dst], check=False, capture_output=True)
    subprocess.run(["install_name_tool", "-add_rpath", "@loader_path", dst], check=False, capture_output=True)
# ggml also scans a backend directory compiled into libggml (Homebrew's libexec). On a dev machine that loads a
# second copy of every backend; blank it (same length, path that never exists) so only the bundle is used.
import re
for name in seen.values():
    dst = os.path.join(OUT, name)
    data = open(dst, "rb").read()
    patched = re.sub(rb"/opt/homebrew/Cellar/ggml/[^\x00]*?/libexec", lambda m: (b"/var/empty/" + b"x" * len(m.group(0)))[:len(m.group(0))], data)
    if patched != data:
        open(dst, "wb").write(patched)
        print("blanked compiled-in backend dir in " + name)
for name in seen.values():
    subprocess.run(["codesign", "--force", "--sign", "-", os.path.join(OUT, name)], check=True, capture_output=True)

# Any reference left pointing outside the bundle is a bug.
leaks = []
for name in seen.values():
    for ref in deps(os.path.join(OUT, name)):
        if not ref.startswith(SYSTEM) and not ref.startswith("@loader_path/") and not ref.startswith("@rpath/"): leaks.append((name, ref))
print("bundled %d files into %s" % (len(seen), OUT))
for n in sorted(seen.values()): print("  " + n)
if leaks: raise SystemExit("external references left: %r" % leaks)
