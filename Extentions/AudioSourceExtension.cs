using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PowerCellStudio
{
    public static class AudioSourceExtension
    {
        public static void OnReachLoopPoint(this AudioSource audioSource, UnityAction<AudioSource> action)
        {
            ApplicationManager.instance.StartCoroutine(WaitForLoopPoint(audioSource, action));
        }

        private static IEnumerator WaitForLoopPoint(AudioSource audioSource, UnityAction<AudioSource> action)
        {
            while (!audioSource.isPlaying)
            {
                yield return null;
            }
            var time = audioSource.clip.length;
            yield return new WaitForSeconds(time);
            action?.Invoke(audioSource);
        }
    }
}