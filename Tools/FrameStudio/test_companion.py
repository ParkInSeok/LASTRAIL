import unittest,tempfile,base64,io
from pathlib import Path
from PIL import Image
from repository import Repository
from companion import configure_companion
class CompanionTests(unittest.TestCase):
 def test_valid_independent_parts_and_invalid_time_preserve_state(self):
  with tempfile.TemporaryDirectory() as d:
   r=Repository(Path(d));p={'frames':[{'duration_ms':2400}],'clip_identity':{'direction':2},'revision':0}
   b=io.BytesIO();Image.new('RGBA',(64,64),(100,80,70,120)).save(b,format='PNG')
   data={'parts':dict.fromkeys(['body','head','left','right'],base64.b64encode(b.getvalue()).decode()),'timing':{'command':920,'charge':1160,'fire':1340,'end':1460}}
   configure_companion(r,p,data);self.assertEqual(len(set(p['companion']['parts'].values())),4)
   old=p['companion'];data['timing']['fire']=100
   with self.assertRaises(ValueError):configure_companion(r,p,data)
   self.assertIs(p['companion'],old)
if __name__=='__main__':unittest.main()
