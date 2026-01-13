// using System.Collections;
// using System.Collections.Generic;
//
// using UnityEngine;
// using UnityEngine.Playables;
//
// namespace PowerCellStudio
// {
//     public class MusicPlayer : BaseAudioPlayer
//     {
//         private Dictionary<MusicGroup, AudioClipContainer> _containers;
//         private Dictionary<MusicGroup, ScriptPlayable<AudioTransfer>> _groupTransfer;
//         private Dictionary<int, ScriptPlayable<AudioQueue>> _groupQueue;
//         private ScriptPlayable<AudioMixerPro> _mixer;
//         private MusicGroup _curGroup;
//
//         /// <summary>
//         /// 音频组过渡时间
//         /// </summary>
//         public float GroupTransferTime = 0.5f;
//
//
//         /// <summary>
//         /// 音乐播放工具
//         /// </summary>
//         /// <param name="name">自定义名称</param>
//         public MusicPlayer(string name)
//         {
//             Init(name);
//         }
//
//         protected override void Init(string name)
//         {
//             if (_graph.IsValid())
//             {
//                 Reset();
//                 return;
//             }
//             _containers = new Dictionary<MusicGroup, AudioClipContainer>();
//             _groupTransfer = new Dictionary<MusicGroup, ScriptPlayable<AudioTransfer>>();
//             _groupQueue = new Dictionary<int, ScriptPlayable<AudioQueue>>();
//             var gameObj = new GameObject(string.IsNullOrEmpty(name)? "MusicPlayer" : name);
//             _audioSource = gameObj.AddComponent<AudioSource>();
//             _audioSource.spatialBlend = 0f;
//             GameObject.DontDestroyOnLoad(gameObj);
//             _curVolume = 1f;
//             _maxVolume = _audioSource.volume;
//             _graph = PlayableGraph.Create(string.IsNullOrEmpty(name)? "MusicPlayerGraph": $"{name}Graph");
//             _graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
//             _mixer = AudioMixerPro.Create(_graph);
//             _mixer.GetBehaviour().enable = true;
//             _graph.CreatePlayableOutput(_audioSource, _mixer, 0, string.IsNullOrEmpty(name)? "MusicPlayerOutput": $"{name}Output");
//             _graph.Play();
//             _audioSource.gameObject.AddComponent<PlayableGraphAutoDestroy>().graph = _graph;
//         }
//         
//         public override void DeInit()
//         {
//             foreach (var (_, audioClipContainer) in _containers)
//             {
//                 audioClipContainer.Destroy();
//             }
//             _containers = null;
//             _groupTransfer = null;
//             _groupQueue = null;
//             if (_graph.IsValid()) _graph.Destroy();
//             if(_audioSource) GameObject.Destroy(_audioSource.gameObject);
//         }
//
//         public override void Reset()
//         {
//             foreach (var (_, audioClipContainer) in _containers)
//             {
//                 audioClipContainer.Destroy();
//             }
//             foreach (var (_, transfer) in _groupTransfer)
//             {
//                 transfer.Destroy();
//             }
//             foreach (var (_, queue) in _groupQueue)
//             {
//                 queue.Destroy();
//             }
//             _containers.Clear();
//             _groupTransfer.Clear();
//             _groupQueue.Clear();
//             _audioSource.volume = _maxVolume;
//             _curVolume = 1f;
//         }
//
//         private AudioClipContainer CreateContainer(AudioClip clip)
//         {
//             if (clip == null)
//             {
//                 throw LinkLog.Exception("the AudioClip was null or empty!");
//             }
//             var newContainer = AudioClipContainer.Create(_graph, clip, true);
//             return newContainer;
//         }
//
//         private AudioClipContainer CreateContainer(IReadOnlyList<AudioClip> clip)
//         {
//             if (clip == null || clip.Count == 0)
//             {
//                 throw LinkLog.Exception("the AudioClip(s) was null or empty!");
//             }
//
//             var newContainer = AudioClipContainer.Create(_graph, clip[0], true);
//             for (int i = 1; i < clip.Count; i++)
//             {
//                 newContainer.Add(clip[i], true);
//             }
//             return newContainer;
//         }
//
//         private void DestroyContainer(AudioClipContainer container)
//         {
//             var hushCode = container.GetHashCode();
//             if (_groupQueue.TryGetValue(hushCode, out var queue))
//             {
//                 queue.Destroy();
//             }
//             container.Destroy();
//         }
//
//         private ScriptPlayable<AudioTransfer> CreateTransfer(MusicGroup group, Playable inputPlayable, int inputIndex)
//         {
//             var transfer = AudioTransfer.Create(_graph, inputPlayable, inputIndex);
//             transfer.GetBehaviour().enable = true;
//             _mixer.GetBehaviour().ConnectOrAddInput((int) group, transfer, 0, 1f);
//             _groupTransfer[group] = transfer;
//             return transfer;
//         }
//
//         /// <summary>
//         /// 重新设定音乐组，并播放音乐
//         /// </summary>
//         /// <param name="clip">音频</param>
//         /// <param name="group">音频组</param>
//         /// <param name="fadeoutTime">渐隐时间</param>
//         /// <param name="intervalTime">空白时间</param>
//         /// <param name="fadeinTime">渐进时间</param>
//         /// <param name="weight">音频组权重</param>
//         public void Play(AudioClip clip, MusicGroup group,
//             float fadeoutTime, float intervalTime, float fadeinTime, float weight = 1f)
//         {
//             if(_waitForClearGroup.Contains(group)) return;
//             if (!clip) return;
//             if (_containers.TryGetValue(group, out var curContainer))
//             {
//                 ApplicationManager.instance.DelayedCall(fadeoutTime + fadeinTime + intervalTime, () => DestroyContainer(curContainer));
//             }
//             
//             var container = CreateContainer(clip);
//             // var isChangeGroup = _curGroup != group;
//             if (_groupTransfer.TryGetValue(group, out var transfer))
//             {
//                 transfer.GetBehaviour().TransferTo(
//                     container.PlayByIndex(0, true),
//                     0, 
//                     fadeoutTime,
//                     fadeinTime,
//                     intervalTime);
//             }
//             else
//             {
//                 transfer = CreateTransfer(group, container.PlayByIndex(0), 0);
//                 _groupTransfer[group] = transfer;
//             }
//             _containers[group] = container;
//             SetWeight(weight, group, GroupTransferTime);
//             _curGroup = group;
//         }
//
//         /// <summary>
//         /// 重新设定音乐组，并播放音乐
//         /// </summary>
//         /// <param name="clips">音频</param>
//         /// <param name="group">音频组</param>
//         /// <param name="fadeoutTime">渐隐时间</param>
//         /// <param name="intervalTime">空白时间</param>
//         /// <param name="fadeinTime">渐进时间</param>
//         /// <param name="loopTimesPerClip">每个音频循环次数</param>
//         /// <param name="weight">音频组权重</param>
//         /// <param name="playRandomly">是否随机播放</param>
//         public void Play(AudioClip[] clips, MusicGroup group, 
//             float fadeoutTime, float intervalTime, float fadeinTime, 
//             int loopTimesPerClip = 1, float weight = 1f, bool playRandomly = false)
//         {
//             if(_waitForClearGroup.Contains(group)) return;
//             if (clips == null || clips.Length == 0) return;
//             if (_containers.TryGetValue(group, out var curContainer))
//             {
//                 ApplicationManager.instance.DelayedCall(fadeoutTime + fadeinTime + intervalTime, () => DestroyContainer(curContainer));
//             }
//             var container = CreateContainer(clips);
//             var queue = AudioQueue.Create(_graph, container);
//             queue.GetBehaviour().enable = true;
//             queue.GetBehaviour().PlayRandomly = playRandomly;
//             queue.GetBehaviour().FadeoutTime = fadeoutTime;
//             queue.GetBehaviour().FadeinTime = fadeinTime;
//             queue.GetBehaviour().IntervalTime = intervalTime;
//             queue.GetBehaviour().DefaultLoop = Mathf.Max(1,loopTimesPerClip);
//             _groupQueue[container.GetHashCode()] = queue;
//             // var isChangeGroup = _curGroup != group;
//             if (_groupTransfer.TryGetValue(group, out var transfer))
//             {
//                 transfer.GetBehaviour().TransferTo(queue,
//                     0,
//                     fadeoutTime,
//                     fadeinTime,
//                     intervalTime);
//             }
//             else
//             {
//                 transfer = CreateTransfer(group, queue, 0);
//                 _groupTransfer[group] = transfer;
//             }
//             _containers[group] = container;
//             SetWeight(weight, group, GroupTransferTime);
//             _curGroup = group;
//         }
//
//         /// <summary>
//         /// 暂停音频组
//         /// </summary>
//         /// <param name="group">音频组</param>
//         public void Pause(MusicGroup group)
//         {
//             if (!_containers.TryGetValue(group, out var container)) return;
//             container.Pause();
//         }
//         
//         /// <summary>
//         /// 暂停所有音频
//         /// </summary>
//         public void PauseAll()
//         {
//             foreach (var (_, clipContainer) in _containers)
//             {
//                 clipContainer.Pause();
//             }
//         }
//
//         /// <summary>
//         /// 删除音频组
//         /// </summary>
//         /// <param name="group">音频组</param>
//         public void Remove(MusicGroup group)
//         {
//             ApplicationManager.instance.StartCoroutine(WaitForGroupClear(group));
//         }
//
//         private HashSet<MusicGroup> _waitForClearGroup = new HashSet<MusicGroup>();
//         private IEnumerator WaitForGroupClear(MusicGroup group)
//         {
//             _waitForClearGroup.Add(group);
//             SetWeight(0f, group, GroupTransferTime, false);
//             yield return new WaitForSecondsRealtime(GroupTransferTime);
//             if (_groupTransfer.TryGetValue(group, out var transfer))
//             {
//                 transfer.Destroy();
//                 _groupTransfer.Remove(group);
//             }
//             
//             if (_containers.TryGetValue(group, out var container))
//             {
//                 var hashCode = container.GetHashCode();
//                 if (_groupQueue.TryGetValue(hashCode, out var queue))
//                 {
//                     queue.Destroy();
//                     _groupQueue.Remove(hashCode);
//                 }
//                 container.Destroy();
//                 _containers.Remove(group);
//             }
//             _waitForClearGroup.Remove(group);
//         }
//         
//         
//
//         /// <summary>
//         /// 回复播放
//         /// </summary>
//         /// <param name="group">音频组</param>
//         public void Resume(MusicGroup group)
//         {
//             if (!_containers.TryGetValue(group, out var container)) return;
//             container.Resume();
//         }
//
//         /// <summary>
//         /// 重新播放音频组
//         /// </summary>
//         /// <param name="group">音频组</param>
//         public void Restart(MusicGroup group)
//         {
//             if (!_containers.TryGetValue(group, out var container)) return;
//             if (container.clips == null || container.clips.Count == 0) return;
//             container.PlayByIndex(0).SetTime(0f);
//         }
//
//         // /// <summary>
//         // /// 删除音频组
//         // /// </summary>
//         // /// <param name="group">音频组</param>
//         // public void Remove(MusicGroup group)
//         // {
//         //     if (!_containers.TryGetValue(group, out var container)) return;
//         //     DestroyContainer(container);
//         //     _containers.Remove(group);
//         // }
//
//         /// <summary>
//         /// 设置音频组播放速度
//         /// </summary>
//         /// <param name="speedValue">归一化播放速度</param>
//         /// <param name="group">音频组</param>
//         public void SetSpeed(float speedValue, MusicGroup group)
//         {
//             if (!_containers.TryGetValue(group, out var container) || container.clips == null) return;
//             foreach (var audioClipPlayable in container.clips)
//             {
//                 audioClipPlayable.Value.SetSpeed(speedValue);
//             }
//         }
//
//         /// <summary>
//         /// 设置音频组权重
//         /// </summary>
//         /// <param name="weight">权重值</param>
//         /// <param name="group">音频组</param>
//         /// <param name="transferTime">过渡时间</param>
//         /// <param name="setGroup">设置为当前主要播放音频组</param>
//         public void SetWeight(float weight, MusicGroup group, float transferTime, bool setGroup = false)
//         {
//             if(_waitForClearGroup.Contains(group)) return;
//             if (!_containers.ContainsKey(group)) return;
//             _mixer.GetBehaviour().TweenWeight((int) group, weight, transferTime);
//             if(setGroup) SetCurGroup(group);
//         }
//
//         /// <summary>
//         /// 设置音频组权重
//         /// </summary>
//         /// <param name="weight">权重值</param>
//         /// <param name="group">音频组</param>
//         /// <param name="setGroup">设置为当前主要播放音频组</param>
//         public void SetWeightDirectly(float weight, MusicGroup group, bool setGroup = false)
//         {
//             if(_waitForClearGroup.Contains(group)) return;
//             if (!_containers.ContainsKey(group)) return;
//             _mixer.GetBehaviour().SetWeightDirectly((int) group, weight);
//             if(setGroup) SetCurGroup(group);
//         }
//
//         /// <summary>
//         /// 获取当前主要播放音频组
//         /// </summary>
//         /// <returns></returns>
//         public MusicGroup GetCurGroup()
//         {
//             return _curGroup;
//         }
//         
//         /// <summary>
//         /// 设置为主要播放音频组
//         /// </summary>
//         /// <param name="group">音频组</param>
//         public void SetCurGroup(MusicGroup group)
//         {
//             _curGroup = group;
//         }
//
//     }
// }