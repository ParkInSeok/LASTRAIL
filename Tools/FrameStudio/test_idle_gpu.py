import unittest
from idle_gpu import IdleRelease


class IdleReleaseTest(unittest.TestCase):
    def test_idle_busy_pending_and_repeat(self):
        now = [0]
        queue = {'queue_running': [], 'queue_pending': []}
        calls = []
        def call(path, data=None):
            calls.append((path, data))
            return queue if path == '/queue' else None
        guard = IdleRelease(call, lambda: now[0])
        self.assertFalse(guard.tick())
        now[0] = 29
        self.assertFalse(guard.tick())
        queue['queue_running'] = ['existing-job']
        now[0] = 40
        self.assertFalse(guard.tick())
        queue['queue_running'] = []
        queue['queue_pending'] = ['next-job']
        self.assertFalse(guard.tick())
        queue['queue_pending'] = []
        self.assertFalse(guard.tick())
        now[0] = 70
        self.assertTrue(guard.tick())
        self.assertFalse(guard.tick())
        self.assertEqual([x for x in calls if x[0] == '/free'],
                         [('/free', {'unload_models': True, 'free_memory': True})])
        queue.clear()
        with self.assertRaises(ValueError):
            guard.tick()


if __name__ == '__main__':
    unittest.main()
