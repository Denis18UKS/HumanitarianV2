using UnityEngine;

public sealed class CabinRadio : MonoBehaviour {
    AudioSource speaker;
    Material indicator;
    readonly AudioClip[] calls=new AudioClip[5];
    Color idle=new Color(.12f,.25f,.17f);
    static readonly Color receive=new Color(.38f,1f,.45f);
    public static readonly string[] Messages={
        "Камаро, это база. Проверь багажник. Продовольствие, лекарства и вода — всё должно быть на борту. Приём.",
        "Первый адрес — трамвайное депо. Во дворе ждут продукты.",
        "Депо на связи. Продукты получили, спасибо. Мост закрыт. Держись объезда. Лекарства ждут в амбулатории.",
        "Здесь амбулатория. Лекарства приняли. Осталась вода. Люди ждут на набережной.",
        "Вода на месте. Сегодня всем хватит. Возвращайся в гараж, мы оставили свет. Конец связи."
    };
    GameObject Part(string id,Vector3 position,Vector3 size,Color color){
        return Camaro.Block(id,position,size,color,transform,false);
    }
    public void Fit(Transform vehicle){
        transform.SetParent(vehicle,false);
        // Below the factory receiver, with its face pointing toward the seats.
        transform.localPosition=new Vector3(-.014f,.607f,.182f);
        var charcoal=new Color(.14f,.15f,.15f);
        Part("Корпус рации",Vector3.zero,new Vector3(.20f,.066f,.11f),charcoal);
        Part("Приёмник",new Vector3(-.019f,0,-.057f),new Vector3(.082f,.034f,.006f),new Color(.14f,.32f,.25f));
        indicator=Part("RX",new Vector3(.041f,.013f,-.06f),new Vector3(.012f,.009f,.005f),idle).GetComponent<Renderer>().material;
        for(int i=0;i<5;i++)Part("Решётка",new Vector3(-.084f+i*.007f,0,-.058f),new Vector3(.003f,.035f,.004f),Color.black);
        var mic=Part("Тангента",new Vector3(.127f,.034f,-.022f),new Vector3(.043f,.071f,.030f),charcoal);
        mic.transform.localRotation=Quaternion.Euler(-12,0,-9);
        Part("Клавиша передачи",new Vector3(.104f,.037f,-.022f),new Vector3(.009f,.035f,.021f),Color.black);
        for(int i=0;i<4;i++)Part("Микрофон",new Vector3(.126f,.041f+i*.006f,-.041f),new Vector3(.025f,.002f,.003f),Color.black);
        var cord=gameObject.AddComponent<LineRenderer>();cord.useWorldSpace=false;cord.positionCount=49;
        cord.sharedMaterial=new Material(Camaro.Surface(false));cord.sharedMaterial.color=charcoal;cord.widthMultiplier=.0023f;
        for(int i=0;i<49;i++){
            float t=i/48f,angle=t*12*Mathf.PI;
            cord.SetPosition(i,new Vector3(Mathf.Lerp(.078f,.129f,t)+.003f*Mathf.Sin(angle),-.018f-.065f*Mathf.Sin(t*Mathf.PI),-.043f+.003f*Mathf.Cos(angle)));
        }
        speaker=gameObject.AddComponent<AudioSource>();speaker.playOnAwake=false;speaker.spatialBlend=.65f;speaker.dopplerLevel=0;speaker.minDistance=1;speaker.maxDistance=15;
        for(int i=0;i<calls.Length;i++)calls[i]=Resources.Load<AudioClip>("Dispatch"+i);
    }
    public void Call(int stage){
        speaker.clip=calls[stage];
        if(speaker.clip)speaker.Play();
    }
    public void Suspend(bool paused){if(paused)speaker.Pause();else speaker.UnPause();}
    void Update(){if(indicator)indicator.color=speaker.isPlaying?receive:idle;}
}
