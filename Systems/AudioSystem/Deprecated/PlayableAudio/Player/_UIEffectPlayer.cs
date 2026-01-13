// using UnityEngine;
// using UnityEngine.Audio;
// using UnityEngine.Playables;
//
// namespace PowerCellStudio
// {
//     public class UIEffectPlayer: BaseAudioPlayer
//     {
//         private ScriptPlayable<AudioParallelMixer> _mixer;
//
//         /// <summary>
//         /// UI音效播放工具
//         /// </summary>
//         /// <param name="name">自定义名称</param>
//         public UIEffectPlayer(string name)
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
//             var gameObj = new GameObject(string.IsNullOrEmpty(name)? "UIEffectPlayer" : name);
//             _audioSource = gameObj.AddComponent<AudioSource>();
//             _audioSource.spatialBlend = 0f;
//             GameObject.DontDestroyOnLoad(gameObj);
//             _curVolume = 1f;
//             _maxVolume = _audioSource.volume;
//             _graph = PlayableGraph.Create(string.IsNullOrEmpty(name)? "UIEffectPlayerGraph": $"{name}Graph");
//             _graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
//             _mixer = AudioParallelMixer.Create(_graph);
//             _mixer.GetBehaviour().enable = true;
//             _mixer.GetBehaviour().backgroundScale = 0.3f;
//             _graph.CreatePlayableOutput(_audioSource, _mixer, 0, string.IsNullOrEmpty(name)? "UIEffectPlayerOutput": $"{name}Output");
//             _graph.Play();
//             _audioSource.gameObject.AddComponent<PlayableGraphAutoDestroy>().graph = _graph;
//         }
//         
//         public override void DeInit()
//         {
//             if (_graph.IsValid()) _graph.Destroy();
//             if(_audioSource) GameObject.Destroy(_audioSource.gameObject);
//         }
//
//         public override void Reset()
//         {
//             _mixer.GetBehaviour().RemoveAll();
//             _audioSource.volume = _maxVolume;
//             _curVolume = 1f;
//         }
//
//         public int Play(AudioClip clip, bool isLoop = false)
//         {
//             var playableClip = AudioClipPlayable.Create(_graph, clip, isLoop);
//             var index = _mixer.GetBehaviour().AddClip(playableClip, 0);
//             playableClip.SetTime(0f);
//             playableClip.Play();
//             return index;
//         }
//
//         public void Remove(int index)
//         {
//             _mixer.GetBehaviour().RemoveInput(index);
//         }
//         
//         public void Remove(AudioClip clip)
//         {
//             _mixer.GetBehaviour().RemoveInput(clip.name);
//         }
//         
//         /// <summary>
//         /// 设置音频播放速度
//         /// </summary>
//         /// <param name="speedValue">归一化播放速度</param>
//         public void SetSpeed(float speedValue)
//         {
//             var newValue = Mathf.Max(0f, speedValue);
//             _mixer.SetSpeed(newValue);
//         }
//     }
// }