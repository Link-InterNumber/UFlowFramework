using UnityEngine;

namespace PowerCellStudio
{
    public class ACTController : MonoBehaviour
    {
        public ACTConfig config;
        private ACTAction currentAction;
        
        private void Update()
        {
            ProcessInput();
            UpdateHitBoxes();
        }

        void ProcessInput()
        {
            foreach (var transition in currentAction.transitions)
            {
                if (CheckCondition(transition))
                {
                    PlayAction(transition.targetAction);
                    break;
                }
            }
        }

        bool CheckCondition(TransitionCondition condition)
        {
            switch (condition.inputType)
            {
                case InputType.AttackButton:
                    return Input.GetButtonDown("Fire1");
                // 其他条件判断...
            }
            return false;
        }

        void PlayAction(string actionName)
        {
            currentAction = config.actions.Find(a => a.actionName == actionName);
            // 播放动画逻辑...
        }

        void UpdateHitBoxes()
        {
            // 根据当前动画帧更新碰撞体
            // 实际需要根据动画播放进度计算当前帧
            int currentFrame = (int)(Time.time * 60);
            
            foreach (var hitFrame in currentAction.hitFrames)
            {
                if (currentFrame == hitFrame.frame)
                {
                    // 生成碰撞检测逻辑...
                }
            }
        }
    }
}