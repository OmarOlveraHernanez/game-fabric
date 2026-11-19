# Titulo neon "TAQUERÍA NEÓN" con Poppins (OFL): tubo rosa + brillo, contorno oscuro.
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import sys
# uso: python3 titulo.py ../../games/Tacos/Assets/_Game/Art/Resources/Sprites/titulo.png
out=sys.argv[1]
F='/usr/share/fonts/truetype/google-fonts/Poppins-BoldItalic.ttf'
W,H=1500,420
def layer(text,size,y,col,glowc):
    f=ImageFont.truetype(F,size)
    m=Image.new('L',(W,H),0); d=ImageDraw.Draw(m)
    d.text((W//2,y),text,font=f,anchor='mm',fill=255)
    return m
img=Image.new('RGBA',(W,H),(0,0,0,0))
for text,size,y,core,glow in [('TAQUERÍA',150,135,(255,214,240),(255,60,170)),('NEÓN',150,300,(210,255,250),(40,230,220))]:
    m=layer(text,size,y,core,glow)
    for r,a in [(28,0.55),(12,0.8),(4,1.0)]:
        g=m.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(r))
        col=Image.new('RGBA',(W,H),glow+(0,)); col.putalpha(g.point(lambda v:int(v*a)))
        img.alpha_composite(col)
    edge=m.filter(ImageFilter.MaxFilter(9))
    dark=Image.new('RGBA',(W,H),(40,8,40,0)); dark.putalpha(edge.point(lambda v:int(v*0.0)))
    tube=Image.new('RGBA',(W,H),glow+(0,)); tube.putalpha(m.filter(ImageFilter.MaxFilter(7)))
    img.alpha_composite(tube)
    core_l=Image.new('RGBA',(W,H),core+(0,)); core_l.putalpha(m.filter(ImageFilter.MinFilter(5)))
    img.alpha_composite(core_l)
bb=img.getchannel('A').point(lambda v:255 if v>8 else 0).getbbox()
img.crop(bb).save(out)
print(img.crop(bb).size)
