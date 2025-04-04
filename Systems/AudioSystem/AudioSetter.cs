using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class AudioSetter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public string clipRef;
        public AudioSourceType audioType;
        public MusicGroup musicGroup;
        public bool playOnEnable = true;
        public bool playOnUp = false;
        public bool playOnDown = false;
        public bool attachToGameObject = false;
        public float fadeoutTime = 1f;
        public float intervalTime = 1f;
        public float fadeinTime = 1f;

        private bool _played = false;
        public bool played => _played;

        private void OnEnable()
        {
            if(!playOnEnable) return;
            PlayAudio();
        }

        // [Button(ButtonSizes.Large)]
        public void PlayAudio()
        {
            if (AudioManager.instance.IsMute(audioType)) return;
            if (string.IsNullOrEmpty(clipRef)) return;
            PlayClip(clipRef);
        }

        private void PlayClip(string audioClipRef)
        {
            _played = true;
            switch (audioType)
            {
                case AudioSourceType.Music:
                    AudioManager.instance.PlayMusic(audioClipRef, musicGroup,  fadeoutTime, intervalTime, fadeinTime);
                    break;
                case AudioSourceType.Ambience:
                    AudioManager.instance.PlayAmbience(audioClipRef, musicGroup, fadeoutTime, intervalTime, fadeinTime);
                    break;
                case AudioSourceType.Effect3D:
                    AudioManager.instance.RequestPlayEffect(audioClipRef, false, attachToGameObject ? gameObject : null, transform.position, true);
                    break;
                case AudioSourceType.Dialog:
                    AudioManager.instance.PlayDialog(audioClipRef);
                    break;
                case AudioSourceType.UIEffect:
                    AudioManager.instance.RequestPlayEffect(audioClipRef, true, attachToGameObject ? gameObject : null, transform.position, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if(!playOnUp) return;
            PlayAudio();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(!playOnDown) return;
            PlayAudio();
        }
    }
}