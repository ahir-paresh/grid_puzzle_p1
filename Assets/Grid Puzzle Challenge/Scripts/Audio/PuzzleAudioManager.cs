using UnityEngine;

namespace GridPuzzle.Audio
{
    /// <summary>
    /// Dynamic procedural audio synthesizer and sound effect manager.
    /// Synthesizes procedural sound waves (swipe whoosh, merge pop with dynamic pitch scaling,
    /// ice shatters, combo chimes, fanfare) directly in code to provide instant rich audio.
    /// </summary>
    public class PuzzleAudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private bool soundEnabled = true;

        public bool SoundEnabled
        {
            get => soundEnabled;
            set => soundEnabled = value;
        }

        private AudioClip _swipeClip;
        private AudioClip _mergeClip;
        private AudioClip _iceShatterClip;
        private AudioClip _comboClip;
        private AudioClip _powerUpClip;
        private AudioClip _winClip;
        private AudioClip _gameOverClip;

        private void Awake()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D sound
            }

            GenerateProceduralClips();
        }

        private void GenerateProceduralClips()
        {
            _swipeClip = GenerateTone(frequencyStart: 180f, frequencyEnd: 100f, duration: 0.08f, waveform: 1);
            _mergeClip = GenerateTone(frequencyStart: 320f, frequencyEnd: 480f, duration: 0.12f, waveform: 0);
            _iceShatterClip = GenerateNoise(duration: 0.22f);
            _comboClip = GenerateTone(frequencyStart: 520f, frequencyEnd: 780f, duration: 0.18f, waveform: 0);
            _powerUpClip = GenerateTone(frequencyStart: 250f, frequencyEnd: 600f, duration: 0.25f, waveform: 1);
            _winClip = GenerateFanfare(duration: 0.6f);
            _gameOverClip = GenerateTone(frequencyStart: 280f, frequencyEnd: 110f, duration: 0.4f, waveform: 2);
        }

        public void PlaySwipe()
        {
            if (!soundEnabled || _swipeClip == null) return;
            sfxSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(_swipeClip, 0.4f);
        }

        public void PlayMerge(int tileValue)
        {
            if (!soundEnabled || _mergeClip == null) return;
            // Higher value tiles have higher pitch!
            float pitch = 1.0f + Mathf.Log(Mathf.Max(2, tileValue), 2) * 0.08f;
            sfxSource.pitch = Mathf.Clamp(pitch, 1.0f, 2.5f);
            sfxSource.PlayOneShot(_mergeClip, 0.6f);
        }

        public void PlayIceDamage(bool shattered)
        {
            if (!soundEnabled) return;
            sfxSource.pitch = shattered ? 1.2f : 0.9f;
            sfxSource.PlayOneShot(_iceShatterClip, shattered ? 0.8f : 0.4f);
        }

        public void PlayCombo(int comboCount)
        {
            if (!soundEnabled || _comboClip == null) return;
            sfxSource.pitch = 1.0f + (comboCount - 1) * 0.15f;
            sfxSource.PlayOneShot(_comboClip, 0.7f);
        }

        public void PlayPowerUp()
        {
            if (!soundEnabled || _powerUpClip == null) return;
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(_powerUpClip, 0.75f);
        }

        public void PlayWin()
        {
            if (!soundEnabled || _winClip == null) return;
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(_winClip, 0.9f);
        }

        public void PlayGameOver()
        {
            if (!soundEnabled || _gameOverClip == null) return;
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(_gameOverClip, 0.85f);
        }

        public void PlayButtonClick()
        {
            if (!soundEnabled) return;
            sfxSource.pitch = 1.5f;
            sfxSource.PlayOneShot(_swipeClip, 0.35f);
        }

        // Procedural Audio Generation Helpers
        private AudioClip GenerateTone(float frequencyStart, float frequencyEnd, float duration, int waveform)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / samplesCount;
                float freq = Mathf.Lerp(frequencyStart, frequencyEnd, t);
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);

                float wave = waveform switch
                {
                    0 => Mathf.Sin(phase), // Sine
                    1 => (Mathf.Repeat(phase / (2f * Mathf.PI), 1f) * 2f) - 1f, // Sawtooth
                    _ => Mathf.Sign(Mathf.Sin(phase)) * 0.6f // Square
                };

                // Exponential decay envelope
                float envelope = Mathf.Exp(-t * 5f);
                samples[i] = wave * envelope * 0.4f;
            }

            var clip = AudioClip.Create("SFX_Tone", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateNoise(float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / samplesCount;
                float noise = (UnityEngine.Random.value * 2f - 1f);
                float envelope = Mathf.Exp(-t * 6f);
                samples[i] = noise * envelope * 0.4f;
            }

            var clip = AudioClip.Create("SFX_Noise", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateFanfare(float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            float[] notes = { 440f, 554f, 659f, 880f }; // A major arpeggio
            int noteLength = samplesCount / notes.Length;

            for (int i = 0; i < samplesCount; i++)
            {
                int noteIdx = Mathf.Clamp(i / noteLength, 0, notes.Length - 1);
                float freq = notes[noteIdx];
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);

                float localT = (float)(i % noteLength) / noteLength;
                float envelope = Mathf.Sin(localT * Mathf.PI);

                samples[i] = Mathf.Sin(phase) * envelope * 0.45f;
            }

            var clip = AudioClip.Create("SFX_Fanfare", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
