using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PowerCellStudio
{
    public static class AudioSourceExtension
    {
        /// <summary>
        /// 在音频源到达循环点时调用指定的动作。
        /// Invokes the specified action when the audio source reaches its loop point.
        /// </summary>
        /// <param name="audioSource">要监视的音频源。</param>
        /// <param name="action">音频源到达循环点时要调用的动作。</param>
        public static void OnReachLoopPoint(this AudioSource audioSource, UnityAction<AudioSource> action)
        {
            if (audioSource.clip == null)
            {
                Debug.LogWarning("AudioSource has no clip assigned.");
                return;
            }

            ApplicationManager.instance.StartCoroutine(WaitForLoopPoint(audioSource, action));
        }

        private static IEnumerator WaitForLoopPoint(AudioSource audioSource, UnityAction<AudioSource> action)
        {
            while (audioSource != null && !audioSource.isPlaying)
            {
                yield return null;
            }

            if (audioSource == null || audioSource.clip == null)
            {
                yield break;
            }

            while (audioSource.isPlaying)
            {
                var timeRemaining = audioSource.clip.length - audioSource.time;
                yield return new WaitForSeconds(timeRemaining);

                // Optionally check if the audio source is still valid and playing
                if (audioSource == null || !audioSource.isPlaying)
                {
                    yield break;
                }
                
                action?.Invoke(audioSource);
            }
        }
    }
}