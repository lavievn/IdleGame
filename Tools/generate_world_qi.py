from pathlib import Path
import json,csv,math,random,hashlib,collections,struct
import argparse
PARSER=argparse.ArgumentParser(description="Export versioned deterministic world Qi data and debug overlay; never rewrites source artwork or runtime config.")
PARSER.add_argument('--project-root',default=str(Path(__file__).resolve().parents[1]))
PARSER.add_argument('--output-dir',required=True)
PARSER.add_argument('--seed',type=int,default=20261106)
PARSER.add_argument('--version',type=int,default=1)
PARSER.add_argument('--overrides')
ARGS=PARSER.parse_args()
if ARGS.version<1 or ARGS.version>2147483647 or not -2147483648<=ARGS.seed<=2147483647:PARSER.error('Version/seed must fit the runtime configuration integer range')
REPO=Path(ARGS.project_root).resolve();ROOT=Path(ARGS.output_dir).resolve();ROOT.mkdir(parents=True,exist_ok=True)
if ROOT==REPO or ROOT.is_relative_to(REPO/'Assets'):raise SystemExit('Export outside Assets; review data before installing a new version.')
def write(path,text):
    target=ROOT/Path(path).name
    if target.exists():
        old=json.loads(target.read_text(encoding='utf-8'));new=json.loads(text)
        if old['version']==new['version'] and old!=new:raise SystemExit('Changed data requires a new configuration version or separate output directory for review.')
    target.write_text(text,encoding='utf-8',newline='\n')
SEED=ARGS.seed
RATES=[.6,.8,1.,1.3,1.8,2.5,3.5,5.]
LABELS=['Khô cạn','Loãng','Bình thường','Khá','Dồi dào','Đậm đặc','Linh địa','Hotspot / Long mạch']
TIER_BOUNDS=[.7,.9,1.15,1.55,2.15,3.,4.75]
STARTS={'TV-20','TL-16','XD-11','TM-12','HT-17'}
def contains(v,x,y):
    inside=False
    for a,b in zip(v,v[1:]+v[:1]):
        if (a['y']>y)!=(b['y']>y) and x<(b['x']-a['x'])*(y-a['y'])/(b['y']-a['y'])+a['x']:inside=not inside
    return inside
