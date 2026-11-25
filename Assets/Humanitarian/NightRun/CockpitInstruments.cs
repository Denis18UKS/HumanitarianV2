using UnityEngine;
using System.Collections.Generic;

public sealed class CockpitInstruments : MonoBehaviour {
    readonly List<Camera> mirrors=new List<Camera>();
    readonly List<RenderTexture> feeds=new List<RenderTexture>();
    Transform fuelNeedle,speedNeedle,rpmNeedle;
    Texture2D map;Color32[] pixels;
    const int W=192,H=96;
    float refresh;
    static readonly Color32 asphalt=new Color32(17,34,35,255),road=new Color32(97,131,119,255),goal=new Color32(255,186,61,255);
    public static Material Effect(int mode){
        var shader=Resources.Load<Shader>("NightSurface");
        if(!shader)throw new System.InvalidOperationException("Не импортирован NightSurface");
        var material=new Material(shader);material.SetFloat("_Mode",mode);return material;
    }
    static void Display(Transform parent,Vector3 p,Vector2 size,Texture tex,bool mirror){
        var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=mirror?"Зеркальная поверхность":"Карта в панели";
        g.GetComponent<Collider>().enabled=false;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=new Vector3(size.x,size.y,1);
        var material=Effect(5);
        if(mirror){var m=g.GetComponent<MeshFilter>().mesh;var uv=m.uv;for(int i=0;i<uv.Length;i++)uv[i].x=1-uv[i].x;m.uv=uv;}
        material.mainTexture=tex;g.GetComponent<Renderer>().sharedMaterial=material;
    }
    public void Fit(Camaro car){
        // Screen positions are measured on the rear-facing side of each original display.
        Mirror(car.transform,new Vector3(0,1.128f,.098f),new Vector2(.20f,.046f),180);
        Mirror(car.parts["Door_L_hinge"],car.transform.TransformPoint(new Vector3(-.953f,.907f,.425f)),new Vector2(.146f,.083f),200,true);
        Mirror(car.parts["Door_R_hinge"],car.transform.TransformPoint(new Vector3(.953f,.907f,.425f)),new Vector2(.146f,.083f),160,true);
        speedNeedle=car.parts["Speed_needle"];rpmNeedle=car.parts["RPM_needle"];
        Display(car.transform,new Vector3(-.212f,.827f,.182f),new Vector2(.091f,.046f),Resources.Load<Texture2D>("FuelDial"),false);
        fuelNeedle=new GameObject("Стрелка топлива").transform;fuelNeedle.SetParent(car.transform,false);
        fuelNeedle.localPosition=new Vector3(-.212f,.814f,.179f);
        Camaro.Block("Указатель топлива",new Vector3(0,.012f,0),new Vector3(.0024f,.024f,.001f),new Color(1,.55f,.2f),fuelNeedle,false);
        map=new Texture2D(W,H,TextureFormat.RGBA32,false);map.filterMode=FilterMode.Point;pixels=new Color32[W*H];
        Display(car.transform,new Vector3(.104f,.865f,.301f),new Vector2(.103f,.052f),map,false);
    }
    void Mirror(Transform parent,Vector3 p,Vector2 size,float yaw,bool world=false){
        var rt=new RenderTexture(320,128,16);rt.Create();feeds.Add(rt);
        if(world)p=parent.InverseTransformPoint(p);
        Display(parent,p,size,rt,true);
        var mount=new GameObject("Обзор назад").transform;mount.SetParent(parent,false);mount.localPosition=p;
        mount.localRotation=Quaternion.Euler(4,yaw,0);var c=mount.gameObject.AddComponent<Camera>();
        c.nearClipPlane=.065f;c.farClipPlane=240;c.fieldOfView=53;c.targetTexture=rt;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.07f,.09f,.13f);
        c.cullingMask=~((1<<29)|(1<<30));c.enabled=false;mirrors.Add(c);
        // The lens looks backward from the glass; the layer mask excludes the body, other screens and wheel rig.
    }
    public void Tick(bool active,Route route,int stage,float speed,float rpm,float fuel){
        speedNeedle.localRotation=Quaternion.Euler(0,0,-Mathf.Clamp01(speed/180f)*254);
        rpmNeedle.localRotation=Quaternion.Euler(0,0,36-Mathf.Clamp01(rpm/6000f)*252);
        fuelNeedle.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(70,-70,Mathf.Clamp01(fuel/100f)));
        foreach(var c in mirrors)c.enabled=active;
        if(!active||Time.time<refresh)return;refresh=Time.time+.16f;
        for(int i=0;i<pixels.Length;i++)pixels[i]=asphalt;
        for(int i=1;i<route.road.Length;i++)Line(Project(route.road[i-1].Pos),Project(route.road[i].Pos),road,1);
        for(int i=1;i<route.road.Length;i++){
            var a=route.road[i-1].Pos;var b=route.road[i].Pos;
            if(b.z>=transform.position.z-5 && a.z<route.stops[stage].z)
                Line(Project(a),Project(b),new Color32(73,184,145,255),1);
        }
        foreach(var pump in new[]{new Vector3(70,0,234),new Vector3(9,0,473)}){
            var xy=Project(pump);Dot(xy.x,xy.y,2,new Color32(94,192,224,255));
        }
        var target=Project(route.stops[stage].Pos);
        Dot(Mathf.Clamp(target.x,3,W-4),Mathf.Clamp(target.y,3,H-4),3,goal);
        Line(new Vector2Int(W/2-4,24),new Vector2Int(W/2,33),Color.white,1);
        Line(new Vector2Int(W/2,33),new Vector2Int(W/2+4,24),Color.white,1);
        map.SetPixels32(pixels);map.Apply(false);
    }
    Vector2Int Project(Vector3 pos){var v=transform.InverseTransformPoint(pos);return new Vector2Int(W/2+Mathf.RoundToInt(v.x*.75f),28+Mathf.RoundToInt(v.z*.75f));}
    void Line(Vector2Int a,Vector2Int b,Color32 color,int r){
        int n=Mathf.Max(1,Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y)));
        for(int i=0;i<=n;i++)Dot(Mathf.RoundToInt(Mathf.Lerp(a.x,b.x,i/(float)n)),Mathf.RoundToInt(Mathf.Lerp(a.y,b.y,i/(float)n)),r,color);
    }
    void Dot(int x,int y,int radius,Color32 c){for(int j=-radius;j<=radius;j++)for(int i=-radius;i<=radius;i++)if(x+i>=0&&x+i<W&&y+j>=0&&y+j<H)pixels[(y+j)*W+x+i]=c;}
    void OnDestroy(){foreach(var rt in feeds){rt.Release();Destroy(rt);}if(map)Destroy(map);}
}

