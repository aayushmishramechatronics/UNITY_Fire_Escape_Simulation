using UnityEngine;

namespace FireEscape
{
    public static partial class FEVisualUpgradeBuilder
    {
        private static void BuildLab()
        {
            var lab=Child("Detailed_Chemistry_Laboratory",art);
            int id=0;
            foreach(float x in new[]{-4.5f,4.5f})foreach(float z in new[]{-6.5f,-3f})
            {
                var bench=Child("Laboratory_Workstation_"+(++id),lab);bench.localPosition=new Vector3(x,0,z);
                Workstation(bench,id);
            }
            FumeHood(new Vector3(-4.6f,0,-9.2f),180,lab);
            SinkUnit(new Vector3(4.7f,0,-9.15f),180,lab);
            TallCabinet(new Vector3(-7.42f,0,-5.8f),-90,lab);
            TallCabinet(new Vector3(7.42f,0,-6.6f),90,lab);
            Stool(new Vector3(-4.7f,0,-4.9f),lab);
            Stool(new Vector3(4.7f,0,-4.9f),lab);
            Poster("SafetyPoster",new Vector3(-7.88f,1.9f,-1),Quaternion.Euler(0,-90,0),.73f,lab);
            // Exit-facing wall carries a restrained university identity above the existing plan.
            Label("NORTHBRIDGE UNIVERSITY",new Vector3(-4,3.22f,1.75f),Quaternion.identity,3.5f,.10f,new Color(.13f,.25f,.31f),lab);
            var shower=Child("Emergency_Shower",lab);shower.localPosition=new Vector3(7.55f,0,.3f);
            Rod("Supply_Pipe",new Vector3(0,.2f,0),new Vector3(0,2.85f,0),.026f,"Steel",shower);
            Rod("Shower_Arm",new Vector3(0,2.85f,0),new Vector3(-.48f,2.85f,0),.026f,"Steel",shower);
            Rod("Shower_Head",new Vector3(-.48f,2.78f,0),new Vector3(-.48f,2.84f,0),.12f,"Steel",shower);
            Rod("Pull_Rod",new Vector3(-.12f,2.75f,-.1f),new Vector3(-.12f,1.6f,-.1f),.008f,"Steel",shower);
            Box("Pull_Handle",new Vector3(-.12f,1.59f,-.1f),new Vector3(.17f,.035f,.045f),"Red",shower,.008f);
            // Vent registers and detector make the ceiling read as building services.
            foreach(var p in new[]{new Vector3(-3,3.27f,-5),new Vector3(3,3.27f,-1)})
            {
                Box("Vent_Frame",p,new Vector3(.62f,.04f,.62f),"White",lab,.015f);
                for(int i=0;i<9;i++)Box("Vent_Slot",p+new Vector3(0,-.024f,-.24f+i*.06f),new Vector3(.5f,.008f,.018f),"Black",lab,.002f);
            }
            Rod("Smoke_Detector",new Vector3(0,3.21f,-4),new Vector3(0,3.28f,-4),.08f,"White",lab);
            Box("Detector_Indicator",new Vector3(.035f,3.2f,-4),new Vector3(.015f,.009f,.015f),"Red",lab,.003f);
        }

