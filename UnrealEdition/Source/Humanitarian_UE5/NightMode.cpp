#include "NightMode.h"
#include "Sound/SoundWaveProcedural.h"
#include "Components/AudioComponent.h"
#include "GameFramework/GameUserSettings.h"
#include "Camera/CameraComponent.h"
#include "Components/BoxComponent.h"
#include "Components/SpotLightComponent.h"
#include "Components/PointLightComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/DirectionalLight.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"
#include "EngineUtils.h"
#include "Components/LightComponent.h"
#include "GameFramework/PlayerController.h"
#include "Kismet/GameplayStatics.h"
#include "ProceduralMeshComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Serialization/MemoryReader.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "InputCoreTypes.h"

namespace {
const FString Slot=TEXT("NightRoute1");
const TCHAR* Axles[]={TEXT("Wheel_FL"),TEXT("Wheel_FR"),TEXT("Wheel_RL"),TEXT("Wheel_RR")};
FVector ReadPoint(FMemoryReader& R){
    float X,Y,Z;R<<X<<Y<<Z;return FVector(-X,Y,Z)*100;
}
void Paint(UMeshComponent* Mesh,FLinearColor Color){
    auto* Base=LoadObject<UMaterialInterface>(nullptr,TEXT("/Engine/BasicShapes/BasicShapeMaterial.BasicShapeMaterial"));
    if(Base){auto* Mat=UMaterialInstanceDynamic::Create(Base,Mesh);Mat->SetVectorParameterValue(TEXT("Color"),Color);Mesh->SetMaterial(0,Mat);}
}
}
ANightMode::ANightMode(){DefaultPawnClass=ANightCar::StaticClass();HUDClass=ANightHUD::StaticClass();}
ANightCar::ANightCar(){
    PrimaryActorTick.bCanEverTick=true;
    Bounds=CreateDefaultSubobject<UBoxComponent>(TEXT("Vehicle"));SetRootComponent(Bounds);
    Bounds->InitBoxExtent(FVector(245,93,40));Bounds->SetCollisionProfileName(TEXT("Pawn"));
    Camera=CreateDefaultSubobject<UCameraComponent>(TEXT("Camera"));Camera->SetupAttachment(Bounds);
}
UStaticMeshComponent* ANightCar::Block(FVector P,FVector Size,FLinearColor Color,bool Car,bool Solid){
    AActor* Owner=Car?this:GetWorld()->SpawnActor<AActor>();
    auto* Mesh=NewObject<UStaticMeshComponent>(Owner);
    Owner->AddInstanceComponent(Mesh);
    if(Car)Mesh->SetupAttachment(Bounds);else Owner->SetRootComponent(Mesh);
    Mesh->SetMobility(EComponentMobility::Movable);
    Mesh->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Cube.Cube")));
    Mesh->SetCollisionEnabled(Solid?ECollisionEnabled::QueryAndPhysics:ECollisionEnabled::NoCollision);
    if(Solid)Mesh->SetCollisionResponseToAllChannels(ECR_Block);
    Mesh->RegisterComponent();Mesh->SetRelativeLocation(P);Mesh->SetWorldScale3D(Size/100);Paint(Mesh,Color);
    return Mesh;
}
void ANightCar::LoadModel(){
    TArray<uint8> Bytes;
    if(!FFileHelper::LoadFileToArray(Bytes,*(FPaths::ProjectContentDir()/TEXT("Route/camaro.bytes"))))return;
    FMemoryReader R(Bytes);int32 Count;R<<Count;
    for(int32 N=0;N<Count;N++){
        ANSICHAR Name[33]={};R.Serialize(Name,32);FString Group=ANSI_TO_TCHAR(Name);
        uint8 C[3];R.Serialize(C,3);FVector Pivot=ReadPoint(R);
        auto*& Parent=Parts.FindOrAdd(Group);
        if(!Parent){
            Parent=NewObject<USceneComponent>(this);AddInstanceComponent(Parent);Parent->SetupAttachment(Bounds);Parent->RegisterComponent();Parent->SetRelativeLocation(Pivot-FVector(0,0,60));
        }
        int32 Size;R<<Size;
        if(Size<=0||Size%3||Size>(R.TotalSize()-R.Tell())/12)return;
        TArray<FVector> V,Normals;TArray<int32> Idx;V.Reserve(Size);Idx.Reserve(Size);
        for(int32 I=0;I<Size;I++){V.Add(ReadPoint(R));Idx.Add(I);}
        for(int32 I=0;I<Size;I+=3){
            FVector Normal=FVector::CrossProduct(V[I+2]-V[I],V[I+1]-V[I]).GetSafeNormal();
            Normals.Add(Normal);Normals.Add(Normal);Normals.Add(Normal);
        }
        auto* Mesh=NewObject<UProceduralMeshComponent>(this);AddInstanceComponent(Mesh);Mesh->SetupAttachment(Parent);Mesh->RegisterComponent();
        Mesh->CreateMeshSection_LinearColor(0,V,Idx,Normals,TArray<FVector2D>(),TArray<FLinearColor>(),TArray<FProcMeshTangent>(),false);
        Paint(Mesh,FLinearColor(FColor(C[0],C[1],C[2])));
    }
}
void ANightCar::Build(){
    FString Json;TSharedPtr<FJsonObject> Data;
    if(!FFileHelper::LoadFileToString(Json,*(FPaths::ProjectContentDir()/TEXT("Route/route.json")))||!FJsonSerializer::Deserialize(TJsonReaderFactory<>::Create(Json),Data))return;
    auto Pos=[](TSharedPtr<FJsonObject> J){return FVector(J->GetNumberField(TEXT("z")),J->GetNumberField(TEXT("x")),0)*100;};
    for(auto V:Data->GetArrayField(TEXT("stops"))){auto J=V->AsObject();Names.Add(J->GetStringField(TEXT("name")));Stops.Add(Pos(J));}
    for(auto V:Data->GetArrayField(TEXT("cargo"))){auto J=V->AsObject();Cargo.Add({J->GetStringField(TEXT("name")),(float)J->GetNumberField(TEXT("kg")),(float)J->GetNumberField(TEXT("fragility"))});}
    Limit=Data->GetNumberField(TEXT("capacity"));
    Block(FVector(28000,0,-40),FVector(140000,120000,70),FLinearColor(.075,.105,.085));
    const auto& Road=Data->GetArrayField(TEXT("road"));
    for(int I=1;I<Road.Num();I++){
        auto A=Pos(Road[I-1]->AsObject()),B=Pos(Road[I]->AsObject());auto Dir=(B-A).GetSafeNormal();
        Block((A+B)*.5,FVector((B-A).Size()+200,900,4),FLinearColor(.14,.15,.18))->SetWorldRotation(Dir.Rotation());
        for(float D=200;D<(B-A).Size();D+=1400)Block(A+Dir*D+FVector(0,0,4),FVector(300,15,2),FLinearColor(.52,.53,.5))->SetWorldRotation(Dir.Rotation());
    }
    Block(FVector(18500,0,70),FVector(200,1400,140),FLinearColor(.67,.33,.12),false,true);
    for(auto S:Stops){
        for(int I=0;I<4;I++)Block(S+FVector(I/2*1800-800,I%2?1400:-1400,250),FVector(900,800,500),FLinearColor(.29,.23,.2),false,true);
        Block(S+FVector(0,500,150),FVector(45,45,300),FLinearColor(.88,.63,.18));
    }
    auto* Moon=GetWorld()->SpawnActor<ADirectionalLight>();Moon->SetActorRotation(FRotator(-50,-35,0));Moon->GetLightComponent()->SetIntensity(2.0f);Moon->GetLightComponent()->SetLightColor(FLinearColor(.60,.73,1));
    LoadModel();
    for(int I=0;I<2;I++){
        auto* Light=NewObject<USpotLightComponent>(this);AddInstanceComponent(Light);Light->SetupAttachment(Bounds);Light->RegisterComponent();
        Light->SetRelativeLocation(FVector(244,I?68:-68,0));Light->SetRelativeRotation(FRotator(-5,0,0));Light->SetIntensity(95000);Light->SetOuterConeAngle(29);Light->SetAttenuationRadius(7500);Lamps.Add(Light);
        auto* Brake=NewObject<UPointLightComponent>(this);AddInstanceComponent(Brake);Brake->SetupAttachment(Bounds);Brake->RegisterComponent();
        Brake->SetRelativeLocation(FVector(-248,I?62:-62,5));Brake->SetLightColor(FLinearColor::Red);Brake->SetAttenuationRadius(210);Tail.Add(Brake);
    }
    for(int I=0;I<3;I++)Boxes.Add(Block(FVector(-205,(I-1)*43,1),FVector(42,37,28),I==1?FLinearColor::White:FLinearColor(.49,.34,.18),true));
    Ready=Parts.Contains(TEXT("Trunk"))&&Stops.Num()==5&&Cargo.Num()==3;
}
void ANightCar::BeginPlay(){
    Super::BeginPlay();
    TArray<uint8> Wav;
    if(FFileHelper::LoadFileToArray(Wav,*(FPaths::ProjectContentDir()/TEXT("Route/idle.wav")))&&Wav.Num()>44){
        auto* Tone=NewObject<USoundWaveProcedural>(this);Tone->SetSampleRate(22050);Tone->NumChannels=1;Tone->Duration=INDEFINITELY_LOOPING_DURATION;
        TArray<uint8> Pcm;Pcm.Append(Wav.GetData()+44,Wav.Num()-44);
        Tone->OnSoundWaveProceduralUnderflow.BindLambda([Pcm](USoundWaveProcedural* Sound,int32){Sound->QueueAudio(Pcm.GetData(),Pcm.Num());});
        Tone->QueueAudio(Pcm.GetData(),Pcm.Num());
        Motor=NewObject<UAudioComponent>(this);AddInstanceComponent(Motor);Motor->SetupAttachment(Bounds);Motor->RegisterComponent();Motor->SetSound(Tone);Motor->SetVolumeMultiplier(0);Motor->Play();
    }
    Trip=NewObject<UTripSave>(this);
    SetActorLocation(FVector(0,0,60));
}
void ANightCar::Restart(bool Restore){
    if(!Ready){
        if(BuildAttempted)return;
        BuildAttempted=true;
        Build();
        if(!Ready){Screen=TEXT("Ошибка загрузки ресурсов");return;}
    }
    auto* Prior=Restore?Cast<UTripSave>(UGameplayStatics::LoadGameFromSlot(Slot,0)):nullptr;
    Trip=Prior?Prior:NewObject<UTripSave>(this);
    if(!Prior)Trip->Point=FVector(0,0,60);
    SetActorLocationAndRotation(Trip->Point,FRotator(0,Trip->Yaw,0));Speed=0;Screen=TEXT("Рейс");Sync();
    Radio=TEXT("Диспетчер • Проверь груз и следуй к отмеченному пункту.");if(!Prior)Store();
}
void ANightCar::Store(){Trip->Point=GetActorLocation();Trip->Yaw=GetActorRotation().Yaw;UGameplayStatics::SaveGameToSlot(Trip,Slot,0);}
void ANightCar::Sync(){
    for(int I=0;I<3;I++)Boxes[I]->SetVisibility(Trip->Status[I]==1);
    Parts[TEXT("Trunk")]->SetRelativeRotation(FRotator(Trip->Lid?-58:0,0,0));
    for(auto* L:Lamps)L->SetVisibility(Headlights);
}
float ANightCar::Mass() const {float M=0;for(int I=0;I<Cargo.Num();I++)if(Trip->Status[I]==1)M+=Cargo[I].Mass;return M;}
float ANightCar::Distance() const {return FVector::Dist2D(GetActorLocation(),Stops[Trip->Stage])/100;}
FString ANightCar::Hint() const {
    if(Distance()>10)return TEXT("Следуй к отметке • M маршрут");
    if(FMath::Abs(Speed)>100)return TEXT("Остановись");
    if(Trip->Stage==4)return TEXT("E • Закончить рейс");
    if(!Trip->Lid)return TEXT("E • Открыть багажник");
    if(Trip->Stage>0)return TEXT("E • Передать ")+Cargo[Trip->Stage-1].Name;
    int I=Trip->Status.IndexOfByKey(0);return I==INDEX_NONE?TEXT("E • Закрыть багажник и выехать"):TEXT("E • Загрузить ")+Cargo[I].Name;
}
void ANightCar::Interact(){
    if(Distance()>10||FMath::Abs(Speed)>100)return;
    if(Trip->Stage==4){
        Screen=Trip->Integrity.ContainsByPredicate([](float Q){return Q<50;})?TEXT("Частичный успех"):TEXT("Рейс завершён");return;
    }
    if(!Trip->Lid)Trip->Lid=true;
    else if(!Trip->Stage){
        int I=Trip->Status.IndexOfByKey(0);
        if(I==INDEX_NONE){Trip->Stage++;Trip->Lid=false;}
        else if(Mass()+Cargo[I].Mass<=Limit)Trip->Status[I]=1;
    }else{
        int I=Trip->Stage-1;if(Trip->Status[I]!=1)return;
        Trip->Status[I]=2;Trip->Stage++;Trip->Lid=false;
        Radio=I==0?TEXT("Диспетчер • Мост закрыт. На развилке держись правой дороги."):TEXT("Диспетчер • Следующий пункт: ")+Names[Trip->Stage];
    }
    Sync();Store();
}
void ANightCar::Tick(float Dt){
    Super::Tick(Dt);auto* PC=Cast<APlayerController>(GetController());if(!PC)return;
    auto Key=[PC](FKey K){return PC->WasInputKeyJustPressed(K);};
    auto Held=[PC](FKey K){return PC->IsInputKeyDown(K);};
    if(Key(EKeys::Escape)){if(Screen==TEXT("Рейс"))Screen=TEXT("Пауза");else if(Screen==TEXT("Пауза"))Screen=TEXT("Рейс");}
    if(Screen!=TEXT("Рейс")){
        if(Key(EKeys::V))Volume=Volume>=1?0:FMath::Min(1.f,Volume+.25f);
        if(Key(EKeys::B))Sensitivity=Sensitivity>=.9f?.15f:Sensitivity+.15f;
        if(Key(EKeys::G)){auto* Settings=GEngine->GetGameUserSettings();if(Settings){Settings->SetOverallScalabilityLevel((Settings->GetOverallScalabilityLevel()+1)%4);Settings->ApplySettings(false);}}
        if(Key(EKeys::N)||Key(EKeys::Enter))Restart(false);
        if(Key(EKeys::R)&&UGameplayStatics::DoesSaveGameExist(Slot,0))Restart(true);
        if(Key(EKeys::M))Screen=TEXT("Меню");
        if(Key(EKeys::Q))PC->ConsoleCommand(TEXT("quit"));
    }else{
        if(FMath::Abs(Speed)<50){
            if(Key(EKeys::H)){auto* P=Parts.Find(TEXT("Hood"));if(P)(*P)->SetRelativeRotation(FRotator(FMath::IsNearlyZero((*P)->GetRelativeRotation().Pitch)?55:0,0,0));}
            if(Key(EKeys::O)){for(auto Side:{TEXT("Door_L_hinge"),TEXT("Door_R_hinge")})if(auto* P=Parts.Find(Side))(*P)->SetRelativeRotation(FRotator(0,FMath::IsNearlyZero((*P)->GetRelativeRotation().Yaw)?(FString(Side).Contains(TEXT("_L"))?58:-58):0,0));}
        }
        if(Key(EKeys::E))Interact();
        if(Key(EKeys::R))Restart(true);
        if(Key(EKeys::C)){Cabin=!Cabin;Look=0;}
        if(Key(EKeys::L)){Headlights=!Headlights;Sync();}
        if(Key(EKeys::M))Map=!Map;if(Key(EKeys::Tab))Inventory=!Inventory;
        float Throttle=float(Held(EKeys::W))-float(Held(EKeys::S));
        float Steer=float(Held(EKeys::D))-float(Held(EKeys::A));
        bool Braking=Held(EKeys::SpaceBar)||Trip->Lid||!Trip->Stage;
        float Target=Braking?0:Throttle*(Throttle>0?2500:750);
        float Accel=Braking||Speed*Throttle<0?1250:Throttle==0?145:550/(1+Mass()/1450);
        Speed=FMath::FInterpConstantTo(Speed,Target,Dt,Accel);
        AddActorWorldRotation(FRotator(0,Steer*FMath::Clamp(Speed/750,-1.f,1.f)*65*Dt,0));
        FHitResult Hit;auto Before=GetActorLocation();
        AddActorWorldOffset(GetActorForwardVector()*Speed*Dt,true,&Hit);
        if(Hit.bBlockingHit){
            float Impact=FMath::Abs(Speed)/100;Speed=0;
            if(Impact>4){Trip->Health=FMath::Max(0.f,Trip->Health-Impact*1.15f);
                for(int I=0;I<3;I++)if(Trip->Status[I]==1)Trip->Integrity[I]=FMath::Max(0.f,Trip->Integrity[I]-Impact*Cargo[I].Fragility);}
        }
        float Travel=FVector::Dist2D(Before,GetActorLocation())/100;
        Trip->Fuel=FMath::Max(0.f,Trip->Fuel-Travel*.075f);Trip->Seconds+=Dt;
        Roll=FMath::Fmod(Roll+Speed*Dt/33*180/PI,360.f);
        for(int I=0;I<4;I++)if(auto* P=Parts.Find(Axles[I]))(*P)->SetRelativeRotation(FRotator(Roll,I<2?Steer*28:0,0));
        if(auto* P=Parts.Find(TEXT("Steering_Wheel")))(*P)->SetRelativeRotation(FRotator(0,0,Steer*80));
        for(auto* L:Tail)L->SetIntensity(Braking||Throttle*Speed<0?380:55);
        if(Trip->Fuel<=0||Trip->Health<=0)Screen=TEXT("Рейс прерван");
    }
    if(Held(EKeys::RightMouseButton)){float X,Y;PC->GetInputMouseDelta(X,Y);Look=FMath::Clamp(Look+X*Sensitivity,-85.f,85.f);}
    if(Motor){Motor->SetVolumeMultiplier(Screen==TEXT("Рейс")?Volume:0);Motor->SetPitchMultiplier(.8f+FMath::Abs(Speed)/2200);}
    Camera->SetRelativeLocation(Cabin?FVector(3,-40,42):FRotator(0,Look,0).RotateVector(FVector(-660,0,265)));
    Camera->SetRelativeRotation(FRotator(Cabin?0:-21,Look,0));
}
void ANightHUD::DrawHUD(){
    Super::DrawHUD();auto* P=Cast<ANightCar>(GetOwningPawn());if(!P||!P->Trip)return;
    float S=FMath::Clamp(Canvas->SizeY/800.f,.72f,1.65f),Y=26;
    auto Line=[&](const FString& T,FLinearColor C=FLinearColor::White){DrawText(T,C,32*S,Y*S,GEngine->GetMediumFont(),S);Y+=25;};
    DrawRect(FLinearColor(.022,.035,.055,.87),16*S,16*S,620*S,(P->Inventory||P->Screen!=TEXT("Рейс")?390:235)*S);
    Line(TEXT("HUMANITARIAN    /    Ночной рейс"),FLinearColor(.93,.73,.39));
    Line(P->Screen);
    if(P->Screen==TEXT("Меню")){
        Line(TEXT("Enter • Начать новый рейс"));
        if(UGameplayStatics::DoesSaveGameExist(Slot,0))Line(TEXT("R • Продолжить с контрольной точки"));
        Line(FString::Printf(TEXT("V • Громкость %.0f%%     B • Обзор %.2f     G • Качество"),P->Volume*100,P->Sensitivity));
        Line(TEXT("Q • Выход"));return;
    }
    if(P->Screen==TEXT("Ошибка загрузки ресурсов")){Line(TEXT("Проверьте файлы Content/Route. Q • Выход"));return;}
    if(P->Names.Num()!=5)return;
    if(P->Screen==TEXT("Рейс")){
        Line(FString::Printf(TEXT("%s • %.0f м"),*P->Names[P->Trip->Stage],P->Distance()));
        float Bearing=FMath::FindDeltaAngleDegrees(P->GetActorRotation().Yaw,(P->Stops[P->Trip->Stage]-P->GetActorLocation()).Rotation().Yaw);
        Line(FString::Printf(TEXT("Курс на цель: %+.0f°    Передано %d/3"),Bearing,FMath::Max(0,P->Trip->Stage-1)));
        Line(P->Hint());Line(P->Radio);Line(TEXT("C камера   L свет   Tab груз   M маршрут   Esc пауза"));
        if(P->Map)Line(TEXT("Склад → двор → правый объезд → медпункт → вода → гараж"));
    }else{
        Line(TEXT("N • Новый рейс     R • Контрольная точка     Q • Выход"));
        Line(TEXT("Esc • Снять паузу     M • Меню"));
        Line(FString::Printf(TEXT("V громкость %.0f%%   B обзор %.2f   G качество"),P->Volume*100,P->Sensitivity));
        Line(TEXT("W/S газ и тормоз с реверсом • A/D руль • Space ручник • E передача"));
    }
    Line(FString::Printf(TEXT("%.0f км/ч    Топливо %.0f%%    Кузов %.0f%%    %.1f мин"),FMath::Abs(P->Speed)*.036,P->Trip->Fuel,P->Trip->Health,P->Trip->Seconds/60));
    if(P->Inventory||P->Screen!=TEXT("Рейс")){
        for(int I=0;I<P->Cargo.Num();I++)Line(FString::Printf(TEXT("%s • %.0f кг • %.0f%% • %s"),*P->Cargo[I].Name,P->Cargo[I].Mass,P->Trip->Integrity[I],P->Trip->Status[I]==2?TEXT("передано"):P->Trip->Status[I]==1?TEXT("в машине"):TEXT("склад")));
        Line(FString::Printf(TEXT("Багажник %.0f / %.0f кг"),P->Mass(),P->Limit));
    }
}
