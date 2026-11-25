using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable] public class RoutePoint { public float x,z; public string name; public Vector3 Pos=>new Vector3(x,0,z); }
[Serializable] public class Parcel { public string name; public float kg,fragility; }
[Serializable] public class Route {public int capacity;public RoutePoint[] road,stops;public Parcel[] cargo;}
[Serializable] public class Trip {
    public int stage; public int[] status=new int[3]; public float[] quality={100,100,100};
    public float fuel=100,health=100,seconds,yaw; public Vector3 position; public bool lid;
}

public sealed class NightRun : MonoBehaviour {
    Route route; Trip trip; Camaro car; Camera cam;
    string page="Меню", radio=""; bool cabin, headlights=true, map, cargo;
    bool doors,hood;
    float steer,gas, sensitivity=1,look; bool hand;
    const string Slot="Humanitarian.NightRoute.1";
    bool worldReady;
    bool loadCheckpoint;
    string startupError;
    void Awake(){
        cam=new GameObject("Камера").AddComponent<Camera>();
        cam.gameObject.AddComponent<AudioListener>();
        cam.nearClipPlane=.035f;cam.farClipPlane=1100;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.045f,.067f,.11f);
        Debug.Log("Humanitarian: главное меню открыто");
    }
    void BeginTrip(bool load){
        if(page=="Загрузка")return;
        loadCheckpoint=load;
        page="Загрузка";
        StartCoroutine(LoadTrip());
    }
    System.Collections.IEnumerator LoadTrip(){
        yield return null;
        yield return null;
        try{
            if(!worldReady){
                var data=Resources.Load<TextAsset>("route");
                if(!data)throw new InvalidOperationException("Отсутствует ресурс route.json");
                route=JsonUtility.FromJson<Route>(data.text);
                World();
                car=new GameObject("Ghost Camaro").AddComponent<Camaro>();
                car.Assemble();
                car.impact=Damage;
                worldReady=true;
            }
            ResetTrip(loadCheckpoint);
        }catch(Exception ex){
            startupError=ex.Message;
            Debug.LogException(ex);
            page="Ошибка запуска рейса";
        }
    }
    void World(){
        RenderSettings.ambientLight=new Color(.35f,.39f,.48f);
        var sun=new GameObject("Луна").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.62f,.72f,.97f);sun.intensity=.75f;sun.transform.rotation=Quaternion.Euler(37,-35,0);
        Camaro.Block("Ground",new Vector3(0,-.21f,280),new Vector3(1200,.4f,1400),new Color(.11f,.16f,.13f));
        for(int i=1;i<route.road.Length;i++){
            var a=route.road[i-1].Pos;var b=route.road[i].Pos;var rot=Quaternion.LookRotation(b-a);
            Camaro.Block("Дорога",(a+b)*.5f,new Vector3(9,.025f,(b-a).magnitude+2),new Color(.22f,.23f,.25f),null,false).transform.rotation=rot;
            for(float d=2;d<(b-a).magnitude;d+=14)Camaro.Block("Разметка",a+(b-a).normalized*d+Vector3.up*.024f,new Vector3(.12f,.02f,3),Color.gray,null,false).transform.rotation=rot;
        }
        Camaro.Block("Закрытый мост",new Vector3(0,.7f,185),new Vector3(14,1.4f,2),new Color(.67f,.36f,.13f));
        foreach(var s in route.stops){
            for(int i=0;i<4;i++)Camaro.Block("Дом",s.Pos+new Vector3(i%2==0?-14:14,2.5f,(i/2)*18-8),new Vector3(8,5,9),new Color(.31f,.26f,.23f));
            Camaro.Block("Знак",s.Pos+new Vector3(5,1.5f,0),new Vector3(.45f,3,.45f),new Color(.89f,.65f,.22f));
        }
    }
    void ResetTrip(bool load){
        trip=load&&PlayerPrefs.HasKey(Slot)?JsonUtility.FromJson<Trip>(PlayerPrefs.GetString(Slot)):new Trip{position=Vector3.up*.3f};
        car.body.isKinematic=false;car.body.linearVelocity=Vector3.zero;car.body.angularVelocity=Vector3.zero;car.body.position=trip.position;car.body.rotation=Quaternion.Euler(0,trip.yaw,0);
        page="Рейс";radio="Диспетчер • Открой багажник и загрузи три партии.";if(!load && page!="Меню")Save();
        Sync();
    }
    void Save(){trip.position=car.body.position;trip.yaw=car.transform.eulerAngles.y;PlayerPrefs.SetString(Slot,JsonUtility.ToJson(trip));PlayerPrefs.Save();}
    void Sync(){
        for(int i=0;i<3;i++)car.boxes[i].SetActive(trip.status[i]==1);
        car.parts["Trunk"].localRotation=Quaternion.Euler(trip.lid?58:0,0,0);
        foreach(var light in car.beams)light.enabled=headlights;
    }
    float Mass(){float kg=0;for(int i=0;i<3;i++)if(trip.status[i]==1)kg+=route.cargo[i].kg;return kg;}
    void Damage(float speed){if(page!="Рейс")return;trip.health=Mathf.Max(0,trip.health-speed*1.15f);for(int i=0;i<3;i++)if(trip.status[i]==1)trip.quality[i]=Mathf.Max(0,trip.quality[i]-speed*route.cargo[i].fragility);}
    float Distance()=>Vector2.Distance(new Vector2(car.body.position.x,car.body.position.z),new Vector2(route.stops[trip.stage].x,route.stops[trip.stage].z));
    string Hint(){
        if(Distance()>10)return "Следуй к отметке • M — схема";
        if(car.body.linearVelocity.magnitude>1)return "Остановись у получателя";
        if(trip.stage==4)return "E — закончить рейс";
        if(!trip.lid)return "E — открыть багажник";
        if(trip.stage==0){int i=Array.IndexOf(trip.status,0);return i<0?"E — закрыть багажник и выехать":"E — загрузить: "+route.cargo[i].name;}
        return "E — передать: "+route.cargo[trip.stage-1].name;
    }
    void Interact(){
        if(Distance()>10 || car.body.linearVelocity.magnitude>1)return;
        if(trip.stage==4){page=EndTitle();return;}
        if(!trip.lid)trip.lid=true;
        else if(trip.stage==0){
            int i=Array.IndexOf(trip.status,0);
            if(i<0){trip.stage++;trip.lid=false;radio="Диспетчер • Продовольствие ждут в Сосновом дворе.";}
            else if(Mass()+route.cargo[i].kg<=route.capacity)trip.status[i]=1;
        }else{
            int i=trip.stage-1;if(trip.status[i]!=1)return;trip.status[i]=2;trip.stage++;trip.lid=false;
            radio=i==0?"Диспетчер • Мост закрыт. На развилке держись правой дороги.":"Диспетчер • Следующая точка: "+route.stops[trip.stage].name;
        }
        Sync();Save();
    }
    string EndTitle()=>Array.Exists(trip.quality,q=>q<50)?"Частичный успех":"Рейс завершён";
    void Update(){
        var k=Keyboard.current;if(k==null)return;
        if(k.escapeKey.wasPressedThisFrame){if(page=="Рейс")page="Пауза";else if(page=="Пауза")page="Рейс";}
        if(page=="Меню" && k.enterKey.wasPressedThisFrame){BeginTrip(false);return;}
        if(page!="Рейс")return;
        gas=(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0);steer=(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0);hand=k.spaceKey.isPressed;
        if(car.body.linearVelocity.magnitude<.5f){
            if(k.hKey.wasPressedThisFrame)hood=!hood;
            if(k.oKey.wasPressedThisFrame)doors=!doors;
            car.parts["Hood"].localRotation=Quaternion.Euler(hood?-55:0,0,0);
            car.parts["Door_L_hinge"].localRotation=Quaternion.Euler(0,doors?58:0,0);
            car.parts["Door_R_hinge"].localRotation=Quaternion.Euler(0,doors?-58:0,0);
        }
        if(k.eKey.wasPressedThisFrame)Interact();if(k.cKey.wasPressedThisFrame){cabin=!cabin;look=0;}
        if(Mouse.current!=null && Mouse.current.rightButton.isPressed)look=Mathf.Clamp(look+Mouse.current.delta.x.ReadValue()*sensitivity*.15f,-85,85);
        if(k.lKey.wasPressedThisFrame){headlights=!headlights;Sync();}
        if(k.mKey.wasPressedThisFrame)map=!map;if(k.tabKey.wasPressedThisFrame)cargo=!cargo;
        if(k.rKey.wasPressedThisFrame)ResetTrip(true);
    }
    void FixedUpdate(){
        if(!worldReady)return;
        bool active=page=="Рейс";car.body.isKinematic=!active;
        if(!active)return;
        car.Drive(gas,steer,hand,Mass(),trip.stage>0&&!trip.lid);
        trip.seconds+=Time.fixedDeltaTime;trip.fuel=Mathf.Max(0,trip.fuel-car.body.linearVelocity.magnitude*Time.fixedDeltaTime*.075f);
        if(trip.fuel<=0||trip.health<=0)page="Рейс прерван";
    }
    void LateUpdate(){
        if(!worldReady || trip==null)return;
        var t=car.transform;var dir=t.rotation*Quaternion.Euler(0,look,0);
        cam.transform.position=cabin?t.TransformPoint(new Vector3(-.4f,1.02f,.03f)):t.position+dir*new Vector3(0,3.3f,-6.5f);
        cam.transform.rotation=cabin?dir:Quaternion.LookRotation(t.position+Vector3.up*.7f-cam.transform.position);
    }
    void OnGUI(){
        GUI.matrix=Matrix4x4.Scale(Vector3.one*Mathf.Clamp(Screen.height/750f,.75f,2f));
        GUILayout.BeginArea(new Rect(20,20,440,700),GUI.skin.box);
        GUILayout.Label("HUMANITARIAN   /   Ночной рейс");
        if(page=="Загрузка"){GUILayout.Label("Загрузка маршрута и Ghost Camaro…");}
        else if(page=="Ошибка запуска рейса"){
            GUILayout.Label("Не удалось открыть рейс");
            GUILayout.Label(startupError);
            GUILayout.Label(Application.consoleLogPath);
            if(GUILayout.Button("Вернуться в главное меню"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("NightRoute");
        }else if(page=="Рейс"){
            GUILayout.Label(route.stops[trip.stage].name+"   •   "+Distance().ToString("0")+" м");
            GUILayout.Label($"{car.body.linearVelocity.magnitude*3.6f:0} км/ч    Топливо {trip.fuel:0}%    Кузов {trip.health:0}%");
            GUILayout.Label($"Курс на цель {Vector3.SignedAngle(car.transform.forward,route.stops[trip.stage].Pos-car.body.position,Vector3.up):+0;-0;0}°   Передано {Mathf.Max(0,trip.stage-1)}/3");
            GUILayout.Label(Hint());GUILayout.Label(radio);
            GUILayout.Label("C камера   L свет   Tab груз   M маршрут   Esc пауза");
            if(map){DrawMap();}
            if(map)GUILayout.Label("Склад → Сосновый двор → правый объезд → Медпункт → Водонапорная станция → Гараж");
            if(cargo)Manifest();
        }else{
            GUILayout.Label(page);
            if(page!="Меню"&&page!="Пауза"){Manifest();GUILayout.Label($"Время {trip.seconds/60:0.0} мин • топливо {trip.fuel:0}% • кузов {trip.health:0}%");}
            if(page=="Пауза"&&GUILayout.Button("Вернуться за руль"))page="Рейс";
            if(PlayerPrefs.HasKey(Slot)&&GUILayout.Button("Продолжить с контрольной точки"))BeginTrip(true);
            if(GUILayout.Button("Начать новый рейс  [Enter]"))BeginTrip(false);
            GUILayout.Label("W/S газ и тормоз с реверсом • A/D руль • Space ручник • E погрузка и передача • R контрольная точка");
            GUILayout.Label("Громкость");AudioListener.volume=GUILayout.HorizontalSlider(AudioListener.volume,0,1);
            GUILayout.Label("Чувствительность обзора (ПКМ)");sensitivity=GUILayout.HorizontalSlider(sensitivity,.3f,2);
            if(GUILayout.Button("Качество: "+QualitySettings.names[QualitySettings.GetQualityLevel()]))QualitySettings.SetQualityLevel((QualitySettings.GetQualityLevel()+1)%QualitySettings.names.Length);
            if(page!="Меню"&&GUILayout.Button("Главное меню"))page="Меню";
            if(GUILayout.Button("Выход"))Application.Quit();
        }GUILayout.EndArea();
    }
    void DrawMap(){
        Rect box=GUILayoutUtility.GetRect(240,165); GUI.Box(box,"");
        Vector2 MapPoint(Vector3 pos)=>new Vector2(box.center.x+pos.x*.8f,box.yMax-10-pos.z*.24f);
        var old=GUI.color;
        for(int i=1;i<route.road.Length;i++){
            var a=MapPoint(route.road[i-1].Pos);var b=MapPoint(route.road[i].Pos);
            for(int j=0;j<=30;j++){var p=Vector2.Lerp(a,b,j/30f);GUI.DrawTexture(new Rect(p.x,p.y,2,2),Texture2D.whiteTexture);}
        }
        GUI.color=Color.yellow;var goal=MapPoint(route.stops[trip.stage].Pos);GUI.DrawTexture(new Rect(goal.x-3,goal.y-3,6,6),Texture2D.whiteTexture);
        GUI.color=Color.cyan;var carPos=MapPoint(car.body.position);GUI.DrawTexture(new Rect(carPos.x-2,carPos.y-2,4,4),Texture2D.whiteTexture);GUI.color=old;
    }
    void Manifest(){for(int i=0;i<3;i++)GUILayout.Label($"{route.cargo[i].name} • {route.cargo[i].kg} кг • {trip.quality[i]:0}% • {new[]{"склад","в машине","передано"}[trip.status[i]]}");GUILayout.Label($"В багажнике {Mass()} / {route.capacity} кг");}
}
