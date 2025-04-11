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
    public class Wave : AnimationProcess
    {
        public float Intensity = 1;
        public AnimationCurve AnimationCurve;

        protected override string OnSetTag()
        {
            return "wave";
        }

        protected override void OnProcessCreate()
        {
            AnimationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
            AnimationCurve.postWrapMode = WrapMode.PingPong;
            AnimationCurve.preWrapMode = WrapMode.PingPong;
            CharacterTimeOffset = 100;
            Intensity = 5;
        }

        protected override Character OnProgress(Character character)
        {
            character.Transform.Move(Vector3.up * AnimationCurve.Evaluate(Time) * Intensity);
            return character;
        }
    }
}