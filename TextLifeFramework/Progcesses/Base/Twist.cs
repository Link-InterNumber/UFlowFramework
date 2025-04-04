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
    public class Twist : AnimationProcess
    {
        public float Intensity = 2;
        private const float TWIST_PARAM = 2.5f;

        protected override string OnSetTag()
        {
            return "twist";
        }

        protected override Character OnProgress(Character character)
        {
            for (int i = 0; i < character.Transform.Vertices.Length; i++)
            {
                character.Transform.Vertices[i] += Mathf.Sin((Time+(i* TWIST_PARAM)) * Mathf.PI) * Vector3.up *
                                                   Intensity;
            }
            return character;
        }
    }
}