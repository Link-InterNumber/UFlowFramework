/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using System.Collections.Generic;
using EnjoyGameClub.TextLifeFramework.Core;
using UnityEngine;

namespace EnjoyGameClub.TextLifeFramework.Editor
{
    [Serializable]
    public class ProcessList
    {
        [SerializeReference]
        public List<AnimationProcess> ProcessesList = new();
    }
}