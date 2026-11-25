using UnityEngine;

public sealed partial class NightRun {
    NightWeather weather;
    int weatherMode, fps;
    float fogLevel, fov, volume;
    bool showRadio;
    string returnPage="Меню";
    Texture2D cover;
    static readonly string[] Skies={"Туман","Дождь и туман","Ливень и туман"};
    static readonly int[] Caps={30,60,90,120};
    static readonly Color Ink=new Color(.031f,.055f,.069f), Cream=new Color(.91f,.9f,.84f), Gold=new Color(.92f,.68f,.33f);
    const string Prefix="NightRoute.Options.";
    void LoadOptions(){
        if(PlayerPrefs.HasKey(Prefix+"quality"))QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"quality"),0,QualitySettings.names.Length-1));
        weatherMode=Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"weather",1),0,2);
        fogLevel=Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"fog",.35f));
        seatLift=Mathf.Clamp(PlayerPrefs.GetFloat(Prefix+"seatLift",0),-.045f,.045f);
        sensitivity=PlayerPrefs.GetFloat(Prefix+"mouse",1);fov=PlayerPrefs.GetFloat(Prefix+"fov",65);
        volume=PlayerPrefs.GetFloat(Prefix+"volume",.75f);fps=Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"fps",1),0,3);
        showRadio=PlayerPrefs.GetInt(Prefix+"radio",1)!=0;
        if(PlayerPrefs.HasKey(Prefix+"full")) Screen.fullScreen=PlayerPrefs.GetInt(Prefix+"full")!=0;
        cover=Resources.Load<Texture2D>("MenuCamaro");ApplyOptions();
    }
    void ApplyOptions(){cam.fieldOfView=fov;AudioListener.volume=volume;QualitySettings.vSyncCount=0;Application.targetFrameRate=Caps[fps];}
    void SaveOptions(){
        PlayerPrefs.SetFloat(Prefix+"seatLift",seatLift);
        PlayerPrefs.SetInt(Prefix+"weather",weatherMode);PlayerPrefs.SetInt(Prefix+"fps",fps);PlayerPrefs.SetInt(Prefix+"radio",showRadio?1:0);
        PlayerPrefs.SetInt(Prefix+"full",Screen.fullScreen?1:0);PlayerPrefs.SetFloat(Prefix+"fog",fogLevel);PlayerPrefs.SetFloat(Prefix+"mouse",sensitivity);
        PlayerPrefs.SetFloat(Prefix+"volume",volume);PlayerPrefs.SetFloat(Prefix+"fov",fov);PlayerPrefs.Save();
    }
    void OnApplicationQuit(){SaveOptions();}
    static void Panel(Rect rect,Color color){
        var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
    }
    static void Label(float x,float y,float w,float h,string text,int size,Color color,bool bold=false){
        var style=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,wordWrap=true,alignment=TextAnchor.MiddleLeft};
        style.normal.textColor=color;GUI.Label(new Rect(x,y,w,h),text,style);
    }
    static bool Button(float x,float y,float w,string text,bool primary=false){
        var rect=new Rect(x,y,w,44);
        Panel(rect,rect.Contains(Event.current.mousePosition)?new Color(.36f,.38f,.34f):primary?new Color(.81f,.60f,.30f):new Color(.105f,.15f,.17f));
        Label(x+14,y,w-23,44,text,18,primary?Ink:Cream,true);
        return GUI.Button(rect,GUIContent.none,GUIStyle.none);
    }
    void OnGUI(){
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        if(page!="Рейс")Panel(new Rect(0,0,Screen.width,Screen.height),Ink);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,Vector3.one*scale);
        if(page=="Рейс"){Hud();return;}
        if(page=="Настройки"){Options();return;}
        if(cover)GUI.DrawTexture(new Rect(462,0,818,720),cover,ScaleMode.ScaleAndCrop);
        Panel(new Rect(0,0,462,720),Ink);Panel(new Rect(44,91,46,3),Gold);
        Label(44,110,410,63,"HUMANITARIAN",46,Cream,true);
        Label(44,173,395,40,page=="Меню"?"Ночной рейс":page,25,Gold);
        if(page=="Загрузка"){Label(44,240,370,75,"Подготовка машины и маршрута…",20,Cream);return;}
        if(page=="Ошибка запуска рейса"){
            Label(44,225,378,205,startupError+"\n"+Application.consoleLogPath,17,Cream);
            if(Button(44,474,355,"В главное меню"))UnityEngine.SceneManagement.SceneManager.LoadScene("NightRoute");
            return;
        }
        if(page=="Управление"){
            Label(44,220,375,300,"W / S   Газ, тормоз, задний ход\nA / D   Руль      Пробел   Ручник\nE   Погрузка и передача груза\nC   Камера      ПКМ   Осмотреть салон\nV   Запросить диспетчера\nL   Фары      O / H   Двери и капот\nM   Карта      Tab   Груз\nR   Контрольная точка      Esc   Пауза",19,Cream);
            if(Button(44,552,355,"Назад"))page=returnPage;return;
        }
        float y=232;
        if(page!="Меню"&&page!="Пауза"&&trip!=null){
            Label(44,y,380,90,$"Передано {System.Array.FindAll(trip.status,v=>v==2).Length}/3   •   {trip.seconds/60:0.0} мин\nСохранность партий: {trip.quality[0]:0}% / {trip.quality[1]:0}% / {trip.quality[2]:0}%\nТопливо {trip.fuel:0}%   •   Кузов {trip.health:0}%",17,Cream);
            y+=94;
        }
        if(page=="Пауза"){if(Button(44,y,355,"Вернуться за руль",true))page="Рейс";y+=50;}
        if(Button(44,y,355,"Начать новый рейс",page!="Пауза"))BeginTrip(false);y+=50;
        if(PlayerPrefs.HasKey(Slot)){if(Button(44,y,355,"Продолжить с контрольной точки"))BeginTrip(true);y+=50;}
        if(Button(44,y,355,"Настройки")){returnPage=page;page="Настройки";}y+=50;
        if(Button(44,y,355,"Управление")){returnPage=page;page="Управление";}y+=50;
        if(page!="Меню"){if(Button(44,y,355,"Главное меню"))page="Меню";y+=50;}
        if(Button(44,y,355,"Выйти из игры"))Application.Quit();
    }
    void Hud(){
        var bg=new Color(.023f,.04f,.05f,.86f);
        Panel(new Rect(28,24,380,78),bg);Panel(new Rect(28,24,4,78),Gold);
        Label(47,28,340,36,route.stops[trip.stage].name,24,Cream,true);
        Label(47,63,345,31,$"{Distance():0} м   •   Передано {Mathf.Max(0,trip.stage-1)}/3   •   {Skies[weatherMode]}",15,Gold);
        Panel(new Rect(957,605,295,87),bg);
        Label(974,610,255,44,$"{car.body.linearVelocity.magnitude*3.6f:0} км/ч",30,Cream,true);
        Label(974,660,268,23,$"Топливо {trip.fuel:0}%    Кузов {trip.health:0}%",16,Gold);
        Label(30,607,850,32,Hint(),21,Cream,true);
        if(showRadio)Label(30,644,850,40,radio,17,Cream);
        if(map||cargo){
            GUILayout.BeginArea(new Rect(28,118,380,map&&cargo?365:220),GUI.skin.box);
            if(map)DrawMap();if(cargo)Manifest();GUILayout.EndArea();
        }
    }
    static float Slider(float x,float y,string title,string value,float current,float min,float max){
        Label(x,y,276,30,title,19,Cream);Label(x+288,y,140,30,value,17,Gold);
        return GUI.HorizontalSlider(new Rect(x,y+42,365,20),current,min,max);
    }
    void Options(){
        Label(70,48,700,72,"Настройки",42,Cream,true);
        Panel(new Rect(70,130,1140,2),Gold);
        Label(70,152,400,40,"Ночной рейс",24,Gold,true);
        Label(688,152,420,40,"Изображение и звук",24,Gold,true);
        GUI.changed=false;
        if(Button(70,212,365,"Погода   ·   "+Skies[weatherMode])){weatherMode=(weatherMode+1)%Skies.Length;SaveOptions();}
        fogLevel=Slider(70,282,"Плотность тумана",$"{Mathf.Lerp(.011f,.029f,fogLevel):0.000}",fogLevel,0,1);
        sensitivity=Slider(70,359,"Обзор мышью",$"{sensitivity:0.0}×",sensitivity,.3f,2);
        if(Button(70,453,365,"Текст рации   ·   "+(showRadio?"Вкл":"Выкл"))){showRadio=!showRadio;SaveOptions();}
        seatLift=Slider(70,512,"Высота сиденья",$"{seatLift*100:+0.0;-0.0;0} см",seatLift,-.045f,.045f);
        volume=Slider(688,202,"Громкость",$"{volume*100:0}%",volume,0,1);
        fov=Slider(688,280,"Угол обзора",$"{fov:0}°",fov,48,92);
        if(Button(688,366,365,"Частота кадров   ·   "+Caps[fps])){fps=(fps+1)%Caps.Length;SaveOptions();}
        if(Button(688,423,365,Screen.fullScreen?"Экран   ·   Полный":"Экран   ·   Оконный"))Screen.fullScreen=!Screen.fullScreen;
        if(Button(688,480,365,"Графика   ·   "+QualitySettings.names[QualitySettings.GetQualityLevel()])){
            int quality=(QualitySettings.GetQualityLevel()+1)%QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(quality);PlayerPrefs.SetInt(Prefix+"quality",quality);
        }
        if(GUI.changed)ApplyOptions();
        ApplyOptionsIfNeeded();
        if(Button(70,612,365,"Сохранить и вернуться",true)){SaveOptions();page=returnPage;}
    }
    void ApplyOptionsIfNeeded(){
        if(Application.targetFrameRate!=Caps[fps]||cam.fieldOfView!=fov||AudioListener.volume!=volume||QualitySettings.vSyncCount!=0)ApplyOptions();
    }
}
