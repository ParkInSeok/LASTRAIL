import unittest
from pathlib import Path
from combat_review import resolve_media, REVIEWS, page

class CombatReviewTests(unittest.TestCase):
 def test_sources_exist_and_are_independent(self):
  self.assertEqual({x['character'] for x in REVIEWS}, {'Reina','Eve','Sera','Nyx','Kyle'})
  for row in REVIEWS:
   self.assertTrue(resolve_media(row['path'].split('?')[0]).is_file())
 def test_cannot_escape_or_read_source_secrets(self):
  for value in ['../server.py','%2e%2e/server.py','/server.py','kyle-guard/../../../../../AGENTS.md','d-console/build.py']:
   with self.assertRaises(ValueError):resolve_media(value)
 def test_project_binding_and_no_network_dependency(self):
  text=page('63da550f13b44fdfa49cc2c7e0e3249c')
  self.assertIn('studio-left-ultimate-20261107/battle.html',text)
  self.assertNotIn('127.0.0.1:8768',text)
  self.assertIn('iframe',text)
if __name__=='__main__':unittest.main()
