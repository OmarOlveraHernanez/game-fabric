from PIL import Image, ImageDraw
M = {'papi_correr_0':2,'papi_correr_1':1,'papi_salto':4,'papi_golpe':5}
ims = {k:Image.open(f'raw/raw_{v}.png') for k,v in M.items()}
s = min(min(240/i.width, 240/i.height) for i in ims.values())
print('escala', round(s,3))
import os; os.makedirs('out', exist_ok=True)
prev = Image.new('RGBA',(256*4+50,256+20),(235,220,190,255)); d=ImageDraw.Draw(prev)
for n,(k,i) in enumerate(ims.items()):
    i = i.crop(i.getbbox())
    i = i.resize((round(i.width*s), round(i.height*s)), Image.LANCZOS)
    c = Image.new('RGBA',(256,256),(0,0,0,0))
    x = 248 - i.width if k.startswith('papi_correr') else (256-i.width)//2
    c.alpha_composite(i,(x, 250-i.height))
    c.save(f'out/{k}.png'); print(k, i.size)
    prev.alpha_composite(c,(10+n*(256+10),10)); d.rectangle([10+n*266,10,10+n*266+255,265],outline=(150,120,90))
prev.save('/mnt/user-data/outputs/papi_sprites_preview.png')
