using UnityEngine;
using System.Collections.Generic;

// A composite district of Sarajevo in 1994. Coordinates and addresses are fictional.
public sealed class CityDistrict {
    readonly Transform district=new GameObject("Сараево • ночной рейс").transform;
    readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
    readonly Color plaster=new Color(.48f,.44f,.37f),stone=new Color(.31f,.32f,.32f),iron=new Color(.16f,.19f,.20f),light=new Color(.91f,.72f,.42f),red=new Color(.51f,.24f,.16f);
    GameObject Box(string name,float x,float y,float z,float w,float h,float d,Color color,bool solid=false){
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(district,false);g.transform.localPosition=new Vector3(x,y,z);g.transform.localScale=new Vector3(w,h,d);
        g.GetComponent<Collider>().enabled=solid;
        if(!materials.TryGetValue(color,out var mat)){mat=new Material(Camaro.Surface(false));mat.color=color;materials[color]=mat;}
        g.GetComponent<Renderer>().sharedMaterial=mat;g.isStatic=true;return g;
    }
    void Sign(string name,float x,float y,float z,float yaw){
        var text=new GameObject(name).AddComponent<TextMesh>();text.transform.SetParent(district,false);text.transform.position=new Vector3(x,y,z);text.transform.rotation=Quaternion.Euler(0,yaw,0);
        text.text=name;text.characterSize=.26f;text.fontSize=54;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.9f,.83f,.63f);
    }
    void Lamp(float x,float z){
        Box("Фонарь",x,2.25f,z,.14f,4.5f,.14f,iron);Box("Плафон",x,4.45f,z,.55f,.14f,.5f,light);
        var l=new GameObject("Дежурный свет").AddComponent<Light>();l.transform.SetParent(district,false);l.transform.position=new Vector3(x,4,z);l.type=LightType.Point;l.range=16;l.intensity=7;l.color=new Color(1,.72f,.45f);
    }
    void Building(float x,float z,int floors,int variation){
        float height=floors*2.8f,w=8+variation%3,d=11+variation%4;float side=Mathf.Sign(x),front=x-side*(w/2+.07f);
        Box("Жилой дом",x,height/2,z,w,height,d,variation%2==0?plaster:stone,true);
        Box("Карниз",x,height,z,w+.4f,.27f,d+.4f,iron);
        if(variation%3!=0){
            for(int sideRoof=-1;sideRoof<=1;sideRoof+=2)
                Box("Черепица",x+sideRoof*w*.24f,height+.65f,z,w*.58f,.26f,d+.55f,red).transform.rotation=Quaternion.Euler(0,0,-sideRoof*16);
        }else Box("Дымоход",x+2,height+.85f,z-2,1,1.7f,1,stone);
        for(int j=0;j<floors;j++)for(int i=-1;i<=1;i++){
            float wz=z+i*3.1f;Box("Оконная рама",front,1.65f+2.8f*j,wz,.16f,1.34f,1.33f,(j+i+variation)%6==0?light:iron);
            if((i+j+variation)%5==0)Box("Доска",front-side*.11f,1.65f+2.8f*j,wz,.14f,.23f,1.55f,red).transform.rotation=Quaternion.Euler(18,0,0);
            if(j>0&&i==0&&variation%2==1)Box("Балкон",front-side*.5f,.95f+2.8f*j,wz,1.2f,.35f,2.4f,stone);
        }
        Box("Подъезд",front,.9f,z+2,.18f,1.8f,.98f,iron);
    }
    public void Build(Route route){
        Box("Штаб — арка склада",-11,2.65f,-7,11,5.3f,16,stone,true);
        Box("Стальные ворота",-5.43f,1.65f,-7,.18f,3.3f,5.4f,iron);
        Sign("SKLADIŠTE   •   04",-5.25f,3.9f,-7,-90);
        for(int i=0;i<7;i++)Box("Ящики снабжения",7+i%3, .55f+(i/3)*.85f,-2+i%2,1.2f,1.05f,1.25f,red,true);
        var rand=new System.Random(1994);
        for(int i=0;i<30;i++){
            float z=32+i*21;float x=RoadAt(route,z);
            for(int s=-1;s<=1;s+=2){
                if((z>130&&z<308&&s==1)||(z>410&&z<540&&s==1))continue;
                Building(x+s*(16+rand.Next(6)),z+rand.Next(-3,4),rand.Next(2,6),rand.Next(11));
            }
            if(i%4==0)Lamp(x-6,z);
        }
        for(int i=0;i<2;i++)Box("Трамвайный рельс",-7.3f+i*1.45f,.02f,88,.10f,.04f,125,iron);
        Box("Депо",-16,3.2f,126,13,6.4f,17,red,true);Box("Ворота депо",-9.4f,2,126,.2f,4,6,iron);
        Sign("TRAMVAJSKI DEPO",-9.2f,4.9f,126,-90);
        Box("Трамвай",-7.1f,1.45f,109,2.4f,2.7f,8.5f,new Color(.54f,.38f,.21f),true);
        for(int i=0;i<5;i++)Box("Окно трамвая",-5.86f,1.95f,105.8f+i*1.45f,.06f,.85f,1.0f,iron);
        Box("Амбулатория",14,2.4f,365,13,4.8f,15,plaster,true);Sign("AMBULANTA",7.3f,3.8f,365,90);Lamp(6,360);
        Box("Река",38,.06f,483,30,.12f,148,new Color(.13f,.22f,.25f));
        Box("Парапет",21.8f,.55f,483,.7f,1.1f,148,stone,true);
        Box("Пункт воды",10,1.65f,510,4,3.3f,6,new Color(.25f,.32f,.27f),true);
        Sign("VODA",7.9f,2.5f,510,90);
        Box("Бочка с топливом",11,.68f,473,1.1f,1.36f,1.1f,red,true);Box("Пост снабжения",14,1.5f,473,4,3,5,stone,true);Sign("GORIVO",11.8f,2,473,90);
        Box("Навес заправки",70,4.4f,234,13,.55f,16,new Color(.48f,.32f,.18f));
        foreach(float x in new[]{65f,75f})foreach(float z in new[]{228f,240f})Box("Стойка",x,2.1f,z,.3f,4.2f,.3f,iron,true);
        Box("Колонка",72,1,234,1.1f,2,1.2f,red,true);Box("Табло",71.4f,1.4f,234,.08f,.54f,.79f,iron);
        Sign("GORIVO",64,4.4f,234,90);Lamp(73,232);
        Box("Гараж связи",-14,2.8f,584,12,5.6f,15,stone,true);Sign("RADIONICA",-7.9f,4,584,-90);Lamp(-7,580);
        for(int i=0;i<32;i++){
            float z=155+rand.Next(95),x=RoadAt(route,z)+rand.Next(-35,36);
            if(Mathf.Abs(x-RoadAt(route,z))<7)continue;
            Box("Обломки кладки",x,.24f,z,.7f,.48f,.6f,stone,true).transform.rotation=Quaternion.Euler(8,i*63,13);
        }
        for(int i=0;i<15;i++){
            float a=i*Mathf.PI*2/15;var hill=GameObject.CreatePrimitive(PrimitiveType.Sphere);hill.transform.SetParent(district,false);
            hill.transform.position=new Vector3(Mathf.Sin(a)*320,-28,320+Mathf.Cos(a)*560);
            hill.transform.localScale=new Vector3(250,130+rand.Next(75),290);hill.GetComponent<Collider>().enabled=false;hill.GetComponent<Renderer>().sharedMaterial=materials[iron];
        }
    }
    static float RoadAt(Route route,float z){
        for(int i=1;i<route.road.Length;i++)if(z<=route.road[i].z)return Mathf.Lerp(route.road[i-1].x,route.road[i].x,Mathf.InverseLerp(route.road[i-1].z,route.road[i].z,z));
        return 0;
    }
}
