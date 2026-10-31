using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireEscape
{
    public static partial class FEVisualUpgradeBuilder
    {
        private static void UpgradeEffects()
        {
            // The existing manager starts, pauses, and resets these systems as before.
            foreach(var ps in emergency.emergencyRoot.GetComponentsInChildren<ParticleSystem>(true))
            {
                bool smoke=ps.name=="Smoke" || ps.name.StartsWith("Door_Seep");
                float scale=ps.transform.parent.name=="Side_Fire"?.6f:1;
                ConfigureParticle(ps,smoke,scale,ps.name.StartsWith("Door_Seep"));
            }
            foreach(var door in department.GetComponentsInChildren<FEDoor>(true))
                if(door.explosion!=null)
                {
                    var ps=door.explosion;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                    var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=mats["Flame"];
                    var main=ps.main;main.maxParticles=120;main.startSize=new ParticleSystem.MinMaxCurve(.6f,1.2f);
                    main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.8f);
                    var emission=ps.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)65)});
                    Sheet(ps);GradientColor(ps,false);
                }
            var lights=new List<Light>();
            foreach(var l in emergency.emergencyLights)
            {
                if(l==null)continue; l.color=new Color(1,.38f,.075f);l.range=l.transform.parent.name=="Side_Fire"?4:6;
                l.shadows=l.transform.parent.name=="Lab_Fire"?LightShadows.Soft:LightShadows.None;
                l.shadowBias=.04f;l.shadowNormalBias=.18f;lights.Add(l);
                Transform parent=l.transform.parent;
                var sparks=Child("Rising_Embers",parent).gameObject.AddComponent<ParticleSystem>();
                sparks.transform.localPosition=Vector3.up*.35f;ConfigureSparks(sparks);
            }
            atmosphere.fireLights=lights.ToArray();
            Transform e=emergency.emergencyRoot.transform;
            // Local ceiling smoke avoids a uniform opaque screen across the whole level.
            CeilingSmoke(new Vector3(4.4f,2.64f,-3),new Vector3(2.8f,.10f,2.8f),e,.75f);
            CeilingSmoke(new Vector3(.4f,2.74f,8),new Vector3(2.4f,.06f,2.7f),e,.30f);
            CeilingSmoke(new Vector3(10,2.74f,12),new Vector3(3.4f,.07f,2.4f),e,.25f);
            // Soot is attached to the same emergency root, so restarting removes it.
            Quad("Burnt_Floor_Around_Lab_Fire",new Vector3(4.8f,.012f,-3),Quaternion.Euler(90,0,0),new Vector2(3.6f,2.8f),"Soot",e);
            Quad("Soot_Above_Lab_Fire",new Vector3(4.8f,3.291f,-3),Quaternion.Euler(-90,0,0),new Vector2(3.5f,3.2f),"Soot",e);
            Quad("Charred_Worktop",new Vector3(4.8f,1.008f,-3),Quaternion.Euler(90,0,0),new Vector2(1.45f,1.08f),"Soot",e);
            Quad("Exit2_Soot",new Vector3(-21.875f,1.75f,24),Quaternion.Euler(0,90,0),new Vector2(3,3),"Soot",e);
            // Retain every original blocking collider. Replace only the block-shaped visible rubble.
            foreach(var t in e.GetComponentsInChildren<Transform>(true).ToArray())
            {
                if(t.name.StartsWith("Falling_Rubble_") || t.name.StartsWith("Corridor_Debris_") || t.name=="Exit2_Debris_Blockage")
                    Rubble(t);
            }
            var damage=Child("Damaged_Ceiling_Section",e);damage.localPosition=new Vector3(11.7f,0,12.75f);
            var tile=Box("Hanging_Acoustic_Panel",new Vector3(0,2.86f,0),new Vector3(1.2f,.035f,.58f),"Ceiling",damage,.006f);
            tile.localRotation=Quaternion.Euler(22,12,-14);
            Rod("Bent_Ceiling_Tee",new Vector3(-.8f,3.26f,0),new Vector3(.6f,2.76f,.13f),.014f,"Steel",damage);
            // Everything hangs above the 1.8 m character; this is scenery, not a new obstacle.
        }

        private static void ConfigureParticle(ParticleSystem ps,bool smoke,float scale,bool seep)
        {
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=smoke?55:75;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=Color.white;main.startSize3D=!smoke;
            if(smoke)
            {
                main.startLifetime=new ParticleSystem.MinMaxCurve(seep?.9f:1.0f,seep?1.6f:1.7f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(.32f,.6f);
                main.startSize=new ParticleSystem.MinMaxCurve((seep?.3f:.55f)*scale,(seep?.5f:.95f)*scale);
                main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            }
            else
            {
                main.startLifetime=new ParticleSystem.MinMaxCurve(.4f,.8f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(.08f,.25f);
                main.startSizeX=new ParticleSystem.MinMaxCurve(.45f*scale,.95f*scale);
                main.startSizeY=new ParticleSystem.MinMaxCurve(.95f*scale,1.6f*scale);main.startSizeZ=1;
                main.startRotation=new ParticleSystem.MinMaxCurve(-.10f,.10f);
            }
            var emission=ps.emission;emission.rateOverTime=seep?5:smoke?9:36;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=0;velocity.y=smoke?.40f:.36f;velocity.z=0;
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,smoke?AnimationCurve.Linear(0,.7f,1,1.8f):new AnimationCurve(new Keyframe(0,.5f),new Keyframe(.24f,1),new Keyframe(1,.12f)));
            var rotation=ps.rotationOverLifetime;rotation.enabled=smoke;rotation.z=new ParticleSystem.MinMaxCurve(-.16f,.16f);
            var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=mats[smoke?"Smoke":"Flame"];
            r.renderMode=ParticleSystemRenderMode.Billboard;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            Sheet(ps);GradientColor(ps,smoke);
        }
        private static void Sheet(ParticleSystem ps)
        {
            var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Grid;
            sheet.numTilesX=4;sheet.numTilesY=4;sheet.animation=ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));
            sheet.startFrame=new ParticleSystem.MinMaxCurve(0,1);sheet.cycleCount=1;
        }
        private static void GradientColor(ParticleSystem ps,bool smoke)
        {
            var color=ps.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();
            gradient.SetKeys(smoke?new[]{new GradientColorKey(new Color(.28f,.29f,.30f),0),new GradientColorKey(new Color(.49f,.50f,.51f),1)}:
                new[]{new GradientColorKey(new Color(1,1,.9f),0),new GradientColorKey(new Color(1,.62f,.30f),.6f),new GradientColorKey(new Color(.5f,.2f,.06f),1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(smoke?.32f:.95f,.18f),new GradientAlphaKey(smoke?.23f:.7f,.65f),new GradientAlphaKey(0,1)});
            color.color=gradient;
        }
        private static void ConfigureSparks(ParticleSystem ps)
        {
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.localRotation=Quaternion.Euler(-90,0,0);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.95f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.6f,1.3f);main.startSize=new ParticleSystem.MinMaxCurve(.012f,.038f);
            main.maxParticles=30;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=10;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=16;shape.radius=.35f;
            var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=mats["Spark"];r.shadowCastingMode=ShadowCastingMode.Off;
            var color=ps.colorOverLifetime;color.enabled=true;var g=new Gradient();
            g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.25f,.01f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.1f),new GradientAlphaKey(0,1)});color.color=g;
        }
        private static void CeilingSmoke(Vector3 p,Vector3 extent,Transform parent,float alpha)
        {
            var ps=Child("Ceiling_Smoke",parent).gameObject.AddComponent<ParticleSystem>();ps.transform.localPosition=p;
            ConfigureParticle(ps,true,1,false);
            var main=ps.main;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.65f,1.2f);
            main.startLifetime=new ParticleSystem.MinMaxCurve(3,4);main.maxParticles=30;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=extent;
            var emission=ps.emission;emission.rateOverTime=4;
            var velocity=ps.velocityOverLifetime;velocity.x=.06f;velocity.y=.015f;velocity.z=.02f;
            main.startColor=new Color(1,1,1,alpha);
            var size=ps.sizeOverLifetime;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.85f,1,1.15f));
        }
        private static void Rubble(Transform old)
        {
            var renderer=old.GetComponent<MeshRenderer>();if(renderer==null)return;
            renderer.enabled=false;
            var p=Child("Detailed_Rubble_Shell",old);
            Vector3 s=old.localScale;p.localScale=new Vector3(1/s.x,1/s.y,1/s.z);
            // Every shell moves with the original collapse transform.
            bool tall=s.y>2;int rows=tall?5:2;
            for(int i=0;i<rows;i++)
            {
                float y=-s.y*.4f+s.y*.8f*(rows==1?0:(float)i/(rows-1));
                var chunk=Box("Fractured_Concrete",new Vector3((i%2-.5f)*s.x*.1f,y,0),new Vector3(s.x*.86f,s.y/rows*.85f,s.z*.94f),i%2==0?"Concrete":"Char",p,.035f);
                chunk.localRotation=Quaternion.Euler(i%2==0?9:-7,0,i%2==0?4:-6);
                Box("Exposed_Core",new Vector3(0,y+.04f,s.z*.46f),new Vector3(s.x*.70f,s.y/rows*.5f,.04f),"Char",p,.012f);
            }
            for(int i=0;i<3;i++)
                Rod("Twisted_Rebar",new Vector3(-s.x*.35f,-s.y*.3f,-s.z*.35f+i*s.z*.30f),new Vector3(s.x*.35f,s.y*.35f,-s.z*.20f+i*s.z*.26f),.016f,"Steel",p);
        }
        private static void SetupAudio()
        {
            var audio=new List<AudioSource>();
            foreach(var light in atmosphere.fireLights)
            {
                var a=Child("Local_Fire_Audio",light.transform.parent).gameObject.AddComponent<AudioSource>();
                a.playOnAwake=false;a.loop=true;a.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Source+"FireLoop.wav");
                a.spatialBlend=1;a.rolloffMode=AudioRolloffMode.Logarithmic;a.minDistance=1.3f;a.maxDistance=12;
                a.volume=.58f;a.dopplerLevel=0;audio.Add(a);
            }
            atmosphere.fireAudio=audio.ToArray();
            atmosphere.footsteps=Enumerable.Range(1,4).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Source+"Footstep"+i+".wav")).ToArray();
            atmosphere.structureCreak=AssetDatabase.LoadAssetAtPath<AudioClip>(Source+"StructureCreak.wav");
        }
    }
}
