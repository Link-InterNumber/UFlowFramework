/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using EnjoyGameClub.TextLifeFramework.Core;

namespace EnjoyGameClub.TextLifeFramework.Processes
{
    [Serializable]
    public class Rotate : AnimationProcess
    {
        public float RotateSpeed = 30;
        protected override string OnSetTag()
        {
            return "rotate";
        }

        protected override Character OnProgress( Character character)
        {
            character.Transform.Rotate(Time * RotateSpeed);
            return character;
        }
    }
}