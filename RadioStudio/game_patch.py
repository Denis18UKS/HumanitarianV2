import hashlib, struct, wave, pathlib, sys, os, json
P=pathlib.Path(__file__).resolve().parent
AH='4699233561c0b0d3b9a7956e97cad4d9529e047646f516e1d6be2d0591019093'
RH='ee312e5ce2010c6ecd9ed297d2661afe9a90e525b937b5161535e6589aa312e2'
NAMES=[f'ConvoyDispatch{i}' for i in range(5)]
def sha(b):return hashlib.sha256(b).hexdigest()
def fsb(w):
    with wave.open(str(w),'rb') as f:
        if (f.getnchannels(),f.getsampwidth(),f.getframerate(),f.getcomptype())!=(1,2,24000,'NONE'):raise ValueError(f'Expected 24 kHz mono PCM16: {w}')
        n=f.getnframes(); pcm=f.readframes(n)
    if not 0<n<24000*30 or len(pcm)!=n*2:raise ValueError('Bad WAV duration/data')
    data=pcm+b'\0'*((-len(pcm))%32)
    header=struct.pack('<4s6I',b'FSB5',1,1,8,0,len(data),2)+bytes(32)
    bank=header+struct.pack('<Q',(n<<34)|(6<<1))+data
    assert len(header)==60 and len(bank)==68+len(data)
    return bank,n

def patch(a,r,folder):
    if sha(a)!=AH or sha(r)!=RH:raise ValueError('Unsupported build: original resource hashes differ. No files modified.')
    a=bytearray(a);r=bytearray(r)
    info=[]
    for name in NAMES:
        key=name.encode();j=a.find(key)
        if j<0 or a.count(key)!=1 or struct.unpack_from('<I',a,j-4)[0]!=15 or a[j+52:j+70]!=b'resources.resource':raise ValueError('Unexpected AudioClip layout '+name)
        off,size=struct.unpack_from('<QQ',a,j+72)
        if r[off:off+4]!=b'FSB5' or off+size>len(r) or struct.unpack_from('<I',a,j+88)[0]!=1:raise ValueError('Unexpected original audio bank')
        bank,n=fsb(folder/(name+'.wav'))
        r[off:off+size]=bytes(size)
        r.extend(bytes((-len(r))%32)); newoff=len(r);r.extend(bank)
        struct.pack_into('<i',a,j+24,24000);struct.pack_into('<f',a,j+32,n/24000)
        struct.pack_into('<QQI',a,j+72,newoff,len(bank),0)
        info.append(dict(name=name,old_offset=off,old_size=size,new_offset=newoff,new_size=len(bank),seconds=n/24000))
    return bytes(a),bytes(r),info

def install(root,folder):
    d=root if (root/'resources.assets').is_file() else root/'Humanitarian_Unity_Data'
    a=d/'resources.assets';r=d/'resources.resource'
    if not a.is_file() or not r.is_file():raise ValueError('Choose folder containing Humanitarian_Unity.exe (or its _Data folder)')
    backup=pathlib.Path(os.environ.get('LOCALAPPDATA',str(P)))/'HumanitarianRadioDLC'/'Backup_Astra07'
    ba=backup/a.name;br=backup/r.name
    original_a=a.read_bytes();original_r=r.read_bytes()
    if sha(original_a)==AH and sha(original_r)==RH:
        newa,newr,info=patch(original_a,original_r,folder)
    else:
        meta=backup/'installed.json'
        if not meta.exists() or not ba.exists() or not br.exists():raise ValueError('Build mismatch; no modifications')
        old=json.loads(meta.read_text())
        if [sha(original_a),sha(original_r)]!=old['installed']:raise ValueError('Current build differs from previous DLC; no modifications')
        newa,newr,info=patch(ba.read_bytes(),br.read_bytes(),folder)
    backup.mkdir(parents=True,exist_ok=True)
    if not ba.exists():ba.write_bytes(original_a)
    if not br.exists():br.write_bytes(original_r)
    if sha(ba.read_bytes())!=AH or sha(br.read_bytes())!=RH:raise ValueError('Invalid backup; no changes')
    ta=a.with_name(a.name+'.dlc.tmp');tr=r.with_name(r.name+'.dlc.tmp')
    try:
        ta.write_bytes(newa);tr.write_bytes(newr)
        os.replace(tr,r)
        try:os.replace(ta,a)
        except Exception:
            r.write_bytes(original_r);raise
    finally:
        ta.unlink(missing_ok=True);tr.unlink(missing_ok=True)
    (backup/'installed.json').write_text(json.dumps({'installed':[sha(newa),sha(newr)],'clips':info},indent=2))
    print('Installed five clips. Backup outside game:',backup)
    for i in info:print(i['name'],round(i['seconds'],2),'s')
    print('Game version, EXE and save files unchanged.')

def main():
    folder=P/('NeuralAudio' if '--neural' in sys.argv else 'Audio')
    root=pathlib.Path(input('Game folder (containing Humanitarian_Unity.exe): ').strip().strip('"'))
    install(root,folder)
if __name__=='__main__':
    try: main()
    except Exception as e:print('ERROR:',e);sys.exit(1)
