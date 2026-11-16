import unittest
from unittest.mock import Mock
from pathlib import Path
from structural import workflow,generate
from local_provider import render_workflow

class StructuralTests(unittest.TestCase):
 def test_separate_inputs_and_native_crop(self):
  cfg=workflow();g=render_workflow(cfg['workflow'],{'ref:appearance':'a.png','ref:lineart':'b.png','prompt':'test','seed':42})
  self.assertEqual(g['2']['inputs']['image'],'a.png')
  self.assertEqual(g['10']['inputs']['image'],'b.png')
  self.assertEqual(g['7']['inputs']['positive'],['12',0])
  self.assertEqual(g['7']['inputs']['negative'],['12',1])
  self.assertEqual(g['12']['inputs']['image'],['10',0])
  self.assertEqual(g['13']['inputs'],dict(image=['8',0],width=1254,height=1254,x=13,y=13))
  self.assertFalse(any(n['class_type'] in ('ImageScale','SetLatentNoiseMask','ImageCompositeMasked') for n in g.values()))
 def test_missing_lineart_fails_before_submission(self):
  with self.assertRaises(ValueError):render_workflow(workflow()['workflow'],{'ref:appearance':'a.png','prompt':'test','seed':42})
 def test_draft_substitution_rejected(self):
  provider=Mock()
  with self.assertRaises(ValueError):generate(provider,{'draft':True,'reference_roles':{'appearance':'a','lineart':'b'}},[],Path('.'))
  provider.generate.assert_not_called()
 def test_roles_rejected(self):
  provider=Mock()
  with self.assertRaises(ValueError):generate(provider,{'reference_roles':{'first':'a'}},[],Path('.'))
  provider.generate.assert_not_called()

if __name__=='__main__':unittest.main()
