using UnityEngine;

// Glove positions follow the visible rim. Sleeve ends stay at the driver's seat.
public sealed class DriverHands : MonoBehaviour {
    Transform wheel;
    readonly Transform[] gloves=new Transform[2];
    readonly Transform[] sleeves=new Transform[2];
    static Transform Ellipsoid(Transform parent,string name,Vector3 pos,Vector3 size,Color color){
        var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        part.name=name;part.transform.SetParent(parent,false);
        part.transform.localPosition=pos;part.transform.localScale=size;
        part.GetComponent<Collider>().enabled=false;
        var mat=new Material(Camaro.Surface(false));mat.color=color;
        part.GetComponent<Renderer>().sharedMaterial=mat;
        return part.transform;
    }
    public void Fit(Transform steering){
        wheel=steering;
        var leather=new Color(.17f,.14f,.115f);
        for(int i=0;i<2;i++){
            float side=i==0?-1:1;
            gloves[i]=new GameObject("Перчатка").transform;gloves[i].SetParent(transform,false);
            Ellipsoid(gloves[i],"Ладонь",Vector3.zero,new Vector3(.051f,.082f,.050f),leather);
            for(int f=0;f<4;f++)
                Ellipsoid(gloves[i],"Палец",new Vector3(-side*.016f,.029f-f*.017f,.014f),new Vector3(.045f,.016f,.027f),leather);
            Ellipsoid(gloves[i],"Большой палец",new Vector3(-side*.027f,.018f,-.010f),new Vector3(.044f,.023f,.025f),leather);
            sleeves[i]=Ellipsoid(transform,"Рукав",Vector3.zero,Vector3.one,new Color(.115f,.145f,.13f));
        }
        gameObject.SetActive(false);
    }
    public void Pose(bool visible){
        if(gameObject.activeSelf!=visible)gameObject.SetActive(visible);
        if(!visible)return;
        for(int i=0;i<2;i++){
            float side=i==0?-1:1;
            var hand=gloves[i];
            hand.SetPositionAndRotation(wheel.TransformPoint(new Vector3(side*.185f,.051f,-.018f)),wheel.rotation);
            var elbow=transform.TransformPoint(new Vector3(-.4f+side*.172f,.71f,-.23f));
            var wrist=hand.position+hand.rotation*new Vector3(0,-.026f,-.015f);
            var arm=sleeves[i];arm.position=(wrist+elbow)*.5f;arm.rotation=Quaternion.FromToRotation(Vector3.up,wrist-elbow);
            arm.localScale=new Vector3(.07f,(wrist-elbow).magnitude+.035f,.075f);
        }
    }
}
