using UnityEngine;
using UnityEngine.XR;

namespace NeonRift
{
    [RequireComponent(typeof(Camera))]
    public sealed class PremiumPresentation : MonoBehaviour
    {
        Material grade;

        void Awake()
        {
            Shader shader = Resources.Load<Shader>("NeonPremiumGrade");
            if (shader != null && shader.isSupported) grade = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        void OnDestroy()
        {
            if (grade != null) Destroy(grade);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Standalone headsets favor MSAA and minimal full-screen passes.
            if (grade == null || XRSettings.isDeviceActive)
            {
                Graphics.Blit(source, destination);
                return;
            }

            grade.SetFloat("_Contrast", 1.045f);
            grade.SetFloat("_Saturation", 0.94f);
            grade.SetFloat("_Vignette", 0.1f);
            grade.SetFloat("_Lift", 0.018f);
            grade.SetColor("_GradeTint", new Color(0.95f, 1f, 1.045f, 1f));
            Graphics.Blit(source, destination, grade);
        }
    }
}
