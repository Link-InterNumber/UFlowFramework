using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    [System.Serializable]
    public class AddForcePlayableAsset : PlayableAsset
    {
        public ExposedReference<Rigidbody2D> rigidbody2D;
        public Vector2 force;

        // Factory method that generates a playable based on this asset
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<AddForcePlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.rigidbody2D = rigidbody2D.Resolve(graph.GetResolver());
            // Debug.LogError(behaviour.rigidbody2D.gameObject.name);
            behaviour.force = force;
            return playable;
        }
    }
}

