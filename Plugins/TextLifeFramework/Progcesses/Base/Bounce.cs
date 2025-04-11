/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using EnjoyGameClub.TextLifeFramework.Core;
using UnityEngine;

namespace EnjoyGameClub.TextLifeFramework.Processes
{
    [System.Serializable]
    public class Bounce : AnimationProcess
    {
        public float Intensity = 10;

        public AnimationCurve BounceCurve;

        protected override string OnSetTag()
        {
            return "bounce";
        }

        protected override void OnProcessCreate()
        {
            BounceCurve = new AnimationCurve(
                new Keyframe(0, 1f),
                new Keyframe(0.2f, 0f),
                new Keyframe(0.275f, 0.2f),
                new Keyframe(0.35f, 0f),
                new Keyframe(0.8f, 0f),
                new Keyframe(1f, 1f)
            );
            CharacterTimeOffset = 100;
            BounceCurve.preWrapMode = WrapMode.Loop;
            BounceCurve.postWrapMode = WrapMode.Loop;
        }

        protected override Character OnProgress(Character character)
        {
            // 计算动画进度（归一化时间）
            // 通过 AnimationCurve 获取偏移值
            float yOffset = BounceCurve.Evaluate(Time) * Intensity;
            // 让字符沿 Y 轴移动
            character.Transform.Move(new Vector3(0, yOffset, 0));
            return character;
        }
    }
}