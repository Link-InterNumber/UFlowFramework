using UnityEngine;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public interface IActTack
    {

    }

    public class ActManager : TempMonoSingleton<ActManager>
    {
        private LinkPool<TimelineActRunner> _runnerPool;
        private PlayableDirector _director;
        private IAssetLoader _loader;

        public override void Init(object data)
        {
            base.Init(data);
            _runnerPool = new LinkPool<TimelineActRunner>(() => new TimelineActRunner(), 5, 5);
            _director = gameObject.AddComponent<PlayableDirector>();
            _loader = AssetUtils.SpawnLoader("ActManager");
        }

        protected override void OnDestroy()
        {
            _runnerPool?.Dispose();
            AssetUtils.DeSpawnLoader(_loader);
            base.OnDestroy();
        }

        private TimelineActRunner GetRunner()
        {
            return _runnerPool.Get();
        }

        public void LoadAct(int actId, ActBinder actBinder)
        {

            // _loader.LoadAsync<PlayableAsset>(path, (asset) => 
            // {
            //      OnLoadActAsset(asset, actBinder);
            // });
        }

        private void OnLoadActAsset(PlayableAsset timelineAsset, ActBinder actBinder)
        {
            var runner = GetRunner();
            actBinder.actRunner = runner;
            Rebind(timelineAsset, actBinder);
            actBinder.actRunner.SetTimelineAsset(timelineAsset, actBinder.gameObject);
        }

        public void RemoveAct(ActBinder actBinder)
        {
            actBinder.actRunner.DeSpawn();
            actBinder.actRunner = null;
        }

        private void Rebind(PlayableAsset timelineAsset, ActBinder actBinder)
        {
            foreach (var output in timelineAsset.outputs)
            {
                // 判断类型和名称
                if (output.outputTargetType == typeof(Animator))
                {
                    _director.SetGenericBinding(output.sourceObject, actBinder.gameObject.GetComponent<Animator>());
                }
                if (output.sourceObject is IActTack)
                {
                    _director.SetGenericBinding(output.sourceObject, actBinder);
                }
            }
        }
    }
}