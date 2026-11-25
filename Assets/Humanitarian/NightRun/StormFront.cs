using UnityEngine;

public sealed class StormFront {
    readonly Transform[] wisps=new Transform[9];
    readonly Light flash;
    readonly AudioSource thunder;
    float next=9, glow,delay=-1;
    public StormFront(Camaro car){
        for(int i=0;i<wisps.Length;i++){
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Полоса низкого тумана";quad.GetComponent<Collider>().enabled=false;
            quad.transform.position=new Vector3(i%2==0?-5:14,1.8f,i*67+22);quad.transform.localScale=new Vector3(37+i%3*8,4.5f,1);
            quad.GetComponent<Renderer>().sharedMaterial=CockpitInstruments.Effect(3);wisps[i]=quad.transform;
        }
        var sky=new GameObject("Грозовой фронт");flash=sky.AddComponent<Light>();flash.type=LightType.Directional;
        flash.transform.rotation=Quaternion.Euler(51,65,0);flash.color=new Color(.73f,.83f,1);flash.intensity=0;
        thunder=car.gameObject.AddComponent<AudioSource>();thunder.clip=Resources.Load<AudioClip>("Thunder");thunder.playOnAwake=false;thunder.spatialBlend=0;
    }
    public void Tick(Camera camera,bool storm,bool active){
        for(int i=0;i<wisps.Length;i++){
            var v=camera.transform.position-wisps[i].position;v.y=0;if(v.sqrMagnitude>1)wisps[i].rotation=Quaternion.LookRotation(v);
        }
        if(!active||!storm){flash.intensity=0;glow=0;delay=-1;if(thunder.isPlaying)thunder.Stop();return;}
        float dt=Time.deltaTime;next-=dt;
        if(next<=0){glow=1;delay=1.5f+Random.value*1.5f;next=Random.Range(15f,30f);}
        glow=Mathf.MoveTowards(glow,0,dt*2.5f);flash.intensity=glow*glow*2.2f;
        if(delay>=0){delay-=dt;if(delay<0)thunder.Play();}
    }
}
