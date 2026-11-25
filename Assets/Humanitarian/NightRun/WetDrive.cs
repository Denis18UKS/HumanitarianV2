using UnityEngine;
using System.Collections.Generic;

public sealed class WetDrive : MonoBehaviour {
    readonly List<Material> wet=new List<Material>();
    readonly List<Material> asphalt=new List<Material>();
    Transform[] blades;Transform glass;
    Texture2D mask;Color32[] drops;
    const int W=192,H=80;
    float phase,clock,previousAngle,refill;
    public bool wipers=true;
    static Renderer Overlay(MeshFilter mesh,Material material){
        var g=new GameObject("Вода");g.transform.SetParent(mesh.transform,false);g.layer=mesh.gameObject.layer;
        g.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;var rend=g.AddComponent<MeshRenderer>();rend.sharedMaterial=material;
        rend.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return rend;
    }
    public void Fit(Camaro car,List<GameObject> roads){
        blades=new[]{car.parts["Wiper_L"],car.parts["Wiper_R"]};glass=car.parts["Curved_windscreen"];
        mask=new Texture2D(W,H,TextureFormat.RGBA32,false,true);mask.wrapMode=TextureWrapMode.Clamp;drops=new Color32[W*H];
        var windshield=CockpitInstruments.Effect(1);windshield.SetTexture("_Mask",mask);
        var paint=CockpitInstruments.Effect(0);wet.Add(windshield);wet.Add(paint);
        foreach(var m in car.GetComponentsInChildren<MeshFilter>()){
            if(m.transform.parent==glass)Overlay(m,windshield);
            else if(m.name=="Paint"||m.name=="SideGlass"||m.name=="Glass")Overlay(m,paint);
        }
        var puddles=CockpitInstruments.Effect(2);wet.Add(puddles);
        foreach(var g in roads){Overlay(g.GetComponent<MeshFilter>(),puddles);asphalt.Add(g.GetComponent<Renderer>().sharedMaterial);}
        mask.SetPixels32(drops);mask.Apply();
    }
    void Wipe(float angle){
        // Sample the two physical blades on the windshield plane. The mask and blade pose share the same transform.
        for(int i=0;i<2;i++){
            float sign=i==0?1:-1;var blade=blades[i];
            blade.localRotation=Quaternion.AngleAxis(-sign*angle,new Vector3(0,.84f,.54f));
            for(int k=0;k<=64;k++){
                float f=k/64f;var rest=new Vector3(-sign*Mathf.Lerp(.039f,.431f,f),Mathf.Lerp(.026f,.060f,f),Mathf.Lerp(-.018f,-.087f,f));
                var v=glass.InverseTransformPoint(blade.TransformPoint(rest));int x=Mathf.RoundToInt((v.x+.795f)/1.59f*(W-1)),y=Mathf.RoundToInt((v.y-.851f)/.407f*(H-1));
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(x+dx>=0&&x+dx<W&&y+dy>=0&&y+dy<H)drops[(y+dy)*W+x+dx]=new Color32(0,0,0,255);
            }
        }
    }
    public void Tick(int mode,bool improved){
        float rain=mode==0?0:mode==1?.42f:1;
        foreach(var m in wet)m.SetFloat("_Wet",rain);
        foreach(var m in asphalt){m.SetFloat("_Smoothness",.22f+.69f*rain);m.SetColor("_BaseColor",Color.Lerp(new Color(.22f,.23f,.25f),new Color(.10f,.13f,.16f),rain));}
        float dt=Time.deltaTime;clock+=dt;if(clock<.045f)return;dt=clock;clock=0;
        refill+=dt*(mode>=2?92:mode==1?43:-85);int amount=(int)refill;refill-=amount;
        for(int i=0;i<drops.Length;i++){byte c=(byte)Mathf.Clamp(drops[i].r+amount,0,255);drops[i]=new Color32(c,c,c,255);}
        if(wipers&&(mode>0||phase>0)){phase+=dt*(improved?1.45f:1)*(mode>=2?1.15f:.65f);if(mode==0&&phase>=1)phase=0;else phase%=1;}
        float angle=wipers?(1-Mathf.Cos(phase*Mathf.PI*2))*37:Mathf.MoveTowards(previousAngle,0,dt*145);
        if(!wipers)phase=0;
        int n=Mathf.CeilToInt(Mathf.Abs(angle-previousAngle)/2);
        for(int i=1;i<=n;i++)Wipe(Mathf.Lerp(previousAngle,angle,i/(float)n));previousAngle=angle;
        mask.SetPixels32(drops);mask.Apply(false);
    }
}
