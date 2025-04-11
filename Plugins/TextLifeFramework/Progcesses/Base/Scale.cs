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
    public class Scale : AnimationProcess
    {
        public float Intensity = 0.5f;
        public Vector2 Pivot = new Vector2(0.5f, 0.5f);
        public AnimationCurve AnimationCurve = new();

        protected override void OnProcessCreate()
        {
            base.OnProcessCreate();
            AnimationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
            AnimationCurve.postWrapMode = WrapMode.PingPong;
            AnimationCurve.preWrapMode = WrapMode.PingPong;
        }

        protected override string OnSetTag()
        {
            return "scale";
        }

        protected override Character OnProgress( Character character)
        {
            float size = 1 + AnimationCurve.Evaluate(Time) * Intensity;
            character.Transform.Scale(new Vector2(size, size), Pivot);
            return character;
        }
    }
}