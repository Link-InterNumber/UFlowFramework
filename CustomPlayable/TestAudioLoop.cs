using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class TestAudioLoop : MonoBehaviour
    {
        public AudioClip clip;
        public AudioSource source;
        public Text showText;
        // private UIEffectPlayer _uiEffectPlayer;
        public Button btn;


        private void Awake()
        {
            // _uiEffectPlayer = new UIEffectPlayer("");
            // _uiEffectPlayer.Play(clip, false);
            btn.onClick.AddListener(SetLoopFalse);

            // _list = new List<int>();
            // _list[5] = 5;
        }

        private void Update()
        {
            showText.text = "";//$"isNull: {_clipPlayable.IsNull()}, isIsValid: {_clipPlayable.IsValid()}";
        }

        public void SetLoopFalse()
        {
            // _uiEffectPlayer.Play(clip, false);
            // _clipPlayable.SetSpeed(-1);
            // _clipPlayable.SetSpeed(0.1f);
            // _clipPlayable.SetTime(0f);
            // _clipPlayable.Play();
        }

        private void OnDestroy()
        {
            // _uiEffectPlayer.DeInit();
        }
    }
}