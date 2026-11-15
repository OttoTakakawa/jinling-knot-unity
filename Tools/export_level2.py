# Run with Blender --background --factory-startup <source.blend> --python Tools/export_level2.py
import json,math,pathlib
root=pathlib.Path(__file__).resolve().parents[1]; scripts=root/'Assets/JinlingKnot/Scripts'; res=root/'Assets/JinlingKnot/Resources'
import bpy
o=next(o for o in bpy.data.objects if o.type=='CURVE' and o.name=='单根线_MainCurve')
p=[list(o.matrix_world @ v.co.xyz) for v in o.data.splines[0].points]; p=[(x*3,1+z*3,-y*3) for x,y,z in p]
ds=[0.]
for a,b in zip(p,p[1:]): ds.append(ds[-1]+math.dist(a,b))
total=ds[-1]; duration=60.; speed=total/duration
cross=[]
for j in range(2,len(p)-1):
 for i in range(j-2):
  a,b,c,d=p[i],p[i+1],p[j],p[j+1]; ux,uz=b[0]-a[0],b[2]-a[2]; vx,vz=d[0]-c[0],d[2]-c[2]; den=ux*vz-uz*vx
  if abs(den)<1e-9: continue
  wx,wz=c[0]-a[0],c[2]-a[2]; t=(wx*vz-wz*vx)/den; u=(wx*uz-wz*ux)/den
  if 0<=t<1 and 0<=u<1:
   first=ds[i]+t*(ds[i+1]-ds[i]); dist=ds[j]+u*(ds[j+1]-ds[j]); side=1 if c[1]+u*(d[1]-c[1])>a[1]+t*(b[1]-a[1]) else -1
   if dist-first>1 and all(abs(dist-e['distance'])>.5 for e in cross): cross.append(dict(first=first,distance=dist,side=side,automatic=False))
cross.sort(key=lambda e:e['distance'])
# Rank broad bends by curvature, then enforce playable separation in time.
candidates=[]
for i in range(6,len(p)-6):
 a=[p[i][k]-p[i-6][k] for k in (0,2)]; b=[p[i+6][k]-p[i][k] for k in (0,2)]; n=math.hypot(*a)*math.hypot(*b)
 angle=math.acos(max(-1,min(1,sum(x*y for x,y in zip(a,b))/n))) if n else 0
 candidates.append((angle,i))
turns=[]
for angle,i in sorted(candidates,reverse=True):
 d=ds[i]
 if angle>.17 and speed<d<total-speed and all(abs(d-t)>speed*1.8 for t in turns) and all(abs(d-e['distance'])>speed*.65 for e in cross): turns.append(d)
turns.sort()
events=[dict(action='turn',index=i,time=d/speed,distance=d) for i,d in enumerate(turns)]+[dict(action='cross',index=i,time=e['distance']/speed,distance=e['distance']) for i,e in enumerate(cross)]
optional=[dict(action='tap',index=i,time=t,distance=t*speed) for i,t in enumerate(range(2,59)) if all(abs(t-e['time'])>.65 for e in events)]
data=dict(points=[dict(zip(('x','y','z'),v)) for v in p],distances=ds,turns=turns,crossings=cross,events=events,optionalEvents=optional,notes=[dict(time=t,pitch=[74,76,78,81][t%4]) for t in range(61)],times=[0,duration],beatDistances=[0,total],velocities=[speed,speed],wrapStart=total,endTime=duration,pullDuration=5,pullFrames=0,pullNodes=0,pullDistances=ds)
(res/'Route02.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
