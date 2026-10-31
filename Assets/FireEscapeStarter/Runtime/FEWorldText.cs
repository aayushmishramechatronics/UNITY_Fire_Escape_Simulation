using UnityEngine;
namespace FireEscape
{
    // Dynamic fonts can rebuild their texture atlas when more signs are encountered.
    public sealed class FEWorldText : MonoBehaviour
    {
        public Font font;
        public Material depthMaterial;
        private void OnEnable() { Font.textureRebuilt += Refresh; Refresh(font); }
        private void OnDisable() { Font.textureRebuilt -= Refresh; }
        private void Refresh(Font rebuilt)
        {
            if (font != null && rebuilt == font && depthMaterial != null)
                depthMaterial.mainTexture = font.material.mainTexture;
        }
    }
}
