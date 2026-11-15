"""Bake level-two rope tightening. Run after export_level2.py; requires numpy."""
from pathlib import Path
import json, numpy as np
root=Path(__file__).resolve().parents[1]
resources=root/'Assets/JinlingKnot/Resources'
route=json.loads((resources/'Route02.json').read_text())
initial=np.array([[p[k] for k in ('x','y','z')] for p in route['points']],dtype=float)
pos=initial.copy(); ds=np.array(route['distances']); rest=np.linalg.norm(np.diff(initial,axis=0),axis=1)
anchors=[]
for c in route['crossings']:
 pair=[]
 for distance in (c['first'],c['distance']):
  i=min(len(ds)-2,np.searchsorted(ds,distance,side='right')-1);pair.append((i,(distance-ds[i])/(ds[i+1]-ds[i])))
 anchors.append(pair)
frames=[pos.copy()]
pairA,pairB=np.triu_indices(len(pos),3)
left=(initial[0]-initial[1]);left[1]=0;left/=np.linalg.norm(left)
right=(initial[-1]-initial[-2]);right[1]=0;right/=np.linalg.norm(right)
for f in range(1,151):
 t=f/150;u=t*t*(3-2*t);target=rest*(1-.14*u)
 for iteration in range(16):
  # Alternating constraints reduce rope length without a global scale transform.
  for parity in (0,1):
   ids=np.arange(parity,len(pos)-1,2);v=pos[ids+1]-pos[ids];length=np.linalg.norm(v,axis=1)
   correction=v*((length-target[ids])/np.maximum(length,1e-8)*.5)[:,None]
   pos[ids]+=correction;pos[ids+1]-=correction
  # Preserve authored over/under height and each crossing's planar coincidence.
  pos[:,1]=initial[:,1]
  for (i,a),(j,b) in anchors:
   p=pos[i]*(1-a)+pos[i+1]*a;q=pos[j]*(1-b)+pos[j+1]*b;delta=(q-p)*.35;delta[1]=0
   pos[i]+=delta;pos[i+1]+=delta;pos[j]-=delta;pos[j+1]-=delta
  delta=pos[pairB]-pos[pairA];near=np.einsum('ij,ij->i',delta,delta)<.37**2
  pairs=np.column_stack((pairA[near],pairB[near]))
  if len(pairs):
   pairs=pairs[pairs[:,1]-pairs[:,0]>2]
   if len(pairs):
    a,b=pairs.T;v=pos[b]-pos[a];length=np.linalg.norm(v,axis=1);push=v*((.37-length)/np.maximum(length,1e-8)*.5)[:,None];push[:,1]=0
    np.add.at(pos,a,-push);np.add.at(pos,b,push)
  pos[:,1]=initial[:,1]
  pos[0]=initial[0]+left*u*.8;pos[-1]=initial[-1]+right*u*.8
 frames.append(pos.copy())
cache=np.zeros((len(frames),len(pos),4),dtype='<f4');cache[:,:,:3]=np.array(frames);cache[:,:,2]*=-1
(resources/'KnotPull02.bytes').write_bytes(cache.tobytes())
route.update(pullFrames=len(frames),pullNodes=len(pos),pullDuration=5.)
(resources/'Route02.json').write_text(json.dumps(route,separators=(',',':')),encoding='utf-8')
length=np.linalg.norm(np.diff(pos,axis=0),axis=1).sum()
print(json.dumps(dict(frames=len(frames),nodes=len(pos),initialLength=float(rest.sum()),finalLength=float(length),maxDisplacement=float(np.linalg.norm(pos-initial,axis=1).max()),finite=bool(np.isfinite(cache).all()))))
