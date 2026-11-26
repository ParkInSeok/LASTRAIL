import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
p=Path(__file__).parent
data=json.loads((p/'operator-motion-final-input.json').read_text(encoding='utf-8'))
f=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
for batch in range(2):
    sheet=Image.new('RGB',(7*220,1010),(20,24,30));d=ImageDraw.Draw(sheet)
    for x,s in enumerate(data[batch*7:(batch+1)*7]):
        d.text((x*220+8,5),s['name'],font=f,fill=(230,220,190))
        for y,n in enumerate([0,1,2]):
            im=Image.open(s['sources'][n]).convert('RGBA').resize((220,330))
            sheet.paste(im,(x*220,20+y*330),im)
    sheet.save(p/f'operator-contact-{batch}.jpg',quality=94)
