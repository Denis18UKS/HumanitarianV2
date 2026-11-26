from importlib.metadata import distributions
from pathlib import Path
out=Path('dist')/'ThirdPartyLicenses';out.mkdir(parents=True,exist_ok=True)
for dist in distributions():
    name=dist.metadata.get('Name','unknown').replace('/','_').replace('\\','_');dest=out/name;dest.mkdir(exist_ok=True)
    (dest/'METADATA.txt').write_text(str(dist.metadata),encoding='utf-8')
    for f in dist.files or []:
        if any(k in f.name.lower() for k in ('license','licence','copying','notice','copyright','authors')):
            p=dist.locate_file(f)
            if p.is_file():
                try:(dest/('__'.join(f.parts)[-180:])).write_bytes(p.read_bytes())
                except OSError:pass
