using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Runtime side of the sandbox's look: the player's camera is spawned by the rig after
    /// the scene loads, so post-processing (bloom, colour grading) has to be switched on for
    /// it here. Also starts the background music quietly.
    /// </summary>
    public sealed class ScenePolish : MonoBehaviour
    {
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.18f;

        private float _checkUntil;

        private void Start()
        {
            _checkUntil = Time.time + 6f;
            if (music != null)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.clip = music;
                source.loop = true;
                source.volume = musicVolume;
                source.spatialBlend = 0f;
                source.Play();
            }
        }

        private void Update()
        {
            if (Time.time > _checkUntil)
                return;
            foreach (Camera cam in Camera.allCameras)
            {
                if (cam.targetTexture != null)
                    continue; // snapshot and mirror cameras stay cheap
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                if (data != null && !data.renderPostProcessing)
                    data.renderPostProcessing = true;
            }
        }
    }
}
