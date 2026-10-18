"""Real Windows pseudoconsole tests. Never attaches to an existing game/window.

Build terminal_probe.cpp with the same compiler/options as LumenRogue.cpp.
The child uses the game's real creator, shop input loop and terminal renderer;
it reads back native console cells to detect bottom-right scrolling or stale size.
"""
import ctypes as c
from ctypes import wintypes as w
from pathlib import Path
import threading
import time

root = Path(__file__).resolve().parent
k = c.WinDLL('kernel32', use_last_error=True)

class Coord(c.Structure):
    _fields_ = [('X', c.c_short), ('Y', c.c_short)]

class Startup(c.Structure):
    _fields_ = [('cb', w.DWORD), ('reserved', w.LPWSTR), ('desktop', w.LPWSTR), ('title', w.LPWSTR),
                ('x', w.DWORD), ('y', w.DWORD), ('width', w.DWORD), ('height', w.DWORD),
                ('charsX', w.DWORD), ('charsY', w.DWORD), ('fill', w.DWORD), ('flags', w.DWORD),
                ('show', w.WORD), ('reservedBytes', w.WORD), ('reservedPtr', c.c_void_p),
                ('stdin', w.HANDLE), ('stdout', w.HANDLE), ('stderr', w.HANDLE)]

class StartupEx(c.Structure):
    _fields_ = [('base', Startup), ('attributes', c.c_void_p)]

class Process(c.Structure):
    _fields_ = [('process', w.HANDLE), ('thread', w.HANDLE), ('pid', w.DWORD), ('tid', w.DWORD)]

def api(name, result, args):
    fn = getattr(k, name)
    fn.restype, fn.argtypes = result, args
    return fn

pipe = api('CreatePipe', w.BOOL, [c.POINTER(w.HANDLE), c.POINTER(w.HANDLE), c.c_void_p, w.DWORD])
pseudo = api('CreatePseudoConsole', c.c_long, [Coord, w.HANDLE, w.HANDLE, w.DWORD, c.POINTER(w.HANDLE)])
resize = api('ResizePseudoConsole', c.c_long, [w.HANDLE, Coord])
close_pseudo = api('ClosePseudoConsole', None, [w.HANDLE])
close = api('CloseHandle', w.BOOL, [w.HANDLE])
init = api('InitializeProcThreadAttributeList', w.BOOL, [c.c_void_p, w.DWORD, w.DWORD, c.POINTER(c.c_size_t)])
attribute = api('UpdateProcThreadAttribute', w.BOOL, [c.c_void_p, w.DWORD, c.c_size_t, c.c_void_p, c.c_size_t, c.c_void_p, c.c_void_p])
delete = api('DeleteProcThreadAttributeList', None, [c.c_void_p])
create = api('CreateProcessW', w.BOOL, [w.LPCWSTR, w.LPWSTR, c.c_void_p, c.c_void_p, w.BOOL, w.DWORD, c.c_void_p, w.LPCWSTR, c.POINTER(StartupEx), c.POINTER(Process)])
read = api('ReadFile', w.BOOL, [w.HANDLE, c.c_void_p, w.DWORD, c.POINTER(w.DWORD), c.c_void_p])
write = api('WriteFile', w.BOOL, [w.HANDLE, c.c_void_p, w.DWORD, c.POINTER(w.DWORD), c.c_void_p])
wait = api('WaitForSingleObject', w.DWORD, [w.HANDLE, w.DWORD])
terminate = api('TerminateProcess', w.BOOL, [w.HANDLE, w.UINT])
exit_code = api('GetExitCodeProcess', w.BOOL, [w.HANDLE, c.POINTER(w.DWORD)])

def check(ok):
    if not ok:
        raise c.WinError(c.get_last_error())

