import unittest,tempfile,threading,json
from pathlib import Path
from PIL import Image,ImageChops
from domain import new_project
from refinement import refinement_request,LocalRefinement
class ReferenceRefinementTests(unittest.TestCase):
 def test_request_contains_actual_reference_role(self):
  p=new_project('ref');p['refs']['first']='art.png';p.update(refinement_mask='mask.png',refinement_prompt='hair',refinement_reference='art.png');p['frames'][0]['versions']=[{'asset':'source.png'}]
  req=refinement_request(p,0,42)
  self.assertEqual(req['reference_roles'].get('art'),'art.png')
  self.assertTrue(req.get('reference_conditioned'))
 def test_registered_reference_required(self):
  p=new_project('ref');p['refs']['first']='art.png';p.update(refinement_mask='mask.png',refinement_prompt='hair',refinement_reference='unknown.png');p['frames'][0]['versions']=[{'asset':'source.png'}]
  with self.assertRaises(ValueError):refinement_request(p,0,42)
 def test_reference_reaches_sampler_and_protected_pixels_survive(self):
  class Provider:
   cancelled=threading.Event()
   def config(self):return {'output_node':'9','workflow':{'1':{'class_type':'CheckpointLoaderSimple'},'7':{'inputs':{}}}}
   def generate(self,req,refs,work,workflow_override=None):
    self.req=req;self.graph=workflow_override['workflow'];self.refs=refs
    work.mkdir();p=work/'result.png';Image.new('RGB',(512,512),(200,10,50)).save(p);return p
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   root=Path(d);src=root/'source.png';mask=root/'mask.png';art=root/'art.png';Image.new('RGBA',(600,700),(20,30,40,255)).save(src);Image.new('RGB',(70,80),'blue').save(art);m=Image.new('L',(600,700));m.paste(255,(400,100,440,150));m.save(mask)
   req=dict(reference_conditioned=True,reference_roles={'pose':src.name,'mask':mask.name,'art':art.name},prompt='hair');provider=Provider();path=LocalRefinement(provider).generate(req,[src,mask,art],root/'job')
   self.assertIn('art',provider.req['reference_roles']);self.assertIn(art,provider.refs)
   graph=provider.graph;node=graph[graph['7']['inputs']['model'][0]];self.assertEqual(node['class_type'],'IPAdapterAdvanced');self.assertEqual(graph[node['inputs']['image'][0]]['inputs']['image'],'{{ref:art}}')
   self.assertEqual(graph['12']['class_type'],'SetLatentNoiseMask')
   out=Image.open(path).convert('RGBA');original=Image.open(src).convert('RGBA');self.assertEqual(out.size,original.size);self.assertEqual(out.getchannel('A').tobytes(),original.getchannel('A').tobytes());self.assertEqual(out.getpixel((410,120)),(200,10,50,255));self.assertEqual(out.getpixel((1,1)),original.getpixel((1,1)))
   delta=ImageChops.difference(out,original);outside=m.point(lambda v:255 if not v else 0);self.assertFalse(any(ImageChops.multiply(c,outside).getbbox() for c in delta.split()))
if __name__=='__main__':unittest.main()
