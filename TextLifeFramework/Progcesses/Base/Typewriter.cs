/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using EnjoyGameClub.TextLifeFramework.Core;
using EnjoyGameClub.TextLifeFramework.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace EnjoyGameClub.TextLifeFramework.Processes
{
    [Serializable]
    public class Typewriter : AnimationProcess
    {
        private float[] _characterAnimationTime = new float[1];
        private bool[] _characterVisibleState = new bool[1];
        public float AnimationSmooth = 0.8f;
        public ProcessList OnAppearProcess = new();
        public UnityEvent OnTextVisible;
        public TMP_Text TMPText;

        protected override void OnProcessCreate()
        {
            var alphaProcess = new Fade
            {
                AlphaCurve = new AnimationCurve(new Keyframe(0f, 0f),
                    new Keyframe(0.25f, 1f), new Keyframe(1f, 1f))
            };
            var scaleProcess = new Scale
            {
                AnimationCurve = new AnimationCurve(new Keyframe(0f, 0f),
                    new Keyframe(0.25f, 1f), new Keyframe(0.5f, 0f), new Keyframe(1f, 0f))
            };
            OnAppearProcess.ProcessesList.Add(alphaProcess);
            OnAppearProcess.ProcessesList.Add(scaleProcess);
            // 100ms = 0.1s
            CharacterTimeOffset = 100;
        }

        protected override string OnSetTag()
        {
            return "typewriter";
        }

        protected override void OnReset()
        {
            if (_characterAnimationTime == null)
            {
                return;
            }

            Array.Fill(_characterAnimationTime, 0, 0, _characterAnimationTime.Length);
            Array.Fill(_characterVisibleState, false, 0, _characterAnimationTime.Length);
        }

        private void ResetData(Character character)
        {
            if (_characterAnimationTime == null || _characterAnimationTime.Length < character.TotalCount)
            {
                _characterAnimationTime = new float[character.TotalCount];
                _characterVisibleState = new bool[character.TotalCount];
                TMPText = character.TMPComponent;
            }
        }

        protected override Character OnProgress(Character character)
        {
            if (_characterAnimationTime == null || _characterAnimationTime.Length < character.TotalCount)
            {
                ResetData(character);
            }

            // Calculate animation time
            float characterTime = Time / AnimationSmooth;
            float normalizedTime = Mathf.Clamp01(characterTime);
            if (!_characterVisibleState[character.CharIndex] && normalizedTime > 0)
            {
                OnTextVisible?.Invoke();
                _characterVisibleState[character.CharIndex] = true;
            }

            if (_characterAnimationTime[character.CharIndex] >= 1) return character;


            _characterAnimationTime[character.CharIndex] = normalizedTime;

            // Appear effect processes
            foreach (var animationProcess in OnAppearProcess.ProcessesList)
            {
                character = animationProcess.Progress(_characterAnimationTime[character.CharIndex], DeltaTime,
                    character);
            }

            return character;
        }

        /// <summary>
        /// 立即完成所有文本动画的展示。
        /// </summary>
        /// <remarks>
        /// Immediately completes all text animations.
        /// </remarks>
        public void CompleteImmediately()
        {
            Array.Fill(_characterAnimationTime, 1, 0, _characterAnimationTime.Length);
            Array.Fill(_characterVisibleState, true, 0, _characterVisibleState.Length);
        }
        /// <summary>
        /// 显示指定文本并重置动画状态。
        /// </summary>
        /// <remarks>
        /// Displays specified text and resets animation state.
        /// </remarks>
        /// <param name="text">要显示的文本</param>
        public void ShowText(string text)
        {
            TMPText.SetText(text, false);
            Reset();
        }
    }
}