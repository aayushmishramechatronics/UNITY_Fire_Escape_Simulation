using UnityEngine;

namespace FireEscape
{
    public sealed class FEDoor : MonoBehaviour
    {
        public enum DoorKind { Locked, Safe, Fatal }
        public DoorKind kind;
        public string roomName;
        public Transform hinge;
        public Collider leafCollider;
        public ParticleSystem explosion;
        public FEEmergency emergency;
        private bool opened;
        private float angle;
        public string Prompt => kind == DoorKind.Locked ? "E - Check " + roomName : opened ? roomName + " - Door open" : "E - Open " + roomName;

        public string Interact()
        {
            if (!emergency.IsRunning) return "";
            if (kind == DoorKind.Locked) return roomName + " is locked.";
            if (opened) return "";
            opened = true;
            // Opens away from the corridor. Remove leaf collision while it moves
            // to avoid trapping the player against a rotating collider.
            if (leafCollider != null) leafCollider.enabled = false;
            if (kind == DoorKind.Fatal) emergency.TriggerExplosion(roomName, explosion);
            return "";
        }
        private void Update()
        {
            if (emergency == null || (!emergency.IsRunning && !emergency.IsExploding)) return;
            angle = Mathf.MoveTowards(angle, opened ? -100 : 0, Time.deltaTime * 190);
            hinge.localRotation = Quaternion.Euler(0, angle, 0);
        }
        public void ResetDoor()
        {
            opened = false;
            angle = 0;
            hinge.localRotation = Quaternion.identity;
            if (leafCollider != null) leafCollider.enabled = true;
            if (explosion != null) explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