        private static void Workstation(Transform p,int id)
        {
            Box("Epoxy_Worktop",new Vector3(0,.95f,0),new Vector3(2.8f,.1f,1.2f),"Counter",p,.024f);
            Box("Cabinet_Plinth",new Vector3(0,.10f,0),new Vector3(2.5f,.18f,.91f),"Black",p,.006f);
            Box("Cabinet_Carcass",new Vector3(0,.53f,0),new Vector3(2.62f,.72f,1.02f),"White",p,.018f);
            for(int side=-1;side<=1;side+=2)
            {
                for(int k=0;k<3;k++)
                {
                    float x=-.88f+k*.88f;
                    Box("Inset_Door",new Vector3(x,.46f,side*.521f),new Vector3(.842f,.53f,.03f),id%2==0?"Blue":"White",p,.009f);
                    Box("Drawer_Front",new Vector3(x,.8f,side*.521f),new Vector3(.842f,.13f,.03f),"White",p,.008f);
                    Rod("Drawer_Pull",new Vector3(x-.15f,.8f,side*.561f),new Vector3(x+.15f,.8f,side*.561f),.012f,"Steel",p);
                    Rod("Door_Pull",new Vector3(x+.25f,.54f,side*.561f),new Vector3(x+.25f,.72f,side*.561f),.012f,"Steel",p);
                }
            }
            // Reagent shelf doubles as an electrical service spine.
            Box("Service_Spine",new Vector3(0,1.14f,.32f),new Vector3(2.25f,.27f,.11f),"Blue",p,.012f);
            Box("Reagent_Shelf",new Vector3(0,1.56f,.31f),new Vector3(2.35f,.05f,.32f),"Steel",p,.009f);
            foreach(float x in new[]{-1.05f,1.05f})Rod("Shelf_Upright",new Vector3(x,1,.32f),new Vector3(x,1.57f,.32f),.023f,"Steel",p);
            for(int i=0;i<3;i++)
            {
                var b=Child("Amber_Reagent_Bottle",p);b.localPosition=new Vector3(-.68f+i*.63f,1.585f,.31f);
                Vessel(b,false,.72f,"Bottle");
                Box("Bottle_Label",new Vector3(0,.125f,-.080f),new Vector3(.112f,.1f,.004f),"Paper",b,.001f);
                Label(new[]{"NaCl","BUFFER","SAMPLE"}[i],new Vector3(0,.13f,-.083f),Quaternion.identity,.095f,.025f,Color.black,b);
            }
            for(int i=0;i<2;i++)
            {
                float x=-.45f+i*.7f;
                Box("Outlet",new Vector3(x,1.16f,.257f),new Vector3(.16f,.1f,.014f),"White",p,.008f);
                foreach(float dx in new[]{-.028f,.028f})Box("Socket",new Vector3(x+dx,1.16f,.247f),new Vector3(.014f,.034f,.007f),"Black",p,.003f);
            }
            var flask=Child("Borosilicate_Flask",p);flask.localPosition=new Vector3(-.65f,1.002f,-.22f);Vessel(flask,true,1,"Glass");
            Rod("Solution",new Vector3(-.65f,1.013f,-.22f),new Vector3(-.65f,1.084f,-.22f),.094f,"Liquid",p);
            var beaker=Child("Graduated_Beaker",p);beaker.localPosition=new Vector3(-1.04f,1.003f,-.19f);
            LatheObject("Beaker",new[]{new Vector2(0,0),new Vector2(.085f,0),new Vector2(.09f,.008f),new Vector2(.09f,.20f),new Vector2(.083f,.205f),new Vector2(.08f,.195f),new Vector2(.08f,.015f),new Vector2(0,.015f)},"Glass",beaker);
            for(int i=0;i<4;i++)Box("Graduation",new Vector3(0,.05f+i*.035f,-.088f),new Vector3(i%2==0?.035f:.02f,.003f,.002f),"White",beaker,.0005f);
            Instrument(new Vector3(.72f,1.002f,-.14f),p,id);
            Box("Lab_Notebook",new Vector3(.05f,1.013f,-.30f),new Vector3(.24f,.022f,.3f),"Blue",p,.006f);
            Box("Notebook_Pages",new Vector3(.05f,1.027f,-.30f),new Vector3(.225f,.008f,.28f),"Paper",p,.002f);
            Rod("Pen",new Vector3(.23f,1.012f,-.38f),new Vector3(.25f,1.012f,-.20f),.004f,"Black",p);
        }
        private static void Instrument(Vector3 position,Transform parent,int id)
        {
            var p=Child("Digital_Laboratory_Instrument",parent);p.localPosition=position;
            Box("Rounded_Enclosure",new Vector3(0,.075f,0),new Vector3(.5f,.15f,.42f),"White",p,.03f);
            Box("Front_Bezel",new Vector3(0,.077f,-.207f),new Vector3(.44f,.103f,.017f),"Black",p,.012f);
            Box("LCD",new Vector3(-.06f,.077f,-.218f),new Vector3(.22f,.066f,.004f),"Screen",p,.004f);
            Label(id%2==0?"24.8 C":"0.000 g",new Vector3(-.06f,.078f,-.222f),Quaternion.identity,.17f,.036f,new Color(.6f,.95f,.77f),p);
            for(int i=0;i<2;i++)Rod("Control_Knob",new Vector3(.10f+i*.08f,.075f,-.22f),new Vector3(.10f+i*.08f,.075f,-.245f),.019f,"Steel",p);
            if(id%2==0)
            {
                Rod("Ceramic_Hotplate",new Vector3(0,.153f,0),new Vector3(0,.17f,0),.175f,"Black",p);
                var f=Child("Sample_Flask",p);f.localPosition=new Vector3(0,.17f,0);Vessel(f,true,.85f,"Glass");
            }
            else
            {
                Rod("Weighing_Pan",new Vector3(0,.153f,0),new Vector3(0,.17f,0),.17f,"Steel",p);
                foreach(float x in new[]{-.2f,.2f})Box("Balance_Glass",new Vector3(x,.33f,0),new Vector3(.004f,.32f,.35f),"Glass",p,.001f);
                Box("Balance_Back_Glass",new Vector3(0,.33f,.175f),new Vector3(.4f,.32f,.004f),"Glass",p,.001f);
                Box("Balance_Top",new Vector3(0,.5f,0),new Vector3(.41f,.02f,.37f),"Steel",p,.005f);
            }
            foreach(float x in new[]{-.18f,.18f})foreach(float z in new[]{-.14f,.14f})Rod("Instrument_Foot",new Vector3(x,-.012f,z),new Vector3(x,0,z),.024f,"Rubber",p);
        }
        private static void Vessel(Transform parent,bool flask,float scale,string material)
        {
            Vector2[] profile=flask?
                new[]{new Vector2(0,0),new Vector2(.11f,.005f),new Vector2(.117f,.026f),new Vector2(.11f,.085f),new Vector2(.038f,.22f),new Vector2(.034f,.31f),new Vector2(.039f,.315f),new Vector2(.039f,.326f),new Vector2(.029f,.326f),new Vector2(.027f,.30f),new Vector2(.03f,.223f),new Vector2(.102f,.08f),new Vector2(.102f,.018f),new Vector2(0,.018f)}:
                new[]{new Vector2(0,0),new Vector2(.102f,.005f),new Vector2(.107f,.02f),new Vector2(.107f,.22f),new Vector2(.1f,.235f),new Vector2(.052f,.263f),new Vector2(.048f,.30f),new Vector2(.048f,.32f),new Vector2(0,.32f)};
            var t=LatheObject(flask?"Erlenmeyer_Flask":"Reagent_Bottle",profile,material,parent);t.localScale=Vector3.one*scale;
            if(!flask)Rod("Screw_Cap",new Vector3(0,.303f*scale,0),new Vector3(0,.335f*scale,0),.055f*scale,"Black",parent);
        }
        private static Transform LatheObject(string name,Vector2[] profile,string material,Transform parent)
        {
            var t=Child(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes.Lathe(name,profile);
            var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mats[material];
            if(material=="Glass")r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }
        private static void Stool(Vector3 position,Transform parent)
        {
            var p=Child("Laboratory_Stool",parent);p.localPosition=position;
            Rod("Seat",new Vector3(0,.59f,0),new Vector3(0,.65f,0),.22f,"Black",p);
            Rod("Gas_Lift",new Vector3(0,.15f,0),new Vector3(0,.60f,0),.026f,"Steel",p);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;Vector3 end=new Vector3(Mathf.Cos(a)*.26f,.08f,Mathf.Sin(a)*.26f);
                Rod("Base_Spoke",new Vector3(0,.16f,0),end,.021f,"Steel",p);
                Rod("Rubber_Foot",end-Vector3.up*.05f,end,.035f,"Rubber",p);
            }
            var collider=p.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.34f,0);collider.size=new Vector3(.46f,.68f,.46f);
        }
        private static void FumeHood(Vector3 position,float yaw,Transform parent)
        {
            var p=Child("Ducted_Fume_Hood",parent);p.localPosition=position;p.localRotation=Quaternion.Euler(0,yaw,0);
            Box("Base_Cabinet",new Vector3(0,.45f,0),new Vector3(2.3f,.9f,.95f),"White",p,.025f);
            Box("Chemical_Resistant_Worktop",new Vector3(0,.955f,-.04f),new Vector3(2.4f,.11f,1.02f),"Counter",p,.018f);
            Box("Extraction_Back",new Vector3(0,1.53f,.43f),new Vector3(2.3f,1.1f,.07f),"White",p,.008f);
            foreach(float x in new[]{-1.12f,1.12f})Box("Hood_Side",new Vector3(x,1.55f,0),new Vector3(.09f,1.12f,.9f),"White",p,.012f);
            Box("Hood_Header",new Vector3(0,2.12f,0),new Vector3(2.35f,.34f,.96f),"Blue",p,.025f);
            Box("Sash_Glass",new Vector3(0,1.76f,-.435f),new Vector3(2.08f,.42f,.008f),"Glass",p,.002f);
            Box("Sash_Rail",new Vector3(0,1.55f,-.45f),new Vector3(2.12f,.036f,.036f),"Steel",p,.005f);
            Box("Hood_Light",new Vector3(0,1.941f,0),new Vector3(1.8f,.025f,.15f),"LED",p,.004f);
            Label("FUME HOOD  /  CH-04",new Vector3(0,2.12f,-.491f),Quaternion.identity,1.45f,.10f,Color.white,p);
            for(int i=0;i<4;i++)Box("Baffle_Slot",new Vector3(0,1.15f+i*.19f,.39f),new Vector3(1.82f,.025f,.005f),"Black",p,.002f);
            Rod("Extraction_Duct",new Vector3(0,2.3f,.2f),new Vector3(0,3.27f,.2f),.17f,"Steel",p);
            foreach(float x in new[]{-.55f,.55f})
            {
                Box("Base_Door",new Vector3(x,.47f,-.482f),new Vector3(1.04f,.77f,.025f),"Blue",p,.012f);
                Rod("Handle",new Vector3(x+.35f,.5f,-.52f),new Vector3(x+.35f,.70f,-.52f),.012f,"Steel",p);
                var f=Child("Stored_Flask",p);f.localPosition=new Vector3(x,1.014f,.02f);Vessel(f,true,1.15f,"Glass");
            }
            var c=p.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,1.13f,0);c.size=new Vector3(2.4f,2.26f,1.02f);
        }
        private static void SinkUnit(Vector3 position,float yaw,Transform parent)
        {
            var p=Child("Laboratory_Sink_Unit",parent);p.localPosition=position;p.localRotation=Quaternion.Euler(0,yaw,0);
            Box("Cabinet",new Vector3(0,.45f,0),new Vector3(2.5f,.9f,.95f),"White",p,.02f);
            foreach(float x in new[]{-.83f,.83f})Box("Counter_Side",new Vector3(x,.95f,0),new Vector3(.85f,.1f,1),"Counter",p,.015f);
            foreach(float z in new[]{-.365f,.365f})Box("Counter_Rim",new Vector3(0,.95f,z),new Vector3(.84f,.1f,.27f),"Counter",p,.008f);
            Box("Basin_Bottom",new Vector3(0,.76f,0),new Vector3(.79f,.028f,.52f),"Steel",p,.008f);
            foreach(float x in new[]{-.4f,.4f})Box("Basin_Side",new Vector3(x,.858f,0),new Vector3(.025f,.20f,.52f),"Steel",p,.005f);
            foreach(float z in new[]{-.26f,.26f})Box("Basin_Side",new Vector3(0,.858f,z),new Vector3(.8f,.20f,.025f),"Steel",p,.005f);
            Rod("Drain",new Vector3(0,.776f,0),new Vector3(0,.78f,0),.035f,"Black",p);
            Rod("Tap_Stem",new Vector3(0,1,.35f),new Vector3(0,1.32f,.35f),.024f,"Steel",p);
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;
                Rod("Tap_Curve",new Vector3(0,1.32f+Mathf.Sin(a)*.16f,.19f+Mathf.Cos(a)*.16f),new Vector3(0,1.32f+Mathf.Sin(b)*.16f,.19f+Mathf.Cos(b)*.16f),.024f,"Steel",p);
            }
            foreach(float x in new[]{-.21f,.21f})
            {Rod("Tap_Valve",new Vector3(x,1,.34f),new Vector3(x,1.075f,.34f),.038f,x<0?"Blue":"Red",p);}
            Box("Splashback",new Vector3(0,1.16f,.47f),new Vector3(2.5f,.32f,.03f),"Steel",p,.008f);
            for(int i=0;i<3;i++)Box("Door",new Vector3(-.82f+i*.82f,.46f,-.485f),new Vector3(.78f,.79f,.025f),"Blue",p,.012f);
            var c=p.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.5f,0);c.size=new Vector3(2.55f,1,1);
        }
        private static void TallCabinet(Vector3 position,float yaw,Transform parent)
        {
            var p=Child("Glass_Front_Laboratory_Cabinet",parent);p.localPosition=position;p.localRotation=Quaternion.Euler(0,yaw,0);
            Box("Back",new Vector3(0,1.12f,.27f),new Vector3(1.35f,2.24f,.045f),"Blue",p,.008f);
            foreach(float x in new[]{-.65f,.65f})Box("Side",new Vector3(x,1.12f,0),new Vector3(.045f,2.24f,.58f),"White",p,.008f);
            foreach(float y in new[]{.06f,.56f,1.10f,1.64f,2.22f})Box("Shelf",new Vector3(0,y,0),new Vector3(1.35f,.045f,.58f),"White",p,.008f);
            foreach(float x in new[]{-.33f,.33f})
            {
                Box("Glass_Door",new Vector3(x,1.15f,-.29f),new Vector3(.61f,2.08f,.008f),"Glass",p,.003f);
                Rod("Door_Handle",new Vector3(x*.3f,.94f,-.34f),new Vector3(x*.3f,1.16f,-.34f),.012f,"Steel",p);
                for(int j=0;j<3;j++)
                {var f=Child("Cabinet_Bottle",p);f.localPosition=new Vector3(x,.59f+j*.54f,0);Vessel(f,false,.85f,j%2==0?"Bottle":"Glass");}
            }
            var c=p.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,1.12f,0);c.size=new Vector3(1.35f,2.24f,.58f);
        }

        private static void BuildCorridor()
        {
            var hall=Child("Department_Corridor_Details",art);
            Poster("ResearchPoster",new Vector3(-5,1.9f,13.875f),Quaternion.identity,.76f,hall);
            Poster("SafetyPoster",new Vector3(4.5f,1.9f,13.875f),Quaternion.identity,.76f,hall);
            var notice=Child("Department_Noticeboard",hall);notice.localPosition=new Vector3(10,1.8f,13.84f);
            Box("Noticeboard_Frame",Vector3.zero,new Vector3(2.7f,1.22f,.07f),"Steel",notice,.022f);
            Box("Cork_Board",new Vector3(0,0,-.044f),new Vector3(2.59f,1.11f,.018f),"Cork",notice,.005f);
            for(int i=0;i<6;i++)
            {
                var sheet=Child("Pinned_Sheet",notice);sheet.localPosition=new Vector3(-.99f+(i%3)*.97f,.27f-(i/3)*.53f,-.06f);
                sheet.localRotation=Quaternion.Euler(0,0,(i%3-1)*3);
                Box("Paper",Vector3.zero,new Vector3(.55f,.43f,.003f),"Paper",sheet,.001f);
                Label(new[]{"LAB TIMETABLE","RESEARCH NEWS","STUDENT FORUM","SAFETY NOTICE","CHEMISTRY CLUB","FACULTY HOURS"}[i],new Vector3(0,.12f,-.004f),Quaternion.identity,.46f,.045f,new Color(.08f,.18f,.22f),sheet);
                for(int j=0;j<5;j++)Box("Print_Line",new Vector3(-.035f,-.01f-j*.035f,-.004f),new Vector3(.34f-j%2*.06f,.006f,.001f),"Blue",sheet,.0003f);
                Rod("Pin",new Vector3(0,.19f,-.003f),new Vector3(0,.19f,-.015f),.015f,"Red",sheet);
            }
            // Wall-side furniture leaves the central corridor and all doors clear.
            CorridorBench(new Vector3(18,0,13.56f),hall);
            CorridorBench(new Vector3(-16,0,13.56f),hall);
            var radiator=Child("Entry_Wall_Radiator",hall);radiator.localPosition=new Vector3(1.79f,.58f,6);radiator.localRotation=Quaternion.Euler(0,90,0);
            Box("Radiator_Body",Vector3.zero,new Vector3(1.45f,.67f,.12f),"White",radiator,.018f);
            for(int i=0;i<18;i++)Box("Radiator_Fin",new Vector3(-.65f+i*.076f,0,-.071f),new Vector3(.025f,.6f,.015f),"Steel",radiator,.005f);
            // Architectural columns frame the main junction without changing its width.
            foreach(var p in new[]{new Vector3(-2.16f,1.63f,10.05f),new Vector3(2.16f,1.63f,10.05f)})
                Box("Junction_Pilaster",p,new Vector3(.22f,3.26f,.24f),"Blue",hall,.008f);
            foreach(var p in new[]{new Vector3(-17.82f,2.9f,12),new Vector3(21.82f,2.9f,12)})
                Box("Ceiling_Service_Bulkhead",p,new Vector3(.18f,.58f,3.7f),"White",hall,.015f);
            // Frame and frosted markings for the existing glazed classroom partitions.
            foreach(float x in new[]{9,11})
            {
                Box("Glass_Transom",new Vector3(x,2.86f,30),new Vector3(2,.035f,.09f),"Steel",hall,.004f);
                for(int j=0;j<7;j++)Box("Glass_Manifestation",new Vector3(x-.72f+j*.24f,1.58f,30.03f),new Vector3(.095f,.045f,.003f),"White",hall,.004f);
            }
        }
        private static void CorridorBench(Vector3 position,Transform parent)
        {
            var p=Child("Waiting_Bench",parent);p.localPosition=position;
            Box("Seat",new Vector3(0,.45f,0),new Vector3(2.3f,.11f,.52f),"Oak",p,.025f);
            Box("Backrest",new Vector3(0,.75f,.22f),new Vector3(2.3f,.42f,.06f),"Oak",p,.022f);
            foreach(float x in new[]{-.88f,.88f})
            {
                Box("Steel_Support",new Vector3(x,.21f,0),new Vector3(.055f,.42f,.42f),"Steel",p,.008f);
                Rod("Arm_Rest",new Vector3(x,.64f,-.22f),new Vector3(x,.64f,.22f),.023f,"Steel",p);
            }
            var c=p.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.46f,0);c.size=new Vector3(2.3f,.92f,.54f);
        }
        private static void Poster(string material,Vector3 position,Quaternion rotation,float width,Transform parent)
        {
            var p=Child("Framed_"+material,parent);p.localPosition=position;p.localRotation=rotation;
            Box("Frame",Vector3.zero,new Vector3(width+.05f,width*4/3+.05f,.035f),"Steel",p,.008f);
            Quad("Printed_Poster",new Vector3(0,0,-.020f),Quaternion.identity,new Vector2(width,width*4/3),material,p);
        }
    }
}
