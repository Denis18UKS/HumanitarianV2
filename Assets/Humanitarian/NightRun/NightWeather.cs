using UnityEngine;

public sealed class NightWeather {
    readonly ParticleSystem rain;
    readonly Light moon;
    int previous=-1;
    float previousFog=-1;
    public NightWeather(Transform camera){
        moon=GameObject.Find("Луна").GetComponent<Light>();
        var anchor=new GameObject("Осадки");anchor.transform.position=camera.position+Vector3.up*7;
        rain=anchor.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=rain.main;main.playOnAwake=false;main.loop=true;main.maxParticles=2000;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=1.2f;main.startSpeed=0;
        main.startSize=.042f;main.startColor=new Color(.62f,.72f,.84f,.7f);
        var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(22,1,24);
        var velocity=rain.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=-1.1f;velocity.y=-11;velocity.z=.3f;
        var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;
        renderer.lengthScale=2;renderer.velocityScale=.025f;
        renderer.sharedMaterial=Camaro.Surface(true);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
    }
    public void Tick(bool active,int mode,float fog,Camera camera){
        rain.transform.position=camera.transform.position+Vector3.up*7+Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up)*5;
        if(previous!=mode || !Mathf.Approximately(previousFog,fog)){
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=Mathf.Lerp(.011f,.029f,fog)*(mode==2?1.36f:mode==1?1.13f:1);
            RenderSettings.fogColor=mode==2?new Color(.086f,.108f,.143f):new Color(.069f,.092f,.135f);
            camera.backgroundColor=RenderSettings.fogColor;
            moon.intensity=mode==2?.42f:mode==1?.58f:.75f;
            var emission=rain.emission;emission.rateOverTime=mode==2?1050:360;
            var velocity=rain.velocityOverLifetime;velocity.x=mode==2?-3.2f:-.9f;
            previous=mode;previousFog=fog;
        }
        if(active && mode!=0){if(!rain.isPlaying)rain.Play();}
        else if(rain.isPlaying)rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
