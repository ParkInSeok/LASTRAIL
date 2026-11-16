import unittest,copy
from domain import new_project,retime_frames

class RetimeTests(unittest.TestCase):
 def test_retime_retains_art_history_and_invalidates_approval(self):
  p=new_project('timing')
  for i,f in enumerate(p['frames']):f['versions']=[{'asset':str(i)+'.png'}];f['approved']=True
  p['review_export']={'gif':'old.gif'};before=copy.deepcopy(p);durations=[70]+[130]*10+[70]
  retime_frames(p,durations)
  self.assertEqual(sum(durations),1440);self.assertEqual([f['versions'] for f in p['frames']],[f['versions'] for f in before['frames']]);self.assertTrue(all(not f['approved'] for f in p['frames']));self.assertNotIn('review_export',p);self.assertEqual(p['timing_history'][-1]['durations_ms'],[120]*12)
 def test_invalid_time_is_atomic_and_same_time_is_noop(self):
  p=new_project('timing');before=copy.deepcopy(p)
  for values in [[70],[120]*11+[125],[120]*11+[True]]:
   with self.assertRaises(ValueError):retime_frames(p,values)
   self.assertEqual(p,before)
  retime_frames(p,[120]*12);self.assertEqual(p,before)

if __name__=='__main__':unittest.main()
