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
    public class Shake : AnimationProcess
    {
        public float ShakeIntensity = 5;

        protected override string OnSetTag()
        {
            return "shake";
        }

        protected override void OnProcessCreate()
        {
            TimeScale = 10;
            CharacterTimeOffset = 800;
            ShakeIntensity = 5;
        }

        protected override Character OnProgress( Character character)
        {
            float offsetX = (Mathf.PerlinNoise(Time, 0) - 0.5f);
            float offsetY = (Mathf.PerlinNoise(0, Time) - 0.5f);
            Vector3 direction = new Vector3(offsetX, offsetY);
            character.Transform.Move(direction*ShakeIntensity);
            return character;
        }
    }
}