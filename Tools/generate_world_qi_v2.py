"""Offline v2 proposal exporter. Does not install runtime data, alter v1 or sample image Qi."""
from pathlib import Path
import argparse,json,math,random,csv,hashlib,collections,struct
STARTS={'TV-20','TL-16','XD-11','TM-12','HT-17'}
BOUNDS=[.7,.9,1.15,1.55,2.15,3,4.75]
def digest(data):return hashlib.sha256(data).hexdigest()
def generate(project,output,config=None,overrides=None,render=True):
    project=Path(project).resolve();output=Path(output).resolve()
    if output==project or output.is_relative_to(project/'Assets'):raise ValueError('Export outside Assets; activation is separate.')
    d=json.loads((Path(config) if config else project/'Tools/data/WorldQiFieldV2.json').read_text(encoding='utf-8-sig'))
    o=json.loads((Path(overrides) if overrides else project/'Tools/data/WorldQiOverridesV2.json').read_text(encoding='utf-8-sig'))
    if d['version']<2 or d['generatorVersion']!=2 or d['id']!='world-qi:atlas-v2':raise ValueError('Explicit v2 data/generator identity required')
    if type(d['seed']) is not int or not -2147483648<=d['seed']<=2147483647 or type(d['version']) is not int:raise ValueError('Seed/version must be explicit32-bit integers')
    if not all(math.isfinite(d[key]) and d[key]>0 for key in ['peak','hotSigma','startBuffer']) or not .6<d['peak']<=5 or not 0<d['startBuffer']<.3 or type(d['mediumCount']) is not int or not 0<=d['mediumCount']<=20:raise ValueError('Invalid finite spatial field parameters')
    for c in d['targets']+d['cold']:
        if not all(math.isfinite(c[key]) and 0<=c[key]<=1 for key in ['x','y']):raise ValueError('Invalid region coordinates')
        if 'sigma' in c and (not math.isfinite(c['sigma']) or c['sigma']<=0 or not math.isfinite(c['amplitude']) or c['amplitude']<0):raise ValueError('Invalid cold field parameters')
    original=project/'Assets/Resources/WorldQiDistributionV1.json';original_hash=digest(original.read_bytes());v1=json.loads(original.read_text(encoding='utf-8-sig'))
    art=project/'Assets/Resources/WorldMapArtwork.png';width,height=struct.unpack('>II',art.read_bytes()[16:24]);assert (width,height)==(1448,1086)
    seeds=project/'Assets/Resources/WorldMapPolygonSeeds.txt';polygons=json.loads((project/'Assets/Resources/WorldMapPolygons.json').read_text(encoding='utf-8-sig'))
    polygon={p['mapId']:p for p in polygons['polygons']}
    regions={x.split('\t')[0]:x.split('\t')[1] for x in (project/'Assets/Resources/WorldMapCatalog.txt').read_text(encoding='utf-8-sig').splitlines() if x and not x.startswith('#')}
    def contains(v,x,y):
        inside=False
        for a,b in zip(v,v[1:]+v[:1]):
            if (a['y']>y)!=(b['y']>y) and x<(b['x']-a['x'])*(y-a['y'])/(b['y']-a['y'])+a['x']:inside=not inside
        return inside
    nodes=[]
    for line in seeds.read_text(encoding='utf-8-sig').splitlines():
        if not line or line.startswith('#'):continue
        node,x,y,status=line.split('\t');x,y=int(x),int(y);nx,ny=x/width,1-y/height
        assert contains(polygon[node]['vertices'],nx,ny)
        nodes.append(dict(nodeId=node,sourceX=x,sourceY=y,normalizedX=nx,normalizedY=ny,coordinateStatus='RUNTIME_CELL_VERIFIED_'+status))
    nodes.sort(key=lambda n:n['nodeId']);assert len(nodes)==len(regions)==133 and set(regions)=={n['nodeId'] for n in nodes}
    if set(o)-set(regions):raise ValueError('Unknown manual override ID')
    for value in o.values():
        if type(value.get('qiTier')) is not int or not 0<=value['qiTier']<8 or not value.get('reason'):raise ValueError('Invalid override/provenance')
    by_id={n['nodeId']:n for n in nodes};starts=[by_id[x] for x in sorted(STARTS)]
    def dist(a,b):return math.hypot(a['normalizedX']-b['normalizedX'],a['normalizedY']-b['normalizedY'])
    eligible=[n for n in nodes if min(dist(n,s) for s in starts)>=d['startBuffer']+.01]
    centers=[]
    for target in d['targets']:
        available=[n for n in eligible if all(dist(n,c)>.2 for c in centers)]
        node=min(available,key=lambda n:(math.hypot(n['normalizedX']-target['x'],n['normalizedY']-target['y']),n['nodeId']))
        assert math.hypot(node['normalizedX']-target['x'],node['normalizedY']-target['y'])<.20,'Regional target cannot be registered safely'
        centers.append(dict(node,geographicRegion=target['region']))
    rng=random.Random(d['seed']);phase=[rng.uniform(0,math.tau) for _ in range(3)]
    medium=[dict(x=rng.uniform(.07,.93),y=rng.uniform(.07,.93),sigma=rng.uniform(.10,.18),amplitude=rng.uniform(.25,.80)) for _ in range(d['mediumCount'])]
    def gaussian(x,y,c,sigma):return math.exp(-((x-c[0])**2+(y-c[1])**2)/(2*sigma*sigma))
    def base(x,y):
        q=.92+.14*math.sin(6*x+phase[0])*math.cos(5*y+phase[1])+.08*math.sin(15*x-9*y+phase[2])
        for c in medium:q+=c['amplitude']*gaussian(x,y,(c['x'],c['y']),c['sigma'])
        for c in d['cold']:q-=c['amplitude']*gaussian(x,y,(c['x'],c['y']),c['sigma'])
        return q
    def field(x,y):
        q=base(x,y)
        for c in centers:q+=(d['peak']-base(c['normalizedX'],c['normalizedY']))*gaussian(x,y,(c['normalizedX'],c['normalizedY']),d['hotSigma'])
        r=min(math.hypot(x-s['normalizedX'],y-s['normalizedY']) for s in starts);t=min(1,r/d['startBuffer']);t=t*t*(3-2*t)
        return max(.6,min(5,1+(q-1)*t))
    for n in nodes:
        tier=sum(field(n['normalizedX'],n['normalizedY'])>=b for b in BOUNDS);override=o.get(n['nodeId'])
        if override:tier=override['qiTier']
        n.update(qiTier=tier,qiPerSecond=v1['tiers'][tier]['qiPerSecond'],manualOverride=override is not None,provenance=override['reason'] if override else 'registered-regional-correlated-field-v2; proposed, not official long-mai',generatorVersion=2,configurationVersion=d['version'],seed=d['seed'])
    assert all(by_id[x]['qiPerSecond']==1 for x in STARTS)
    assert min(n['qiPerSecond'] for n in nodes)==.6 and max(n['qiPerSecond'] for n in nodes)==5
    table=dict(schemaVersion=1,id=d['id'],version=d['version'],seed=d['seed'],generatorVersion=2,artworkWidth=width,artworkHeight=height,coordinateOrigin=v1['coordinateOrigin'],coordinateSourceSha256=digest(seeds.read_bytes()),artworkSha256=digest(art.read_bytes()),tiers=v1['tiers'],nodes=nodes)
    reviewed=project/'Tools/data/WorldQiDistributionV2.json'
    if reviewed.exists():
        accepted=json.loads(reviewed.read_text(encoding='utf-8-sig'))
        if accepted['version']==table['version'] and accepted!=table:raise ValueError('Changing reviewed seed/field/overrides requires a new data version')
    output.mkdir(parents=True,exist_ok=True);target=output/'WorldQiDistributionV2.json';text=json.dumps(table,ensure_ascii=False,indent=2)+'\n'
    if target.exists():
        old=json.loads(target.read_text(encoding='utf-8-sig'))
        if old['version']==table['version'] and old!=table:raise ValueError('Changed data requires a new configuration version')
    target.write_text(text,encoding='utf-8',newline='\n')
    pairs=[]
    for i,a in enumerate(nodes):
        for b in nodes[i+1:]:
            distance=dist(a,b)
            if distance<.085:pairs.append(dict(a=a['nodeId'],b=b['nodeId'],distance=distance,rateDelta=abs(a['qiPerSecond']-b['qiPerSecond']),override=a['manualOverride'] or b['manualOverride']))
    counts=lambda ns:dict(sorted(collections.Counter(n['qiTier'] for n in ns).items()))
    region_stats={r:dict(count=sum(regions[n['nodeId']]==r for n in nodes),v1=counts([n for n in v1['nodes'] if regions[n['nodeId']]==r]),v2=counts([n for n in nodes if regions[n['nodeId']]==r])) for r in sorted(set(regions.values()))}
    summary=dict(id=d['id'],version=d['version'],seed=d['seed'],generatorVersion=2,count=len(nodes),sha256=digest(text.encode()),v1Sha256=original_hash,v1Counts=counts(v1['nodes']),v2Counts=counts(nodes),regions=region_stats,centers=[dict(nodeId=c['nodeId'],region=c['geographicRegion'],catalogRegion=regions[c['nodeId']],normalizedX=c['normalizedX'],normalizedY=c['normalizedY']) for c in centers],hotNodes=[n['nodeId'] for n in nodes if n['qiTier']==7],largestNearDiscontinuities=sorted(pairs,key=lambda x:x['rateDelta'],reverse=True)[:12],referenceSha256=d['referenceSha256'],referenceUsage=d['referenceUsage'],activation='V2 READY - ACTIVATION BLOCKED: no supported v1 Save configuration upgrade')
    (output/'world-qi-v2-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (output/'world-qi-v2-generator.json').write_text(json.dumps(dict(configuration=d,overrides=o,phases=phase,medium=medium),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    with (output/'world-qi-v2.csv').open('w',encoding='utf-8-sig',newline='') as f:
        cols=list(nodes[0])+['catalogRegion','v1QiPerSecond'];writer=csv.DictWriter(f,fieldnames=cols);writer.writeheader();old={n['nodeId']:n for n in v1['nodes']}
        for n in nodes:writer.writerow(dict(n,catalogRegion=regions[n['nodeId']],v1QiPerSecond=old[n['nodeId']]['qiPerSecond']))
    if render:
        from PIL import Image,ImageDraw
        palette=['#496ca7','#6e97b0','#74b4a0','#a2c680','#d9cd65','#e7a14f','#d96748','#b63456']
        image=Image.open(art).convert('RGBA');overlay=Image.new('RGBA',image.size);draw=ImageDraw.Draw(overlay)
        for y in range(0,height,4):
            for x in range(0,width,4):
                tier=sum(field(x/width,1-y/height)>=b for b in BOUNDS);rgb=tuple(int(palette[tier][j:j+2],16) for j in (1,3,5));draw.rectangle((x,y,x+3,y+3),fill=(*rgb,105))
        image=Image.alpha_composite(image,overlay);draw=ImageDraw.Draw(image)
        for n in nodes:
            x,y=n['sourceX'],n['sourceY'];draw.ellipse((x-6,y-6,x+6,y+6),fill=palette[n['qiTier']],outline='white',width=2)
            if n['manualOverride']:draw.rectangle((x-10,y-10,x+10,y+10),outline='white',width=2)
        for c in centers:draw.text((c['sourceX']+8,c['sourceY']-12),c['nodeId']+' '+c['geographicRegion'],fill='white',stroke_fill='black',stroke_width=2)
        draw.rectangle((24,height-105,width-24,height-30),fill='#17232d')
        draw.text((36,height-100),'V2 PROPOSAL - not active | seed '+str(d['seed'])+' | squares: starts | dots: final node tier',fill='white')
        for i,rate in enumerate(v1['tiers']):draw.rectangle((40+165*i,height-76,56+165*i,height-60),fill=palette[i]);draw.text((61+165*i,height-77),str(rate['qiPerSecond'])+' Qi/s',fill='white')
        image.convert('RGB').save(output/'world-qi-v2-debug-overlay.png')
    assert digest(original.read_bytes())==original_hash,'v1 changed'
    return summary
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--project-root',default=str(Path(__file__).resolve().parents[1]));p.add_argument('--output-dir',required=True);p.add_argument('--configuration');p.add_argument('--overrides');p.add_argument('--no-render',action='store_true');a=p.parse_args()
    print(json.dumps(generate(a.project_root,a.output_dir,a.configuration,a.overrides,not a.no_render),ensure_ascii=False,indent=2))
