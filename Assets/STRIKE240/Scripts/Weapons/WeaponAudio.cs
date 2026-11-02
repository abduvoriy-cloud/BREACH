using UnityEngine;

namespace Strike240
{
    /// <summary>Plays assigned clips, with generated placeholder sounds when no clips are assigned.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class WeaponAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shotClip;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

        private bool ownsShotClip;
        private bool ownsReloadClip;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = volume;

            if (shotClip == null)
            {
                shotClip = CreateShotPlaceholder();
                ownsShotClip = true;
            }
            if (reloadClip == null)
            {
                reloadClip = CreateReloadPlaceholder();
                ownsReloadClip = true;
            }
        }

        public void PlayShot()
        {
            if (audioSource != null && shotClip != null) audioSource.PlayOneShot(shotClip, volume);
        }

        public void PlayReload()
        {
            if (audioSource != null && reloadClip != null) audioSource.PlayOneShot(reloadClip, volume * 0.8f);
        }

        private void OnDestroy()
        {
            if (ownsShotClip && shotClip != null) Destroy(shotClip);
            if (ownsReloadClip && reloadClip != null) Destroy(reloadClip);
        }

        private static AudioClip CreateShotPlaceholder()
        {
            const int sampleRate = 22050;
            const int sampleCount = 3308;
            float[] samples = new float[sampleCount];
            Random.State previousState = Random.state;
            Random.InitState(240);
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Exp(-time * 38f);
                float noise = Random.Range(-1f, 1f);
                float lowThump = Mathf.Sin(2f * Mathf.PI * 95f * time) * 0.5f;
                samples[i] = (noise * 0.6f + lowThump) * envelope * 0.8f;
            }
            Random.state = previousState;
            return CreateClip("Generated Rifle Shot", sampleRate, samples);
        }

        private static AudioClip CreateReloadPlaceholder()
        {
            const int sampleRate = 22050;
            const int sampleCount = 2205;
            float[] samples = new float[sampleCount];
            Random.State previousState = Random.state;
            Random.InitState(241);
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Exp(-time * 95f);
                float click = Mathf.Sin(2f * Mathf.PI * 1250f * time);
                float noise = Random.Range(-1f, 1f) * 0.25f;
                samples[i] = (click * 0.6f + noise) * envelope * 0.5f;
            }
            Random.state = previousState;
            return CreateClip("Generated Reload Click", sampleRate, samples);
        }

        private static AudioClip CreateClip(string clipName, int sampleRate, float[] samples)
        {
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.DontSave;
            return clip;
        }
    }
}
