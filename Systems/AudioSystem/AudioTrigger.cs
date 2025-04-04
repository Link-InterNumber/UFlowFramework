using UnityEngine;

namespace PowerCellStudio
{
    [RequireComponent(typeof(AudioSetter))]
    public class AudioTrigger : MonoBehaviour
    {
        public AudioSetter audioSetter;

        private void Awake()
        {
            if (!audioSetter) audioSetter = GetComponent<AudioSetter>();
            audioSetter.playOnEnable = false;
            audioSetter.playOnUp = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            audioSetter.PlayAudio();
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            audioSetter.PlayAudio();
        }
    }
}