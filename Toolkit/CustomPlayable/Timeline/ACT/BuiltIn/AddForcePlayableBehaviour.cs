using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    // A behaviour that is attached to a playable
    public class AddForcePlayableBehaviour : PlayableBehaviour
    {
        public Rigidbody2D rigidbody2D;
        public Vector2 force;
        
        // // Called when the owning graph starts playing
        // public override void OnGraphStart(Playable playable)
        // {
            
        // }

        // // Called when the owning graph stops playing
        // public override void OnGraphStop(Playable playable)
        // {
            
        // }

        // // Called when the state of the playable is set to Play
        // public override void OnBehaviourPlay(Playable playable, FrameData info)
        // {
            
        // }

        // // Called when the state of the playable is set to Paused
        // public override void OnBehaviourPause(Playable playable, FrameData info)
        // {
            
        // }

        // Called each frame while the state is set to Play
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);
            // var rigidbody2D = playerData as Rigidbody2D;
            rigidbody2D.AddForce(force);
            Debug.LogError(rigidbody2D.gameObject.name);
        }
    }
}


