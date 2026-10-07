"""Platform companion: Linux session mute; macOS detection/guidance; Windows launcher.
No backend reports universal hardware cutoff. Python 3.10+, standard library only.
"""
import argparse
import contextlib
import glob
import json
import os
import platform
import shutil
import subprocess
import sys
import tempfile
import time
import unittest
from abc import ABC, abstractmethod
from pathlib import Path


class Backend(ABC):
    """Contract: preview is read-only; apply requires reviewed snapshot; restore uses originals."""
    @abstractmethod
    def preview(self): ...
    @abstractmethod
    def apply(self, reviewed): ...
    @abstractmethod
    def restore(self): ...


def command(args):
    return subprocess.run(args, capture_output=True, text=True, check=True, timeout=20).stdout


class JournalStore:
    def __init__(self, root):
        self.root = Path(root)
        self.path = self.root / 'linux-session-recovery.json'

    @contextlib.contextmanager
    def lock(self):
        self.root.mkdir(parents=True, exist_ok=True, mode=0o700)
        import fcntl  # Unix kernel lock is released on crash; no stale PID file.
        with (self.root / 'action.lock').open('a') as stream:
            os.chmod(stream.name, 0o600)
            fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
            try:
                yield
            finally:
                fcntl.flock(stream, fcntl.LOCK_UN)

    def load(self):
        state = json.loads(self.path.read_text())
        if state.get('schema') != 1 or state.get('backend') != 'linux-pulse-session':
            raise RuntimeError('Unsupported recovery schema/backend; preserved for review')
        return state

    def save(self, state):
        self.root.mkdir(parents=True, exist_ok=True, mode=0o700)
        fd, name = tempfile.mkstemp(prefix='journal-', dir=self.root)
        try:
            with os.fdopen(fd, 'w') as stream:
                os.chmod(name, 0o600)
                json.dump(state, stream, indent=2)
                stream.flush()
                os.fsync(stream.fileno())
            os.replace(name, self.path)
            if os.name == 'posix':
                directory = os.open(self.root, os.O_RDONLY)
                try:
                    os.fsync(directory)
                finally:
                    os.close(directory)
        finally:
            if os.path.exists(name):
                os.unlink(name)

    def archive(self):
        os.replace(self.path, self.root / ('audit-' + str(time.time_ns()) + '.json'))


class LinuxBackend(Backend):
    level = 'SESSION MUTE ONLY — not hardware/global cutoff; cameras unsupported'
    def __init__(self, runner=command, store=None):
        self.run = runner
        base = os.environ.get('XDG_STATE_HOME', str(Path.home() / '.local/state'))
        self.store = store or JournalStore(Path(base) / 'privacy-switch')

    def sources(self):
        raw = json.loads(self.run(['pactl', '--format=json', 'list', 'sources']))
        result = {}
        for source in raw:
            name = source['name']
            monitor = source.get('monitor_of_sink')
            if name.endswith('.monitor') or monitor not in (None, -1, 4294967295, '4294967295'):
                continue  # speaker loopback is never a microphone target
            if not isinstance(source.get('mute'), bool):
                raise RuntimeError('Source mute value is not a supported JSON boolean')
            props = source.get('properties', {})
            identity = {k: props.get(k) for k in
                        ('device.serial', 'device.string', 'device.bus_path', 'device.api')}
            result[name] = {'id': name, 'muted': source['mute'], 'identity': identity,
                            'driver': source.get('driver'), 'description': source.get('description')}
        return result

    def preview(self):
        sources = self.sources()
        return {'platform': platform.platform(), 'backend': 'linux-pulse-session',
                'enforcement': self.level, 'sources': list(sources.values()),
                'camera_devices_read_only': glob.glob('/dev/video*'),
                'limits': ['Direct ALSA, other users/servers and native PipeWire paths may bypass mute.',
                           'Existing and new streams require live validation; no audio is recorded here.',
                           'Hotplug is not continuously blocked; no camera access controls are changed.'],
                'recovery': self.store.load() if self.store.path.exists() else None}

    def _matched(self, entry):
        current = self.sources().get(entry['id'])
        if not current or current['identity'] != entry['identity']:
            raise RuntimeError('Source absent or identity changed; no write')
        return current

    def apply(self, reviewed):
        with self.store.lock():
            if self.store.path.exists():
                raise RuntimeError('Pending recovery exists; restore it before reapplying')
            live = self.sources()
            expected = {s['id']: s for s in reviewed['sources']}
            if live != expected:
                raise RuntimeError('Sources changed since preview; review again')
            if not live:
                raise RuntimeError('No capture sources; no mutation')
            state = {'schema': 1, 'backend': 'linux-pulse-session', 'created': time.time(),
                     'enforcement': self.level, 'entries': []}
            for source in live.values():
                state['entries'].append(dict(source, original=source['muted'], pending=False,
                                             result='Already muted; unchanged' if source['muted'] else 'Previewed'))
            self.store.save(state)
            for entry in state['entries']:
                if entry['original']:
                    continue
                try:
                    if self._matched(entry)['muted'] != entry['original']:
                        raise RuntimeError('Mute changed externally; skipped')
                    entry['pending'] = True
                    self.store.save(state)
                    self.run(['pactl', 'set-source-mute', entry['id'], '1'])
                    if not self._matched(entry)['muted']:
                        raise RuntimeError('Mute readback failed')
                    entry['result'] = 'Session mute readback true; effective capture UNTESTED'
                except Exception as error:
                    entry['result'] = 'Failed: ' + str(error)
                self.store.save(state)
            return state

    def restore(self):
        with self.store.lock():
            state = self.store.load()
            for entry in reversed(state['entries']):
                if not entry['pending']:
                    continue
                try:
                    current = self._matched(entry)
                    if current['muted'] != entry['original']:
                        self.run(['pactl', 'set-source-mute', entry['id'], '1' if entry['original'] else '0'])
                    if self._matched(entry)['muted'] != entry['original']:
                        raise RuntimeError('Original mute readback failed')
                    entry['pending'] = False
                    entry['result'] = 'Original session mute restored'
                except Exception as error:
                    entry['result'] = 'Restore failed: ' + str(error)
                self.store.save(state)
            if not any(e['pending'] for e in state['entries']):
                self.store.archive()
            return state


