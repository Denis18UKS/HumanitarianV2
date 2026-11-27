import base64, json, os, sys, threading, time, traceback, zipfile, hashlib
from pathlib import Path
from audio_engine import read_wav, wav_bytes, process
import numpy as np
ROOT=Path(getattr(sys,'_MEIPASS',Path(__file__).resolve().parent))
HOME=Path(os.environ.get('LOCALAPPDATA',str(Path.home())))/'ZunserzGames'/'RadioStudio'
LINES=json.loads((ROOT/'lines.json').read_text(encoding='utf-8'))
class Studio:
    def __init__(self,home=HOME):
        self.home=Path(home);self.raw=self.home/'Takes';self.out=self.home/'Audio';self.raw.mkdir(parents=True,exist_ok=True);self.out.mkdir(exist_ok=True);self.lock=threading.RLock();self.stream=None;self.window=None;self.chunks=[];self.frames=0;self.rate=48000;self.record_id=None
    def idx(self,i):
        i=int(i)
        if not 0<=i<len(LINES):raise ValueError('Неверный номер реплики')
        return i
    def path(self,i,raw=False):return (self.raw if raw else self.out)/f'ConvoyDispatch{self.idx(i)}.wav'
    def state(self):
        return {'lines':LINES,'folder':str(self.home),'takes':[{'raw':self.path(i,True).exists(),'ready':self.path(i).exists(),'seconds':round(len(read_wav(self.path(i))[0])/24000,2) if self.path(i).exists() else 0} for i in range(len(LINES))]}
    def devices(self):
        import sounddevice as sd
        return [{'id':i,'name':d['name'],'rate':int(d['default_samplerate'])} for i,d in enumerate(sd.query_devices()) if d['max_input_channels']>0]
    def start_record(self,i,device=None):
        import sounddevice as sd
        with self.lock:
            if self.stream is not None:raise RuntimeError('Сначала останови запись.')
            self.record_id=self.idx(i); self.chunks=[];self.frames=0;self.peak=0.;self.errors=[]
            dev=None if device in (None,'') else int(device); self.rate=int(sd.query_devices(dev,'input')['default_samplerate'])
            def callback(indata,frames,t,status):
                if status:self.errors.append(str(status))
                if self.frames<self.rate*60:
                    block=indata[:,0].copy();self.chunks.append(block);self.frames+=len(block);self.peak=float(np.max(np.abs(block)))
            s=sd.InputStream(device=dev,channels=1,samplerate=self.rate,dtype='float32',callback=callback)
            try:s.start()
            except Exception:s.close();raise
            self.stream=s; return {'ok':True}
    def meter(self):return {'level':getattr(self,'peak',0.),'seconds':self.frames/self.rate,'active':self.stream is not None}
    def stop_record(self,opts):
        with self.lock:
            if self.stream is None:raise RuntimeError('Нет записи.')
            s=self.stream;self.stream=None
            try:s.stop()
            finally:s.close()
            if not self.chunks:raise ValueError('Микрофон не записал звук.')
            x=np.concatenate(self.chunks);i=self.record_id;self.path(i,True).write_bytes(wav_bytes(x,self.rate)); self.path(i).unlink(missing_ok=True); self.chunks=[]
            result=self.render(i,opts);result['warning']='; '.join(set(self.errors));return result
    def render(self,i,opts):
        with self.lock:
            x,r=read_wav(self.path(i,True)); y=process(x,r,opts);self.path(i).write_bytes(wav_bytes(y));return self.state()
    def import_wav(self,i,opts):
        import webview
        selected=self.window.create_file_dialog((webview.FileDialog.OPEN if hasattr(webview, "FileDialog") else webview.OPEN_DIALOG),allow_multiple=False,file_types=('WAV (*.wav)',))
        if not selected:return None
        x,r=read_wav(selected[0]);y=process(x,r,opts);self.path(i,True).write_bytes(wav_bytes(x,r));self.path(i).write_bytes(wav_bytes(y));return self.state()
    def preview(self,i,raw=False):
        return 'data:audio/wav;base64,'+base64.b64encode(self.path(i,raw).read_bytes()).decode('ascii')
    def delete(self,i):
        with self.lock:
            self.path(i).unlink(missing_ok=True);self.path(i,True).unlink(missing_ok=True);return self.state()
    def export_zip(self,actor):
        import webview
        if not all(self.path(i).exists() for i in range(5)):raise ValueError('Сначала запиши все пять реплик.')
        selected=self.window.create_file_dialog((webview.FileDialog.SAVE if hasattr(webview, "FileDialog") else webview.SAVE_DIALOG),save_filename='Humanitarian_Radio_Actor.zip',file_types=('ZIP (*.zip)',))
        if not selected:return None
        target=Path(selected[0] if isinstance(selected,(tuple,list)) else selected)
        if target.suffix.lower()!='.zip':target=target.with_suffix('.zip')
        files={self.path(i).name:self.path(i).read_bytes() for i in range(5)}
        manifest={'project':'Humanitarian 0.7','performer':str(actor).strip(),'source':'human recording / imported WAV; provenance not independently verified','lines':LINES,'format':'mono PCM16 24000 Hz','sha256':{n:hashlib.sha256(b).hexdigest() for n,b in files.items()},'permission':'Not a signed license. Obtain performer written consent separately.'}
        with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as z:
            for n,b in files.items():z.writestr('Audio/'+n,b)
            z.writestr('provenance.json',json.dumps(manifest,ensure_ascii=False,indent=2));z.write(ROOT/'game_patch.py','game_patch.py');z.write(ROOT/'Install_RecordedVoice.cmd','Install_RecordedVoice.cmd');z.write(ROOT/'PERMISSION_RU.txt','PERMISSION_RU.txt')
        return str(target)
    def open_folder(self):
        if sys.platform=='win32':os.startfile(str(self.home))
    def close(self):
        if self.stream:
            try:self.stream.abort();self.stream.close()
            finally:self.stream=None

def main():
    import webview
    api=Studio();w=webview.create_window('Zunserz Games | Radio Studio',str(ROOT/'web'/'index.html'),js_api=api,width=1100,height=730,min_size=(850,620));api.window=w;w.events.closed+=api.close
    webview.start(gui='edgechromium' if sys.platform=='win32' else None,debug=False)
if __name__=='__main__':
    try:main()
    except Exception:
        HOME.mkdir(parents=True,exist_ok=True);(HOME/'error.log').write_text(traceback.format_exc(),encoding='utf-8');raise
