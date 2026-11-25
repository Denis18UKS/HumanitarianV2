using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

public sealed class Camaro : MonoBehaviour
{
    public readonly Dictionary<string,Transform> parts = new Dictionary<string,Transform>();
    public readonly WheelCollider[] wheels = new WheelCollider[4];
    public readonly Light[] beams = new Light[2];
    public readonly Light[] stops = new Light[2];
    public readonly GameObject[] boxes = new GameObject[3];
    public Rigidbody body;
    public System.Action<float> impact;
    AudioSource motor;
    static readonly string[] axles = {"Wheel_FL","Wheel_FR","Wheel_RL","Wheel_RR"};
    static Vector3 ReadVector(BinaryReader r) {
        float x=r.ReadSingle(), y=r.ReadSingle(), z=r.ReadSingle(); return new Vector3(y,z,-x);
    }
    public static GameObject Block(string name,Vector3 pos,Vector3 size,Color color,Transform parent=null,bool solid=true) {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name=name; obj.transform.SetParent(parent,false); obj.transform.localPosition=pos;
        obj.transform.localScale=size;
        string texture=name=="Дорога"?"Asphalt":name=="Дом"?"Brick":name=="Ground"?"Grass":null;
        var surface=new Material(Surface(false));
        surface.SetColor("_BaseColor",color);
        if(texture!=null){
            surface.SetTexture("_BaseMap",Resources.Load<Texture2D>(texture));
            surface.SetTextureScale("_BaseMap",new Vector2(size.x/3, (texture=="Brick"?size.y:size.z)/3));
        }
        obj.GetComponent<Renderer>().sharedMaterial=surface;
        obj.GetComponent<Collider>().enabled=solid;
        return obj;
    }
    public static Material Surface(bool transparent){
        string key=transparent?"NightGlass":"NightLit";
        var material=Resources.Load<Material>(key);
        if(!material || !material.shader.isSupported)throw new System.InvalidOperationException("Недоступен материал "+key);
        return material;
    }
    public void Assemble() {
        body=gameObject.AddComponent<Rigidbody>(); body.mass=1450; body.centerOfMass=new Vector3(0,.28f,0);
        body.interpolation=RigidbodyInterpolation.Interpolate;
        var shell=gameObject.AddComponent<BoxCollider>();shell.center=new Vector3(0,.66f,0);shell.size=new Vector3(1.82f,.7f,4.88f);
        using(var r=new BinaryReader(new MemoryStream(Resources.Load<TextAsset>("camaro").bytes))) {
            int sections=r.ReadInt32();
            for(int i=0;i<sections;i++) {
                string group=Encoding.ASCII.GetString(r.ReadBytes(32)).TrimEnd('\0');
                string atlas=Encoding.ASCII.GetString(r.ReadBytes(32)).TrimEnd('\0');
                var pivot=ReadVector(r);
                if(!parts.TryGetValue(group,out var parent)) {
                    parent=new GameObject(group).transform;parent.SetParent(transform,false);parent.localPosition=pivot;parts.Add(group,parent);
                }
                var child=new GameObject("Surface");child.transform.SetParent(parent,false);
                int count=r.ReadInt32();var v=new Vector3[count];var indices=new int[count];var uv=new Vector2[count];
                for(int j=0;j<count;j++) {v[j]=ReadVector(r);uv[j]=new Vector2(r.ReadSingle(),r.ReadSingle());indices[j]=j%3==1?j+1:j%3==2?j-1:j;}
                var m=new Mesh();m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.vertices=v;m.uv=uv;m.triangles=indices;m.RecalculateNormals();m.RecalculateBounds();
                child.AddComponent<MeshFilter>().sharedMesh=m;
                bool glass=atlas=="Glass_V5"||atlas=="Side_Tint_V5";
                var material=new Material(Surface(glass));
                var texture=Resources.Load<Texture2D>(atlas);
                if(!texture)throw new InvalidDataException("Нет текстуры "+atlas);
                material.SetTexture("_BaseMap", texture);
                child.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
        }
        for(int i=0;i<4;i++) {
            var mount=new GameObject(axles[i]+" suspension"); mount.transform.SetParent(transform,false);
            mount.transform.localPosition=parts[axles[i]].localPosition+Vector3.up*.09f;
            wheels[i]=mount.AddComponent<WheelCollider>();wheels[i].radius=.33f;wheels[i].suspensionDistance=.18f;wheels[i].mass=22;
        }
        for(int i=0;i<2;i++) {
            beams[i]=Lamp(new Vector3(i==0?-.68f:.68f,.58f,2.4f),Color.white,true);
            stops[i]=Lamp(new Vector3(i==0?-.62f:.62f,.65f,-2.47f),Color.red,false);
        }
        for(int i=0;i<3;i++)boxes[i]=Block("Cargo "+i,new Vector3((i-1)*.43f,.61f,-2.05f),new Vector3(.37f,.28f,.42f),new[]{new Color(.56f,.35f,.17f),Color.white,new Color(.24f,.46f,.61f)}[i],transform,false);
        motor=gameObject.AddComponent<AudioSource>();motor.clip=Resources.Load<AudioClip>("idle");motor.loop=true;motor.spatialBlend=.3f;motor.Play();
    }
    Light Lamp(Vector3 pos,Color color,bool front) {
        var t=new GameObject("Lamp").transform;t.SetParent(transform,false);t.localPosition=pos;
        t.localRotation=Quaternion.Euler(4,front?0:180,0);
        var l=t.gameObject.AddComponent<Light>();l.type=front?LightType.Spot:LightType.Point;l.spotAngle=58;l.range=front?75:4;l.intensity=front?8:.4f;l.color=color;return l;
    }
    public void Drive(float throttle,float steer,bool hand,float mass,bool moving) {
        body.mass=1450+mass;
        float forward=Vector3.Dot(body.linearVelocity,transform.forward);
        bool braking=forward*throttle < -.5f;
        for(int i=0;i<4;i++) {
            var w=wheels[i];w.steerAngle=i<2?steer*29:0;
            w.motorTorque=moving && !braking && i>=2 && Mathf.Abs(forward)<25?throttle*1250:0;
            w.brakeTorque=!moving||hand?3500:braking?2600:Mathf.Abs(throttle)<.05f?45:0;
            w.GetWorldPose(out var p,out var q);parts[axles[i]].SetPositionAndRotation(p,q);
        }
        foreach(var l in stops) l.intensity=braking||hand?3:.35f;
        parts["Steering_Wheel"].localRotation=Quaternion.Euler(0,0,-80*steer);
        motor.pitch=.8f+Mathf.Abs(forward)*.045f;
    }
    void OnCollisionEnter(Collision c){if(c.collider.name!="Ground" && c.relativeVelocity.magnitude>4) impact?.Invoke(c.relativeVelocity.magnitude);}
}