def distance(a,b):return math.hypot(a['normalizedX']-b['normalizedX'],a['normalizedY']-b['normalizedY'])
def generate():
    art=REPO/'Assets/Resources/WorldMapArtwork.png';width,height=struct.unpack('>II',art.read_bytes()[16:24]);assert (width,height)==(1448,1086)
    source=REPO/'Assets/Resources/WorldMapPolygonSeeds.txt';polys=json.loads((REPO/'Assets/Resources/WorldMapPolygons.json').read_text(encoding='utf-8-sig'))
    polygon={p['mapId']:p for p in polys['polygons']};nodes=[]
    for line in source.read_text(encoding='utf-8-sig').splitlines():
        if not line or line.startswith('#'):continue
        node,x,y,status=line.split('\t');x,y=int(x),int(y);nx,ny=x/width,1-y/height
        assert node in polygon and contains(polygon[node]['vertices'],nx,ny),(node,'not in actual runtime click cell')
        nodes.append(dict(nodeId=node,sourceX=x,sourceY=y,normalizedX=nx,normalizedY=ny,coordinateStatus='RUNTIME_CELL_VERIFIED_'+status))
    catalog=[s.split('\t')[0] for s in (REPO/'Assets/Resources/WorldMapCatalog.txt').read_text(encoding='utf-8-sig').splitlines() if s and not s.startswith('#')]
    assert len(nodes)==len(catalog)==133 and set(catalog)=={n['nodeId'] for n in nodes}
    nodes.sort(key=lambda n:n['nodeId']);starts=[n for n in nodes if n['nodeId'] in STARTS]
    rng=random.Random(SEED);candidates=[n for n in nodes if min(distance(n,s) for s in starts)>.2];rng.shuffle(candidates)
    hotspots=[]
    for n in candidates:
        if all(distance(n,c)>.28 for c in hotspots):hotspots.append(n)
        if len(hotspots)==2:break
    assert len(hotspots)==2
    cold=[]
    for n in candidates[::-1]:
        if all(distance(n,c)>.18 for c in cold+hotspots):cold.append(n)
        if len(cold)==3:break
    medium=[dict(x=rng.uniform(.05,.95),y=rng.uniform(.05,.95),sigma=rng.uniform(.1,.19),amplitude=rng.uniform(.25,.9)) for _ in range(9)]
    phases=[rng.uniform(0,math.tau) for _ in range(3)]
    def gaussian(x,y,c,sigma):return math.exp(-((x-c[0])**2+(y-c[1])**2)/(2*sigma*sigma))
    def field(x,y):
        q=1.05+.18*math.sin(6*x+phases[0])*math.cos(5*y+phases[1])+.1*math.sin(15*x-9*y+phases[2])
        for c in medium:q+=c['amplitude']*gaussian(x,y,(c['x'],c['y']),c['sigma'])
        for c in hotspots:q+=3.8*gaussian(x,y,(c['normalizedX'],c['normalizedY']),.105)
        for c in cold:q-=2.4*gaussian(x,y,(c['normalizedX'],c['normalizedY']),.095)
        d=min(math.hypot(x-s['normalizedX'],y-s['normalizedY']) for s in starts);t=min(1,d/.19);smooth=t*t*(3-2*t)
        return max(.6,min(5.,1+(q-1)*smooth))
    overrides={n:dict(qiTier=2,reason='Approved starting baseline1Qi/s; smooth radius0.19 keeps local transition; refinement cap1/s retains60minutes.') for n in sorted(STARTS)}
    override_file=Path(ARGS.overrides) if ARGS.overrides else REPO/'Tools/data/WorldQiOverridesV1.json'
    if override_file.exists():overrides=json.loads(override_file.read_text(encoding='utf-8'))
    else:raise SystemExit('Reviewed manual override file is missing')
    if set(overrides)-set(catalog):raise ValueError('Manual overrides contain unknown stable node IDs')
    for n in nodes:
        q=field(n['normalizedX'],n['normalizedY']);tier=sum(q>=bound for bound in TIER_BOUNDS)
        override=overrides.get(n['nodeId']);tier=override['qiTier'] if override else tier
        assert type(tier) is int and 0<=tier<8
        n.update(qiTier=tier,qiPerSecond=RATES[tier],manualOverride=override is not None,provenance=override['reason'] if override else 'seeded-correlated-field-v1',generatorVersion=1,configurationVersion=ARGS.version,seed=SEED)
    assert all(n['qiPerSecond']==1 for n in starts) and min(n['qiPerSecond'] for n in nodes)==.6 and max(n['qiPerSecond'] for n in nodes)==5
    edges=[]
    for i,a in enumerate(nodes):
        for b in nodes[i+1:]:
            dist=distance(a,b)
            if dist<.085:edges.append(dict(a=a['nodeId'],b=b['nodeId'],distance=dist,rateDelta=abs(a['qiPerSecond']-b['qiPerSecond']),tierDelta=abs(a['qiTier']-b['qiTier']),intentionalOverride=a['manualOverride'] or b['manualOverride']))
    assert max(e['rateDelta'] for e in edges if not e['intentionalOverride'])<=1.5,'Unexplained abrupt near-node discontinuity'
    summary=dict(count=133,countsByTier=dict(sorted(collections.Counter(n['qiTier'] for n in nodes).items())),minimum=.6,maximum=5,seed=SEED,generatorVersion=1,hotspotCenters=[n['nodeId'] for n in hotspots],coldCenters=[n['nodeId'] for n in cold],largestNearDiscontinuities=sorted(edges,key=lambda e:e['rateDelta'],reverse=True)[:12])
    definition=dict(schemaVersion=1,id='world-qi:atlas-v1',version=ARGS.version,seed=SEED,generatorVersion=1,artworkWidth=width,artworkHeight=height,coordinateOrigin='source:top-left-pixels; normalized:bottom-left',coordinateSourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),artworkSha256=hashlib.sha256(art.read_bytes()).hexdigest(),tiers=[dict(tier=i,qiPerSecond=q,label=LABELS[i]) for i,q in enumerate(RATES)],nodes=nodes)
    write('Assets/Resources/WorldQiDistributionV1.json',json.dumps(definition,ensure_ascii=False,indent=2)+'\n')
    out=ROOT;out.mkdir(exist_ok=True)
    for name in ['world-qi-v1.csv','world-coordinates.csv']:
        columns=list(nodes[0]) if name.startswith('world-qi') else ['nodeId','sourceX','sourceY','normalizedX','normalizedY','coordinateStatus']
        with (out/name).open('w',encoding='utf-8-sig',newline='') as f:
            w=csv.DictWriter(f,fieldnames=columns,extrasaction='ignore');w.writeheader();w.writerows(nodes)
    (out/'qi-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    (out/'generator-spec.json').write_text(json.dumps(dict(seed=SEED,phases=phases,medium=medium,hotspotCenters=summary['hotspotCenters'],coldCenters=summary['coldCenters'],overrides=overrides),ensure_ascii=False,indent=2),encoding='utf-8')
    # Debug overlay uses existing source art without changing it. Pixel Qi is procedural data, not extracted from a reference heatmap.
    try:
        from PIL import Image,ImageDraw
        art_image=Image.open(art).convert('RGBA');overlay=Image.new('RGBA',art_image.size)
        palette=['#496ca7','#6e97b0','#74b4a0','#a2c680','#d9cd65','#e7a14f','#d96748','#b63456']
        pixels=overlay.load()
        for sy in range(0,height,4):
            for sx in range(0,width,4):
                tier=sum(field(sx/width,1-sy/height)>=bound for bound in TIER_BOUNDS);c=palette[tier];rgb=tuple(int(c[j:j+2],16) for j in (1,3,5))
                for yy in range(sy,min(height,sy+4)):
                    for xx in range(sx,min(width,sx+4)):pixels[xx,yy]=(*rgb,100)
        art_image=Image.alpha_composite(art_image,overlay);draw=ImageDraw.Draw(art_image)
        for n in nodes:
            x,y=n['sourceX'],n['sourceY'];draw.ellipse((x-6,y-6,x+6,y+6),fill=palette[n['qiTier']],outline='white',width=2)
            if n['manualOverride']:draw.rectangle((x-9,y-9,x+9,y+9),outline='white',width=2)
        art_image.convert('RGB').save(out/'world-qi-debug-overlay.png')
    except ImportError:pass
    print(json.dumps(summary,ensure_ascii=False,indent=2))
if __name__=='__main__':generate()
