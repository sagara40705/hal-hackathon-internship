import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Logs'/'WorldThree'
def verify(s):
 d=json.loads((root/f'stage{s}.json').read_text());groups={g['name']:dict(g,at=tuple(g['at'])) for g in d['groups']}
 def m(name,x,y):return {'group':name,'to':[x,y]}
 if s==1:events={(8,14,1):[m('Arrival_Bridge',12,14)],(19,14,1):[m('Arrival_Rise',23,14)]}
 elif s==2:events={(10,12,1):[m('Loop_Link',11,11)],(22,18,1):[m('Loop_Corner',24,18)],(11,18,1):[m('Loop_Descent',8,18)],(8,17,1):[m('Loop_Bridge',10,16)]}
 elif s==3:events={(14,14,1):[m('Hub_LinkPiece',14,15)],(9,21,1):[m('Hub_LinkPiece',16,13)],(23,10,1):[m('Hub_UpLeft',24,10)],(18,16,1):[m('Hub_LinkPiece',16,15)]}
 else:events={(21,11,1):[m('Relay_Descent',7,18)],(10,16,1):[m('Relay_Bridge',14,16)],(21,20,1):[m('Relay_UpLeft',25,20)],(25,21,1):[m('Relay_Bridge',22,22)],(16,24,1):[m('Relay_Descent',14,24)]}
 allowed={tuple(p) for p in d['allowed']};state=False
 def board(all_cells=False):
  b={}
  for name,g in groups.items():
   if not all_cells and g['state']!=-1 and bool(g['state'])!=state:continue
   for x,y,dx,dy,button,wx,wy in g['cells']:
    p=(g['at'][0]+x,g['at'][1]+y);assert p not in b,('overlap',s,p,name);assert p in allowed,('outside',s,p);b[p]=(name,dx,dy,button,wx,wy)
  return b
 p=tuple(d['start']);direction=tuple(d['direction']);got_key=False;steps=[];visits={};warps=0;toggles=0
 for _ in range(150):
  visits[p]=visits.get(p,0)+1;actions=events.get((*p,visits[p]),[])
  for a in actions:
   g=groups[a['group']];assert not g['fixed'] and board()[p][0]!=a['group'],('reserved',s,p,a);g['at']=tuple(a['to']);board(True)
  n=(p[0]+direction[0],p[1]+direction[1]);b=board();assert n in b,('fall',s,n,p,state)
  c=b[n];arrival=n;warp=False
  if c[4] or c[5]:n=(n[0]+c[4],n[1]+c[5]);assert n in b,('warp missing landing',s,arrival,n);c=b[n];warps+=1;warp=True
  if c[3]:state=not state;toggles+=1;assert n in board(),('lost support at switch',s,n)
  nd=(c[1],c[2]) if (c[1],c[2])!=(0,0) else direction
  got_key|=n==tuple(d['key']);steps.append({'from':p,'position':n,'entered':arrival,'direction':nd,'moves':actions,'state':state,'warp':warp});p=n;direction=nd
  if p==tuple(d['goal']):assert got_key;break
 else:raise AssertionError('no goal')
 (root/f'solution{s}.json').write_text(json.dumps({'stage':s,'steps':steps},indent=2))
 print(s,len(steps),'arrivals',sum(len(x['moves']) for x in steps),'moves',warps,'warps',toggles,'toggles')
if __name__=='__main__':
 for s in range(1,5):verify(s)
