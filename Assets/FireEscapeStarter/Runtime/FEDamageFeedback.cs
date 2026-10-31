using UnityEngine;
namespace FireEscape
{
    [DefaultExecutionOrder(200)]
    public sealed class FEDamageFeedback : MonoBehaviour
    {
        public FEEmergency emergency;
        public Camera view;
        private Texture2D vignette;
        private Vector3 home;
        private float strength, clock;
        private bool captured;
        private void Start()
        {
            if (view != null) { home = view.transform.localPosition; captured = true; }
            vignette = new Texture2D(128,128,TextureFormat.RGBA32,false);
            vignette.name="Runtime damage edge";vignette.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float edge=Mathf.Max(Mathf.Abs((x-63.5f)/63.5f),Mathf.Abs((y-63.5f)/63.5f));
                pixels[y*128+x]=new Color(.72f,.018f,.006f,Mathf.Pow(Mathf.InverseLerp(.35f,1,edge),2));
            }
            vignette.SetPixels(pixels);vignette.Apply(false,true);
        }
        private void LateUpdate()
        {
            if(emergency==null || view==null || !captured)return;
            if(emergency.IsPaused) { view.transform.localPosition=home;return; }
            if(!emergency.IsRunning) { strength=0;view.transform.localPosition=home;return; }
            clock+=Time.deltaTime;
            strength=Mathf.MoveTowards(strength,emergency.IsTakingDamage?1:0,Time.deltaTime*(emergency.IsTakingDamage?7:2));
            // Position only: never fight the first-person controller's mouse rotation.
            Vector3 jolt=new Vector3(Mathf.Sin(clock*23),Mathf.Sin(clock*19.3f),0)*(.012f*strength);
            view.transform.localPosition=home+jolt;
        }
        private void OnGUI()
        {
            if(vignette==null || emergency==null || !emergency.IsRunning || strength<.01f)return;
            var previous=GUI.color;int depth=GUI.depth;GUI.depth=20;
            GUI.color=new Color(1,1,1,strength*(.48f+.16f*Mathf.Sin(clock*8)));
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),vignette);
            GUI.color=previous;GUI.depth=depth;
        }
        private void OnDisable() { if(captured && view!=null)view.transform.localPosition=home; }
        private void OnDestroy() { if(vignette!=null)Destroy(vignette); }
    }
}
