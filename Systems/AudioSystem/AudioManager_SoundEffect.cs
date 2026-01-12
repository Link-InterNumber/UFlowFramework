using UnityEngine;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 请求播放效果音。
        /// Request to play an effect sound.
        /// </summary>
        /// <param name="clipRef">音频剪辑引用 / Audio clip reference</param>
        /// <param name="onUI">音效是否为UI音效 / Whether the effect is a UI effect</param>
        /// <param name="attached">附加的游戏对象 / Game object to attach</param>
        /// <param name="position">音效位置 / Position of the effect</param>
        /// <param name="full3D">音效是否为全3D / Whether the effect is full 3D</param>
        public void RequestPlayEffect(string clipRef, bool onUI, GameObject attached, Vector3 position, bool full3D)
        {
            var newQuest = new AudioRequest(clipRef,
                onUI ? (int)AudioSourceType.SFXUI : (int)AudioSourceType.SFX3D,
                false)
            {
                attachGameObject = attached,
                position = position,
                full3D = full3D
            };
            _masterPipeline.PushRequest(newQuest);
        }
    }
}