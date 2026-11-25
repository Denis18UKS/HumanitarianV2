using System;
using UnityEngine;

[Serializable] public class GarageAccount {
    public int credit=60,items;
    public string lastPayment;
}
public sealed partial class NightRun {
    const string GarageKey="NightRoute.Garage.1";
    GarageAccount account;
    static readonly string[] Equipment={"Дождевые шины","Усиленный радиатор","Быстрые дворники"};
    static readonly string[] Benefits={"Лучше сцепление на мокром асфальте","Медленнее нагрев при утечке","Быстрее очищают стекло в ливень"};
    static readonly int[] Cost={60,75,45};
    static readonly Vector3[] Pumps={new Vector3(70,0,234),new Vector3(9,0,473)};
    bool Owned(int n)=>(account.items&(1<<n))!=0;
    void SaveGarage(){PlayerPrefs.SetString(GarageKey,JsonUtility.ToJson(account));PlayerPrefs.Save();}
    bool AtPump(){foreach(var p in Pumps)if(Vector3.Distance(car.body.position,p)<8)return true;return false;}
    bool AtGarage()=>Vector3.Distance(car.body.position,route.stops[0].Pos)<12||Vector3.Distance(car.body.position,route.stops[4].Pos)<12;
    void Pay(){
        if(account.lastPayment==trip.tripId)return;
        account.credit+=Array.Exists(trip.quality,q=>q<50)?65:100;account.lastPayment=trip.tripId;SaveGarage();
    }
    void Repair(){
        trip.health=100;trip.heat=0;trip.tyreFault=false;trip.stalled=false;Save();
    }
    void Workshop(){
        Label(72,54,1100,65,"Мастерская Ghost Camaro",38,Cream,true);
        Label(74,125,1050,45,$"Запас деталей: {account.credit}   •   За завершённый рейс: 65–100",21,Gold);
        for(int i=0;i<3;i++){
            float x=74+i*384;Panel(new Rect(x,210,352,240),new Color(.085f,.125f,.14f));
            Label(x+16,230,320,50,Equipment[i],23,Cream,true);
            Label(x+16,288,320,66,Benefits[i],16,Cream);
            bool canBuy=!Owned(i)&&account.credit>=Cost[i];GUI.enabled=canBuy;
            if(Button(x+16,378,318,Owned(i)?"Установлено":$"Установить • {Cost[i]} деталей",canBuy)){
                account.credit-=Cost[i];account.items|=1<<i;SaveGarage();
            }GUI.enabled=true;
        }
        if(returnPage=="Рейс"&&Button(74,474,350,"Обслужить автомобиль"))Repair();
        if(Button(74,584,350,"Вернуться",true))page=returnPage;
    }
    // Broken pavement is visible on the bypass. Entering it slowly spares the wheel.
    bool onBrokenPavement;
    static readonly Vector3 BrokenPavement=new Vector3(45,0,257);
    void MechanicalStep(){
        float dt=Time.fixedDeltaTime;
        bool crossing=Vector3.Distance(car.body.position,BrokenPavement)<3.8f;
        if(crossing&&!onBrokenPavement&&car.body.linearVelocity.magnitude>10)
            Damage(car.body.linearVelocity.magnitude*.9f);
        onBrokenPavement=crossing;
        car.wheels[0].radius=trip.tyreFault?.285f:.33f;
        float stress=Mathf.Abs(gas)*Mathf.Clamp01((72-trip.health)/55f);
        trip.heat=Mathf.Clamp(trip.heat+dt*(stress*(Owned(1)?2:4)-(Mathf.Abs(gas)<.2f?3.5f:.25f)),0,100);
        if(trip.heat>=99)trip.stalled=true;
        if(trip.stalled&&trip.heat<42)trip.stalled=false;
        car.engineFactor=trip.stalled?0:Mathf.Lerp(.38f,1,trip.health/100)*Mathf.Lerp(1,.65f,trip.heat/100);
        float grip=weatherMode>=2?.73f:weatherMode==1?.89f:1;
        if(Owned(0))grip=Mathf.Lerp(grip,1,.75f);
        for(int i=0;i<4;i++){
            var w=car.wheels[i];var friction=w.sidewaysFriction;friction.stiffness=grip*(trip.tyreFault&&i==0?.56f:1);w.sidewaysFriction=friction;
        }
    }
}
