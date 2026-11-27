import tempfile, wave, io
from pathlib import Path
import numpy as np
from audio_engine import process, wav_bytes, read_wav
from game_patch import fsb
from main import Studio
with tempfile.TemporaryDirectory() as tmp:
    root=Path(tmp)
    s=Studio(root)
    x=(0.3*np.sin(2*np.pi*180*np.arange(48000)/48000)).astype(np.float32)
    for i in range(5):
        s.path(i,True).write_bytes(wav_bytes(x,48000))
        s.render(i,{'mode':'radio' if i%2 else 'clean'})
        bank,n=fsb(s.path(i))
        assert bank[:4]==b'FSB5' and n==24000
        with wave.open(str(s.path(i)),'rb') as f:
            assert (f.getnchannels(),f.getsampwidth(),f.getframerate())==(1,2,24000)
    assert all(t['ready'] for t in s.state()['takes'])
print('Five WAV/FSB smoke tests passed; microphone and WebView2 require interactive Windows testing.')