public sealed class GaragePreview {
    readonly Transform turntable;readonly Camera camera;
    public readonly RenderTexture image;
    public GaragePreview(){
        var model=new GameObject("Ghost Camaro • мастерская").AddComponent<Camaro>();
        model.BuildShell();turntable=model.transform;turntable.position=new Vector3(0,2200,0);
        foreach(var t in turntable.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        image=new RenderTexture(768,576,16);image.Create();
        camera=new GameObject("Камера мастерской").AddComponent<Camera>();camera.targetTexture=image;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.11f,.135f);
        camera.transform.position=turntable.position+new Vector3(-4,2.2f,4.3f);camera.transform.LookAt(turntable.position+Vector3.up*.64f);camera.fieldOfView=39;camera.farClipPlane=20;
        for(int i=0;i<2;i++){
            var lamp=new GameObject("Свет мастерской").AddComponent<Light>();lamp.type=LightType.Point;lamp.cullingMask=1<<30;lamp.range=10;lamp.intensity=24;
            lamp.color=i==0?new Color(.8f,.9f,1):new Color(1,.77f,.53f);lamp.transform.position=turntable.position+new Vector3(i==0?-2:2,3,i==0?2:-2);
        }
    }
    public void Tick(bool visible){camera.enabled=visible;if(visible)turntable.Rotate(Vector3.up,16*Time.unscaledDeltaTime,Space.World);}
}
