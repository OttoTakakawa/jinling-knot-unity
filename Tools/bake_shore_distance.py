"""Bake static quay distance. Rerun after CityGeometry changes; standard Python only."""
import json, struct, hashlib, math, array
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/JinlingKnot/Resources'
meta=json.loads((root/'City.json').read_text());data=(root/'CityGeometry.bytes').read_bytes();offset=0;segments=[];level=-.43
for m in meta['meshes']:
    v=[struct.unpack_from('<6f',data,offset+j*24)[:3] for j in range(m['vertices'])];offset+=m['vertices']*24
    idx=struct.unpack_from('<'+'i'*m['indices'],data,offset);offset+=m['indices']*4
    low=min(p[1] for p in v);high=max(p[1] for p in v)
    if m['water'] or m['group']!=-1 or high-low>1 or not low<level<high:continue
    for j in range(0,len(idx),3):
        points=[]
        for k in range(3):
            a=v[idx[j+k]];b=v[idx[j+(k+1)%3]]
            if (a[1]<level)==(b[1]<level):continue
            t=(level-a[1])/(b[1]-a[1]);points.append((a[0]+(b[0]-a[0])*t,a[2]+(b[2]-a[2])*t))
        if len(points)==2:
            a,b=points;ex=b[0]-a[0];ez=b[1]-a[1];segments.append((a[0],a[1],ex,ez,1/(ex*ex+ez*ez)))
n=512;values=array.array('f')
for row in range(n):
    z=-32+(row+.5)*64/n
    for col in range(n):
        x=-32+(col+.5)*64/n;best=10000
        for ax,az,ex,ez,inv in segments:
            dx=x-ax;dz=z-az;h=min(1,max(0,(dx*ex+dz*ez)*inv));d=(dx-h*ex)**2+(dz-h*ez)**2;best=min(best,d)
        values.append(math.sqrt(best))
(root/'ShoreDistance.bytes').write_bytes(values.tobytes())
(root/'ShoreDistanceInfo.json').write_text(json.dumps(dict(size=n,min=-32,span=64,segments=len(segments),source_sha256=hashlib.sha256(data).hexdigest()),indent=2))
print('Baked',len(segments),'shore segments into',n,'x',n,'distance samples')
