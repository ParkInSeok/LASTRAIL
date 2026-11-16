"""Release ComfyUI caches via its worker; never interrupt or terminate generation."""
import ctypes
import json
import logging
import msvcrt
import sys
import time
from pathlib import Path
from urllib.request import Request, build_opener, ProxyHandler


class IdleRelease:
    def __init__(self, call, clock=time.monotonic):
        self.call, self.clock = call, clock
        self.since = None

    def tick(self):
        queue = self.call('/queue')
        if not isinstance(queue.get('queue_running'), list) or not isinstance(queue.get('queue_pending'), list):
            raise ValueError('Invalid ComfyUI queue response')
        now = self.clock()
        if queue['queue_running'] or queue['queue_pending']:
            self.since = None
            return False
        if self.since is None:
            self.since = now
        if now - self.since < 30:
            return False
        # ComfyUI consumes these flags on its worker, after any raced generation ends.
        self.call('/free', {'unload_models': True, 'free_memory': True})
        self.since = now
        return True


def main(owner_pid):
    root = Path(__file__).resolve().parents[2] / 'LocalTemp'
    root.mkdir(exist_ok=True)
    logging.basicConfig(filename=str(root / 'framestudio-idle-gpu.log'), level=logging.WARNING)
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    kernel.OpenProcess.restype = ctypes.c_void_p
    kernel.WaitForSingleObject.argtypes = [ctypes.c_void_p, ctypes.c_ulong]
    kernel.CloseHandle.argtypes = [ctypes.c_void_p]
    handle = kernel.OpenProcess(0x00100000, False, owner_pid)
    if not handle:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        with (root / 'framestudio-idle-gpu.lock').open('a+b') as lock:
            lock.seek(0)
            try:
                msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
            except OSError:
                return  # Only one guard owns this fixed localhost endpoint.
            http = build_opener(ProxyHandler({}))
            def call(path, data=None):
                body = None if data is None else json.dumps(data).encode()
                with http.open(Request('http://127.0.0.1:8188' + path, data=body,
                                       headers={'Content-Type': 'application/json'}), timeout=5) as response:
                    raw = response.read()
                    return json.loads(raw) if raw else None
            guard = IdleRelease(call)
            failed = False
            while kernel.WaitForSingleObject(handle, 0) == 258:
                try:
                    guard.tick()
                    failed = False
                except Exception:
                    guard.since = None
                    if not failed:
                        logging.exception('Idle release unavailable; no process interrupted')
                    failed = True
                time.sleep(5)
    finally:
        kernel.CloseHandle(handle)


if __name__ == '__main__':
    main(int(sys.argv[1]))
