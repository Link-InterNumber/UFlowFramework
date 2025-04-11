/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */

using EnjoyGameClub.TextLifeFramework.Core;
using UnityEngine;

public class Rainbow : AnimationProcess
{
    protected override string OnSetTag()
    {
        return "rainbow";
    }

    protected override void OnProcessCreate()
    {
        CharacterTimeOffset = 50;
        TimeScale = 0.1f;
    }

    protected override Character OnProgress(Character character)
    {
        float colorH1 = Time < 0 ? Time % 1 + 1 : Time % 1;
        float colorH2 = Time < 0
            ? (Time - CharacterTimeOffset * TIME_OFFSET_PARAM * TimeScale) % 1 + 1
            : (Time - CharacterTimeOffset * TIME_OFFSET_PARAM * TimeScale) % 1;
        var alpha = character.VerticesColor[0].a;
        Color32 color1 = Color.HSVToRGB(colorH1, 1, 1);
        Color32 color2 = Color.HSVToRGB(colorH2, 1, 1);
        color1.a = alpha;
        color2.a = alpha;
        character.VerticesColor[0] = color1;
        character.VerticesColor[1] = color1;
        character.VerticesColor[2] = color2;
        character.VerticesColor[3] = color2;
        return character;
    }
}