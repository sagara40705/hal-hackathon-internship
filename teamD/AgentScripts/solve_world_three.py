"""Search an alternate route through the hub's local switch before collecting its key."""
import heapq,itertools,json,time
from collections import deque
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Logs'/'WorldThree'
def solve(limit=25):
 data=json.loads((root/'stage3.json').read_text());allowed={tuple(p) for p in data['allowed']};target=(13,12);key=tuple(data['key']);goal=tuple(data['goal'])
 fixed={};reserved=set();groups=[]
 for g in data['groups']:
  if g['fixed']:
   for x,y,dx,dy,sw,wx,wy in g['cells']:
    p=(g['at'][0]+x,g['at'][1]+y);fixed[p]=(dx,dy,sw,wx,wy,g['state']);reserved.add(p)
  else:groups.append(g)
 # Distances ignore heading and floor state, but account for all fixed teleports.
 dirs=((1,0),(0,1),(-1,0),(0,-1));reverse={p:[] for p in allowed}
 for p in allowed:
  for dx,dy in dirs:
   n=(p[0]+dx,p[1]+dy)
   if n not in allowed:continue
   c=fixed.get(n)
   if c and (c[3] or c[4]):n=(n[0]+c[3],n[1]+c[4])
   if n in reverse:reverse[n].append(p)
 def bfs(end):
  out={end:0};q=deque([end])
  while q:
   n=q.popleft()
   for p in reverse[n]:
    if p not in out:out[p]=out[n]+1;q.append(p)
  return out
 dt,dk,dg=bfs(target),bfs(key),bfs(goal)
 def h(st):
  p,d,on,k,local,positions=st
  return (dg.get(p,99) if k else dk.get(p,99)+dg.get(key,99)) if local else dt.get(p,99)+dk.get(target,99)+dg.get(key,99)
 initial=(tuple(data['start']),tuple(data['direction']),False,False,False,tuple(tuple(g['at']) for g in groups));queue=[];counter=itertools.count();best={initial:0};parent={initial:None};action={};heapq.heappush(queue,(h(initial)*1.5,0,next(counter),initial));until=time.monotonic()+limit;expanded=0
 while queue and time.monotonic()<until:
  _,cost,_,st=heapq.heappop(queue)
  if best.get(st)!=cost:continue
  p,d,on,k,local,positions=st;expanded+=1
  if p==goal and k and local:
   steps=[];cur=st
   while parent[cur] is not None:
    old=parent[cur];a=action[cur];steps.append({'from':old[0],'position':cur[0],'direction':cur[1],'state':cur[2],'moves':[] if a is None else [a],'warp':abs(cur[0][0]-old[0][0])+abs(cur[0][1]-old[0][1])!=1});cur=old
   steps.reverse();(root/'solution3-alternate.json').write_text(json.dumps({'stage':3,'steps':steps},indent=2));print('Alternate:',len(steps),'arrivals,',sum(len(x['moves']) for x in steps),'moves,',expanded,'states');return True
  board={q:(-1,c[:5]) for q,c in fixed.items() if c[5]==-1 or bool(c[5])==on}
  for i,g in enumerate(groups):
   for x,y,dx,dy,sw,wx,wy in g['cells']:board[(positions[i][0]+x,positions[i][1]+y)]=(i,(dx,dy,sw,wx,wy))
  n=(p[0]+d[0],p[1]+d[1])
  if n not in allowed:continue
  options=[]
  if n in board:options.append((positions,board[n][1],None))
  current=board.get(p,(-1,None))[0];obstacle=board.get(n,(-2,None))[0]
  if n not in reserved:
   for i,g in enumerate(groups):
    if i==current or obstacle not in (-2,i):continue
    for x,y,dx,dy,sw,wx,wy in g['cells']:
     at=(n[0]-x,n[1]-y)
     if at==positions[i]:continue
     footprint=[(at[0]+c[0],at[1]+c[1]) for c in g['cells']]
     if any(q not in allowed or q in reserved or (q in board and board[q][0]!=i) for q in footprint):continue
     pos=list(positions);pos[i]=at;options.append((tuple(pos),(dx,dy,sw,wx,wy),{'group':g['name'],'to':at}))
  for pos,c,a in options:
   dest=n;dx,dy,sw,wx,wy=c
   if wx or wy:
    dest=(n[0]+wx,n[1]+wy)
    if dest not in board:continue
    dx,dy,sw,_,_=board[dest][1]
   nd=(dx,dy) if (dx,dy)!=(0,0) else d;ns=(dest,nd,not on if sw else on,k or dest==key,local or dest==target,pos)
   if dest==goal and not ns[3]:continue
   nc=cost+1+(.25 if a else 0)
   if nc>=best.get(ns,float('inf')):continue
   best[ns]=nc;parent[ns]=st;action[ns]=a;heapq.heappush(queue,(nc+1.5*h(ns),nc,next(counter),ns))
 print('No alternate found within budget;',expanded,'states');return False
if __name__=='__main__':solve()
