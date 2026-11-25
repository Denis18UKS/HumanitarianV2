#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "GameFramework/Pawn.h"
#include "GameFramework/HUD.h"
#include "GameFramework/SaveGame.h"
#include "NightMode.generated.h"

UCLASS()
class UTripSave : public USaveGame {
    GENERATED_BODY()
public:
    UPROPERTY() int32 Stage=0;
    UPROPERTY() TArray<int32> Status={0,0,0};
    UPROPERTY() TArray<float> Integrity={100,100,100};
    UPROPERTY() float Fuel=100;
    UPROPERTY() float Health=100;
    UPROPERTY() float Seconds=0;
    UPROPERTY() bool Lid=false;
    UPROPERTY() FVector Point=FVector::ZeroVector;
    UPROPERTY() float Yaw=0;
};

struct FDelivery {FString Name;float Mass,Fragility;};
UCLASS()
class ANightCar : public APawn {
    GENERATED_BODY()
public:
    ANightCar();
    void BeginPlay() override;
    void Tick(float Dt) override;
    FString Screen=TEXT("Меню"),Radio;
    TArray<FString> Names; TArray<FVector> Stops;
    TArray<FDelivery> Cargo;
    UPROPERTY() UTripSave* Trip=nullptr;
    float Speed=0,Limit=100;
    bool Inventory=false,Map=false;
    float Volume=.6f,Sensitivity=.3f;
    UPROPERTY() class UAudioComponent* Motor;
    float Mass() const;
    float Distance() const;
    FString Hint() const;
private:
    UPROPERTY() class UBoxComponent* Bounds;
    UPROPERTY() class UCameraComponent* Camera;
    UPROPERTY() TMap<FString,USceneComponent*> Parts;
    UPROPERTY() TArray<class USpotLightComponent*> Lamps;
    UPROPERTY() TArray<class UPointLightComponent*> Tail;
    UPROPERTY() TArray<UStaticMeshComponent*> Boxes;
    void Build();
    void LoadModel();
    void Restart(bool Restore);
    void Interact();
    void Store();
    void Sync();
    UStaticMeshComponent* Block(FVector P,FVector Size,FLinearColor Color,bool Car=false,bool Solid=false);
    bool BuildAttempted=false;
    bool Cabin=false,Headlights=true,Ready=false;
    float Roll=0,Look=0;
};
UCLASS()
class ANightHUD : public AHUD {
    GENERATED_BODY()
public: void DrawHUD() override;
};
UCLASS()
class ANightMode : public AGameModeBase {
    GENERATED_BODY()
public: ANightMode();
};
