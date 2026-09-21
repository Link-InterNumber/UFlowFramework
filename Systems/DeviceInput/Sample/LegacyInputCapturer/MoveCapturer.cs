using UnityEngine;

namespace UFlowFramework.Sample
{
    public class MoveCapturer : ILegacyInputCapturer<LegacyInputKey>
    {
        private bool isMove;
        
        public bool TryGetEvent(out InputEvent<LegacyInputKey> inputEvent)
        {
            var moveDir = Vector2.zero;
            
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.W))
            {
                moveDir.y += 1;
            }
            if (Input.GetKey(KeyCode.S))
            {
                moveDir.y += -1;
            }
            if (Input.GetKey(KeyCode.A))
            {
                moveDir.x += -1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                moveDir.x += 1;
            }
#endif

            var moveState = InputEventState.Released;
            if (isMove && moveDir == Vector2.zero)
            {
                moveState = InputEventState.ReleaseThisFrame;
                isMove = false;
            }
            else if (!isMove && moveDir != Vector2.zero)
            {
                moveState = InputEventState.PerformThisFrame;
                isMove = true;
            }
            else if (isMove && moveDir != Vector2.zero)
            {
                moveState = InputEventState.Hold;
            }
            inputEvent = InputEvent<LegacyInputKey>.CreateVector2Event(LegacyInputKey.Move, moveState, moveDir);
            return true;
        }

        public void Dispose()
        {
            isMove = false;
        }
    }
}