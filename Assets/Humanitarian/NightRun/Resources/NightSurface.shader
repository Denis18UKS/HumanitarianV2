Shader "Humanitarian/NightSurface" {
 Properties { _MainTex("Display",2D)="white"{} _Mask("Sweep",2D)="white"{} _Mode("Mode",Float)=0 _Wet("Rain",Range(0,1))=1 }
 SubShader { Tags {"Queue"="Transparent+15" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass { Cull Off ZWrite Off Offset -1,-1 Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 sampler2D _MainTex,_Mask; float _Mode,_Wet;
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float3 n:NORMAL;};
 struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 local:TEXCOORD1;float3 world:TEXCOORD2;float3 normal:TEXCOORD3;float fog:TEXCOORD4;};
 V vert(A i){V o;o.pos=TransformObjectToHClip(i.p.xyz);o.local=i.p.xyz;o.world=TransformObjectToWorld(i.p.xyz);o.normal=TransformObjectToWorldNormal(i.n);o.uv=i.uv;o.fog=ComputeFogFactor(o.pos.z);return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*4375.5453);}
 float noise2(float2 p){float2 n=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(n),hash(n+float2(1,0)),f.x),lerp(hash(n+float2(0,1)),hash(n+1),f.x),f.y);}
 half4 frag(V i):SV_Target {
    if(_Mode>4.5)return half4(tex2D(_MainTex,i.uv).rgb,1);
    if(_Mode>3.5){float2 p=(i.uv-.5)*2;return half4(.62,.72,.83,saturate(1-abs(p.x))*saturate(1-abs(p.y))*.48);}
    if(_Mode>2.5){float2 p=(i.uv-.5)*2;float fade=saturate(1-dot(p,p));float cloud=noise2(i.uv*6+float2(_Time.y*.026,0));return half4(.23,.29,.36,fade*fade*(.18+.46*cloud)*_Wet);}
    if(_Mode>1.5){
      float2 pos=i.world.xz;float pool=smoothstep(.40,.72,noise2(pos*.32)+.10*noise2(pos*1.8));
      float2 cells=pos*2;float wave=frac(_Time.y*1.3+hash(floor(cells)));
      float ring=saturate(1-abs(length(frac(cells)-.5)-wave*.52)*27)*(1-wave);
      float rim=pow(1-saturate(dot(normalize(_WorldSpaceCameraPos-i.world),float3(0,1,0))),3);
      return half4(MixFog(float3(.12,.19,.26)+rim*.18+ring*.22,i.fog),_Wet*pool*(.4+.4*rim));
    }
    float2 coord=(_Mode>.5)?float2(i.local.x,i.local.y*1.8):i.local.xz+i.local.y*float2(.37,1);
    float2 cell=coord*78;float seed=hash(floor(cell));float2 point=frac(cell)-.5;
    point.x+=.22*sin(seed*21);point.y+=.16*sin(_Time.y*(.4+seed)+seed*6);
    float d=length(point*float2(1.24,.77));float bead=(1-smoothstep(.12,.27,d))*step(.47,seed);
    float mask=_Mode>.5?tex2D(_Mask,float2((i.local.x+.795)/1.59,(i.local.y-.851)/.407)).r:1;
    float shine=saturate(.5-point.x*3+point.y*2);
    return half4(MixFog(lerp(float3(.10,.14,.19),float3(.68,.78,.87),shine),i.fog),_Wet*mask*(bead*.71+.025));
 }
 ENDHLSL
 } } }