class MacBackend(Backend):
    def preview(self):
        inventory = json.loads(command(['system_profiler', 'SPAudioDataType', 'SPCameraDataType', '-json']))
        return {'platform': platform.platform(), 'backend': 'macos-guidance',
                'enforcement': 'UNSUPPORTED global cutoff — detection/permission guidance only',
                'inventory': inventory, 'guidance': 'System Settings > Privacy & Security > Microphone / Camera.',
                'limits': ['Per-app permission is not global hardware cutoff.',
                           'No driver unload, SIP changes or permission database edits are performed.']}
    def apply(self, reviewed):
        raise RuntimeError('macOS global mic/camera apply is unsupported; no changes made')
    def restore(self):
        raise RuntimeError('No macOS mutations or recovery are implemented')


class WindowsBackend(Backend):
    def preview(self):
        return {'platform': platform.platform(), 'backend': 'windows-native-launcher',
                'enforcement': 'Use bundled PrivacySwitch.exe for full driver/endpoint audit and review',
                'executable': str(Path(__file__).with_name('PrivacySwitch.exe')),
                'limits': ['Python companion does not duplicate the Windows journal/control paths.']}
    def apply(self, reviewed):
        raise RuntimeError('Launch PrivacySwitch.exe and use its reviewed toggle; no Python mutation')
    def restore(self):
        raise RuntimeError('Use Restore in PrivacySwitch.exe for Windows original-state recovery')


def select_backend():
    name = platform.system()
    if name == 'Windows': return WindowsBackend()
    if name == 'Darwin': return MacBackend()
    if name == 'Linux':
        if not shutil.which('pactl'):
            raise RuntimeError('pactl with JSON source support is required; ALSA/wpctl-only mutation unsupported')
        return LinuxBackend()
    raise RuntimeError('Unsupported OS: ' + name)


def gui(backend):
    import tkinter as tk
    from tkinter import messagebox
    root = tk.Tk(); root.title('Microphone / Camera — platform capabilities')
    text = tk.Text(root, width=100, height=30); text.pack(fill='both', expand=True)
    reviewed = None
    def refresh():
        nonlocal reviewed
        try:
            reviewed = backend.preview()
            text.delete('1.0', 'end'); text.insert('end', json.dumps(reviewed, indent=2))
        except Exception as e:
            reviewed = None; messagebox.showerror('Detection failed', str(e))
    def action():
        if isinstance(backend, WindowsBackend):
            subprocess.Popen([str(Path(__file__).with_name('PrivacySwitch.exe'))]); return
        if not isinstance(backend, LinuxBackend):
            messagebox.showinfo('Unsupported', 'macOS permission guidance only; no device changes available.'); return
        restore = backend.store.path.exists()
        if not restore and reviewed is None: refresh()
        if not restore and reviewed is None: return
        warning = 'Restore saved source mute originals?' if restore else 'Apply SESSION microphone mute only? Cameras stay available. Direct audio paths may bypass this mute. Voice input may stop. No hardware/global cutoff is promised.'
        if not messagebox.askokcancel('Review limited action', warning): return
        try:
            result = backend.restore() if restore else backend.apply(reviewed)
            messagebox.showinfo('Partial enforcement / audit', json.dumps(result, indent=2))
        except Exception as e: messagebox.showerror('Action failed', str(e))
        refresh()
    tk.Button(root, text='Refresh read-only capability report', command=refresh).pack()
    label = 'Open Windows switch' if isinstance(backend, WindowsBackend) else 'Review session mute / restore' if isinstance(backend, LinuxBackend) else 'Show unsupported status'
    tk.Button(root, text=label, command=action).pack()
    refresh(); root.mainloop()


