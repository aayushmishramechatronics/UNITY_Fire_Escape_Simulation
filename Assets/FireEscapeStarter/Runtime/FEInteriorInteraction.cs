using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FireEscape
{
    [DefaultExecutionOrder(-50)]
    public sealed class FEInteriorInteraction : MonoBehaviour
    {
        public FEFirstPerson player;
        public FEEmergency emergency;
        public FEDoor[] doors;
        private FEWallPlan inspected;
        private string prompt = "", feedback = "";
        private float feedbackTime;
        private GUIStyle label;
        private void OnEnable() { if (emergency != null) emergency.RunReset += ResetInterior; }
        private void OnDisable()
        {
            if (emergency != null) emergency.RunReset -= ResetInterior;
            ClosePlan();
        }
        private void ResetInterior()
        {
            inspected = null;
            emergency.inspectionActive = false;
            prompt = feedback = "";
            foreach (var door in doors) if (door != null) door.ResetDoor();
        }
        private void ClosePlan()
        {
            inspected = null;
            if (emergency == null) return;
            emergency.inspectionActive = false;
            if (player != null) player.controlsEnabled = emergency.IsRunning;
        }
        private void Update()
        {
            prompt = "";
            if (emergency == null || player == null) return;
            if (!emergency.IsRunning) { if (inspected != null) ClosePlan(); return; }
#if ENABLE_INPUT_SYSTEM
            bool interact = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (inspected != null)
            {
                player.controlsEnabled = false;
                if (interact) ClosePlan();
                return;
            }
            Camera camera = player.viewCamera;
            if (camera == null) return;
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 3.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                var plan = hit.collider.GetComponentInParent<FEWallPlan>();
                var door = hit.collider.GetComponentInParent<FEDoor>();
                var torch = hit.collider.GetComponentInParent<FETorchPickup>();
                var fireDoor = hit.collider.GetComponentInParent<FEPressureDoor>();
                if (plan != null)
                {
                    prompt = "E - Inspect evacuation plan";
                    if (interact)
                    {
                        inspected = plan;
                        emergency.RecordMapInspection();
                        emergency.inspectionActive = true;
                        player.controlsEnabled = false;
                    }
                }
                else if (torch != null)
                { prompt = "E - Collect torch | F to toggle after pickup"; if(interact)torch.Collect(); }
                else if (fireDoor != null)
                { prompt = fireDoor.Prompt; if(interact)fireDoor.Interact(); }
                else if (door != null)
                {
                    prompt = door.Prompt;
                    if (interact) { feedback = door.Interact(); feedbackTime = Time.time + 2.5f; }
                }
            }
#endif
        }
        private void OnGUI()
        {
            if (emergency == null || !emergency.IsRunning) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                label.normal.textColor = Color.white;
            }
            if (inspected != null)
            {
                GUI.depth = -20;
                Color previous = GUI.color;
                GUI.color = new Color(.025f, .035f, .045f, .97f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = previous;
                GUI.DrawTexture(new Rect(20, 10, Screen.width - 40, Screen.height - 65), inspected.plan, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(10, Screen.height - 48, Screen.width - 20, 42), "E - Close plan   |   The evacuation timer is still running", label);
                GUI.depth = 0;
            }
            else
            {
                string text = Time.time < feedbackTime && !string.IsNullOrEmpty(feedback) ? feedback : prompt;
                if (string.IsNullOrEmpty(text)) return;
                float width = Mathf.Min(540, Screen.width - 30);
                Color oldColor = GUI.color;GUI.color=new Color(.025f,.032f,.04f,1);
                GUI.DrawTexture(new Rect((Screen.width-width)/2,Screen.height-90,width,48),Texture2D.whiteTexture);GUI.color=oldColor;
                GUI.Label(new Rect((Screen.width - width) / 2 + 8, Screen.height - 86, width - 16, 40), text, label);
            }
        }
    }
}
