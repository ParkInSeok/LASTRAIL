import json,tempfile,threading,unittest
from pathlib import Path
from urllib.request import Request,urlopen
from urllib.error import HTTPError
from server import make_server

class HttpTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.server=make_server(0,Path(self.tmp.name)/'data')
        self.thread=threading.Thread(target=self.server.serve_forever,daemon=True);self.thread.start()
        self.base='http://127.0.0.1:'+str(self.server.server_port)
    def tearDown(self):
        self.server.shutdown();self.server.motion.close();self.server.video.close();self.server.server_close();self.thread.join();self.tmp.cleanup()
    def request(self,path,data=None,token=None):
        headers={'Content-Type':'application/json'}
        if token:headers['X-Studio-Token']=token
        with urlopen(Request(self.base+path,data=json.dumps(data).encode() if data is not None else None,headers=headers)) as r:return r.read()
    def test_uses_existing_token_and_preserves_empty_source(self):
        state=json.loads(self.request('/api/state'));token=state['token']
        p=json.loads(self.request('/api/create',{'name':'source'},token))
        with self.assertRaises(HTTPError) as e:self.request('/api/local-motion?id='+p['id'],{'action':'generate'})
        self.assertEqual(e.exception.code,403)
        view=json.loads(self.request('/api/local-motion?id='+p['id']))
        self.assertEqual(view['references'],[])
        with self.assertRaises(HTTPError) as e:self.request('/api/local-motion?id='+p['id'],{'action':'generate','prompt':'breathe','approved_reference':True},token)
        self.assertEqual(e.exception.code,400)
        self.assertEqual(len(json.loads(self.request('/api/state'))['projects']),1)
        self.assertIn('12프레임'.encode(),self.request('/local-motion'))

if __name__=='__main__':unittest.main()
