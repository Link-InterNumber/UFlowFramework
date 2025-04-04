/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using EnjoyGameClub.TextLifeFramework.Core;
using UnityEngine;

namespace EnjoyGameClub.TextLifeFramework.Processes
{
    [Serializable]
    public class Swing : AnimationProcess
    {
        public Vector3 Pivot = new Vector3(0.5f, 0, 0);
        public float SwingRange = 15;

        protected override string OnSetTag()
        {
            return "swing";
        }

        protected override Character OnProgress(Character character)
        {
            // 计算当前的旋转角度，使用正弦函数实现来回摇摆
            float angle = Mathf.Sin(Time * Mathf.PI) * SwingRange;
            // 旋转网格
            character.Transform.Rotate(angle, Pivot);
            return character;
        }
    }
}