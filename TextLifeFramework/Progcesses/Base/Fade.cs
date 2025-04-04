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
    public class Fade : AnimationProcess
    {
        public AnimationCurve AlphaCurve;

        protected override void OnProcessCreate()
        {
            AlphaCurve = AnimationCurve.Linear(0, 0, 1, 1);
            AlphaCurve.postWrapMode = WrapMode.PingPong;
        }

        protected override string OnSetTag()
        {
            return "fade";
        }

        protected override Character OnProgress(Character character)
        {
            character.SetAlpha(AlphaCurve.Evaluate(Time));
            return character;
        }
    }
}