def exercise(start_size, resize_creator):
    report = root / 'qa-shops' / ('native-resize-' + str(start_size[0]) + '-' + str(time.time_ns()) + '.txt')
    report.parent.mkdir(exist_ok=True)
    # Only this test-owned report is replaced by the new probe process.
    input_read, input_write, output_read, output_write, pc = [w.HANDLE() for _ in range(5)]
    check(pipe(c.byref(input_read), c.byref(input_write), None, 0))
    check(pipe(c.byref(output_read), c.byref(output_write), None, 0))
    assert pseudo(Coord(*start_size), input_read, output_write, 0, c.byref(pc)) == 0
    size = c.c_size_t()
    init(None, 1, 0, c.byref(size))
    storage = c.create_string_buffer(size.value)
    check(init(storage, 1, 0, c.byref(size)))
    check(attribute(storage, 0, 0x00020016, pc, c.sizeof(w.HANDLE), None, None))
    startup = StartupEx()
    startup.base.cb = c.sizeof(startup)
    startup.base.flags = 1  # STARTF_USESHOWWINDOW / SW_HIDE; ConPTY has no visible window.
    startup.attributes = c.cast(storage, c.c_void_p)
    process = Process()
    command = c.create_unicode_buffer(f'"{root / "terminal_probe.exe"}" "{report}"')
    check(create(None, command, None, None, False, 0x00080000, None, str(root), c.byref(startup), c.byref(process)))
    close(input_read); close(output_write)
    observed = bytearray()

    def drain():
        buffer, count = c.create_string_buffer(65536), w.DWORD()
        while read(output_read, buffer, len(buffer), c.byref(count), None) and count.value:
            chunk = buffer.raw[:count.value]
            observed.extend(chunk)
            # ConPTY asks the host for its cursor before its first VT output.
            if b'\x1b[6n' in chunk:
                written = w.DWORD()
                response = b'\x1b[1;1R'
                write(input_write, response, len(response), c.byref(written), None)

    reader = threading.Thread(target=drain, daemon=True)
    reader.start()

    def send(key):
        count = w.DWORD()
        check(write(input_write, key, len(key), c.byref(count), None))
        time.sleep(0.2)

    def until(marker):
        deadline = time.monotonic() + 12
        while time.monotonic() < deadline:
            if report.exists():
                text = report.read_text()
                if marker in text:
                    return text
            if wait(process.process, 0) == 0:
                code = w.DWORD()
                exit_code(process.process, c.byref(code))
                raise AssertionError(f'Probe exited {code.value}; report={report.read_text() if report.exists() else "missing"}')
            time.sleep(0.05)
        raise AssertionError(f'{marker} not reached; output bytes={len(observed)}, tail={bytes(observed[-120:])!r}; report={report.read_text() if report.exists() else "missing"}')

    try:
        time.sleep(0.7)
        send(b'\r')  # splash -> ancestry
        if resize_creator:
            assert resize(pc, Coord(220, 85)) == 0
            time.sleep(0.2)
        send(b'\r'); send(b'\r'); send(b'\r')  # ancestry, calling, confirmation
        text = until('town ')
        assert 'town 220 85 1 1 1' in text, text
        assert resize(pc, Coord(80, 25)) == 0
        send(b'r')
        text = until('small ')
        assert 'small 80 25 1 1 1' in text, text
        assert resize(pc, Coord(220, 85)) == 0
        send(b'r')
        text = until('large ')
        assert 'large 220 85 1 1 1' in text, text
        send(b'\r'); send(b'\t'); send(b'\r'); send(b'\x1b')
        text = until('restored ')
        assert 'shop_closed 220 85 1 1 1' in text and 'trades 180 0' in text and 'restored 1' in text, text
        assert wait(process.process, 4000) == 0, 'probe did not exit'
        print(f'Native startup {start_size}, resize during creator={resize_creator}: PASS\n{text}', flush=True)
    finally:
        if wait(process.process, 0) != 0:
            terminate(process.process, 1)  # Only the child created by this test.
            wait(process.process, 3000)
        close_pseudo(pc)
        reader.join(timeout=3)
        close(input_write); close(output_read); close(process.thread); close(process.process)
        delete(storage)

exercise((220, 85), False)
exercise((80, 25), True)