def self_test():
    class MemoryStore(JournalStore):
        @contextlib.contextmanager
        def lock(self): yield  # no live Unix lock or platform writes in Windows simulations
    class Tests(unittest.TestCase):
        def test_linux_restore_and_guards(self):
            states = {'mic': False, 'already-muted': True, 'speaker.monitor': False}
            writes = []
            def fake(args):
                if 'list' in args:
                    return json.dumps([{'name': name, 'mute': muted, 'monitor_of_sink': None,
                                        'properties': {'device.serial': name}, 'driver': 'fake'}
                                       for name, muted in states.items()])
                writes.append(args)
                states[args[2]] = args[3] == '1'
                return ''
            with tempfile.TemporaryDirectory(dir='work') as folder:
                backend = LinuxBackend(fake, MemoryStore(folder))
                reviewed = backend.preview(); backend.apply(reviewed)
                self.assertTrue(states['mic']); self.assertFalse(states['speaker.monitor'])
                self.assertEqual(len(writes), 1)
                with self.assertRaises(RuntimeError): backend.apply(reviewed)
                backend.restore(); self.assertFalse(states['mic']); self.assertTrue(states['already-muted'])
                self.assertFalse(backend.store.path.exists())
                reviewed = backend.preview(); states['new-mic'] = False
                with self.assertRaises(RuntimeError): backend.apply(reviewed)
                reviewed = backend.preview(); backend.apply(reviewed)
                states.pop('mic'); backend.restore()
                self.assertTrue(backend.store.path.exists())
                self.assertTrue(any(e['pending'] for e in backend.store.load()['entries']))
        def test_guidance_never_mutates(self):
            with self.assertRaises(RuntimeError): MacBackend().apply({})
            with self.assertRaises(RuntimeError): MacBackend().restore()
            with self.assertRaises(RuntimeError): WindowsBackend().apply({})
        def test_failed_readback_keeps_recovery(self):
            states = {'mic': False}
            def fake(args):
                if 'list' in args:
                    return json.dumps([{'name': 'mic', 'mute': states['mic'],
                                        'monitor_of_sink': None, 'properties': {'device.serial': 'abc'}}])
                return ''  # silently ineffective command must not become a success
            with tempfile.TemporaryDirectory(dir='work') as folder:
                backend = LinuxBackend(fake, MemoryStore(folder))
                result = backend.apply(backend.preview())
                self.assertTrue(result['entries'][0]['pending'])
                self.assertIn('Failed:', result['entries'][0]['result'])
                # Recreate backend, simulating loss of process memory after persisted intent.
                restored = LinuxBackend(fake, MemoryStore(folder))
                restored.restore()
                self.assertFalse(restored.store.path.exists())
    Path('work').mkdir(exist_ok=True)
    return 0 if unittest.TextTestRunner().run(unittest.defaultTestLoader.loadTestsFromTestCase(Tests)).wasSuccessful() else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--self-test', action='store_true')
    parser.add_argument('--gui', action='store_true')
    parser.add_argument('--apply-reviewed-session-mute', action='store_true',
                        help='Explicit Linux-only limited session-mute authorization; no cameras/global cutoff')
    parser.add_argument('--restore-reviewed', action='store_true')
    args = parser.parse_args()
    if args.self_test: return self_test()
    if args.apply_reviewed_session_mute and args.restore_reviewed:
        parser.error('Choose apply or restore, not both')
    backend = select_backend()
    if args.gui: gui(backend); return 0
    if args.restore_reviewed: result = backend.restore()
    else:
        result = backend.preview()
        if args.apply_reviewed_session_mute: result = backend.apply(result)
    print(json.dumps(result, indent=2))
    return 4 if args.apply_reviewed_session_mute or (args.restore_reviewed and any(e['pending'] for e in result.get('entries', []))) else 0  # apply is intentionally partial enforcement


if __name__ == '__main__':
    try: sys.exit(main())
    except Exception as error:
        print('Unsupported/failed: ' + str(error), file=sys.stderr); sys.exit(1)

