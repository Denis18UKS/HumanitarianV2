import io, wave
import numpy as np
from scipy import signal
from math import gcd
RATE=24000

def read_wav(source):
    with wave.open(source if hasattr(source,'read') else str(source),'rb') as w:
        ch, width, rate, n = w.getnchannels(),w.getsampwidth(),w.getframerate(),w.getnframes()
        if w.getcomptype()!='NONE' or ch<1 or n/rate>120: raise ValueError('Нужен PCM WAV длительностью до 120 секунд.')
        b=w.readframes(n)
    if width==1: x=(np.frombuffer(b,np.uint8).astype(np.float32)-128)/128
    elif width==2: x=np.frombuffer(b,'<i2').astype(np.float32)/32768
    elif width==3:
        a=np.frombuffer(b,np.uint8).reshape(-1,3).astype(np.int32); z=a[:,0]|a[:,1]<<8|a[:,2]<<16; x=((z^0x800000)-0x800000).astype(np.float32)/8388608
    elif width==4: x=np.frombuffer(b,'<i4').astype(np.float32)/2147483648
    else: raise ValueError('Поддерживается PCM 8/16/24/32 бит.')
    return x.reshape(-1,ch).mean(axis=1), rate

def resample(x,rate):
    g=gcd(rate,RATE)
    return signal.resample_poly(x,RATE//g,rate//g).astype(np.float32) if rate!=RATE else x.astype(np.float32)

def wav_bytes(x,rate=RATE):
    out=io.BytesIO()
    with wave.open(out,'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(np.round(np.clip(x,-1,32767/32768)*32768).astype('<i2').tobytes())
    return out.getvalue()

def process(x,rate,opts=None):
    opts=opts or {}; x=resample(np.asarray(x,np.float32),rate)
    if not len(x) or not np.isfinite(x).all(): raise ValueError('Пустая или повреждённая запись.')
    if opts.get('trim',True):
        # smoothed envelope, retain 150ms padding
        env=signal.convolve(np.abs(x),np.ones(240)/240,mode='same'); ix=np.flatnonzero(env>max(0.002,float(env.max())*0.035))
        if not len(ix): raise ValueError('Запись слишком тихая. Проверь микрофон.')
        x=x[max(0,ix[0]-3600):min(len(x),ix[-1]+3601)]
    if len(x)<RATE*.25 or len(x)>=RATE*30: raise ValueError('Реплика должна длиться от 0,25 до 30 секунд (не включая 30).')
    peak=float(np.max(np.abs(x)))
    if peak<.002: raise ValueError('Слишком тихая запись.')
    x=x/max(peak,1e-6)*.75
    mode=opts.get('mode','clean'); strength=float(opts.get('strength',.45)); strength=max(0,min(1,strength))
    if mode=='radio':
        filt=signal.sosfilt(signal.butter(3,[280,3400],btype='bandpass',fs=RATE,output='sos'),x)
        filt=np.tanh(filt*2.3)/np.tanh(2.3)
        x=(1-strength)*x+strength*filt
        noise=np.random.default_rng(42).normal(0,0.005*strength,len(x)); x+=signal.sosfilt(signal.butter(2,[400,3800],btype='bandpass',fs=RATE,output='sos'),noise)
    x=x/max(float(np.max(np.abs(x))),1e-6)*.85
    fade=min(240,len(x)//2); x[:fade]*=np.linspace(0,1,fade);x[-fade:]*=np.linspace(1,0,fade)
    return x.astype(np.float32)
