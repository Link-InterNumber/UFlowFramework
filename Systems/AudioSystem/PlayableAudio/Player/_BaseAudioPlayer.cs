// // using DG.Tweening;
//
// using System;
// using System.Collections;
// using UnityEngine;
// using UnityEngine.Playables;
//
// namespace PowerCellStudio
// {
//     public abstract class BaseAudioPlayer
//     {
//         protected PlayableGraph _graph;
//         protected AudioSource _audioSource;
//         protected float _curVolume;
//         protected float _maxVolume;
//         
//         /// <summary>
//         /// 音频播放组件
//         /// </summary>
//         public AudioSource AudioSource => _audioSource;
//
//         protected abstract void Init(string name);
//         
//         public virtual void DeInit()
//         {
//             if (_graph.IsValid()) _graph.Destroy();
//             if(_audioSource) GameObject.Destroy(_audioSource.gameObject);
//         }
//
//         public abstract void Reset();
//
//         /// <summary>
//         /// 设置播放器更新模式
//         /// </summary>
//         /// <param name="updateMode"></param>
//         public void SetTimeUpdateMode(DirectorUpdateMode updateMode)
//         {
//             _graph.SetTimeUpdateMode(updateMode);
//         }
//
//         /// <summary>
//         /// 获取播放器更新模式
//         /// </summary>
//         /// <returns></returns>
//         public DirectorUpdateMode GetUpdateMode()
//         {
//             return _graph.GetTimeUpdateMode();
//         }
//
//         /// <summary>
//         /// 手动更新播放器
//         /// </summary>
//         /// <param name="dt"></param>
//         public void UpdateManually(float dt)
//         {
//             _graph.Evaluate(dt);
//         }
//
//         /// <summary>
//         /// 停止或开启播放器
//         /// </summary>
//         /// <param name="isTrue"></param>
//         public void Stop(bool isTrue)
//         {
//             if (isTrue && _graph.IsPlaying())
//             {
//                 _graph.Stop();
//             }
//             else if(!isTrue && !_graph.IsPlaying())
//             {
//                 _graph.Play();
//             }
//         }
//
//         private Coroutine _fadeCoroutine;
//         public void Fade(float targetVolume, float transferTime, Action onComplete = null,
//             EaseType easeType = EaseType.Linear)
//         {
//             if(_fadeCoroutine != null) ApplicationManager.instance.StopCoroutine(_fadeCoroutine);
//             _fadeCoroutine = ApplicationManager.instance.StartCoroutine(FadeHandler(targetVolume, transferTime, onComplete, easeType));
//         }
//
//         private IEnumerator FadeHandler(float targetVolume, float transferTime, Action onComplete = null,
//             EaseType easeType = EaseType.Linear)
//         {
//             if(!_audioSource) yield break;
//             var timePass = 0f;
//             var starValue = _audioSource.volume;
//             while (timePass < transferTime)
//             {
//                 var normalized = Ease.GetEase(easeType, Mathf.Clamp01(timePass / transferTime));
//                 _audioSource.volume = Mathf.Lerp(starValue, targetVolume, normalized);
//                 timePass += Time.unscaledDeltaTime;
//                 if(!_audioSource) yield break;
//                 yield return null;
//             }
//             onComplete?.Invoke();
//             if(_audioSource) _audioSource.volume = targetVolume;
//             _fadeCoroutine = null;
//         }
//         
//         /// <summary>
//         /// 设置音量，实际音量=输入参数×设置的最大音量
//         /// </summary>
//         /// <param name="volume">归一化音量</param>
//         /// <param name="transferTime">过度时间</param>
//         /// <param name="onComplete"></param>
//         public void SetVolume(float volume, float transferTime, Action onComplete = null)
//         {
//             var newValue = Mathf.Clamp01(volume * _maxVolume);
//             _curVolume = volume;
//             if(_fadeCoroutine != null) ApplicationManager.instance.StopCoroutine(_fadeCoroutine);
//             if (transferTime <= 0)
//             {
//                 _audioSource.volume = newValue;
//                 onComplete?.Invoke();
//                 return;
//             }
//             Fade(newValue, transferTime, onComplete);
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         /// <param name="isReal"></param>
//         /// <returns></returns>
//         public float GetVolume(bool isReal)
//         {
//             return isReal? _audioSource.volume : _curVolume;
//         }
//         
//         public bool IsMute => _audioSource.mute;
//
//         /// <summary>
//         /// 设置默认音量
//         /// </summary>
//         /// <param name="volume"></param>
//         public void SetMaxVolume(float volume)
//         {
//             var newValue = Mathf.Clamp01(volume);
//             if (newValue == 0)
//             {
//                 LinkLog.LogError("Can not set [DefaultVolume] sub-zero!");
//                 return;
//             }
//             _maxVolume = newValue;
//             SetVolume(_curVolume, 0f);
//         }
//
//         public float GetMaxVolume()
//         {
//             return _maxVolume;
//         }
//
//         /// <summary>
//         /// 静音
//         /// </summary>
//         /// <param name="transferTime">过渡时间</param>
//         public void Mute(float transferTime)
//         {
//             if(_fadeCoroutine != null) ApplicationManager.instance.StopCoroutine(_fadeCoroutine);
//             if (transferTime <= 0)
//             {
//                 _audioSource.volume = 0;
//                 _audioSource.mute = true;
//                 return;
//             }
//             Fade(0, transferTime, ()=> _audioSource.mute = true);
//         }
//
//         /// <summary>
//         /// 取消静音
//         /// </summary>
//         /// <param name="transferTime">过度时间</param>
//         public void Unmute(float transferTime)
//         {
//             _audioSource.mute = false;
//             SetVolume(_curVolume == 0f ? 1f : _curVolume, transferTime);
//         }
//     }
// }