/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using UnityEditor;
using UnityEngine;

namespace EnjoyGameClub.TextLifeFramework.Core
{
    public partial class TextLife
    {
        public StateMachine stateMachine;

        private void InitStateMachine()
        {
            stateMachine = new StateMachine();
            State runtimeState = new State(stateMachine, "runtimeState");
            State debugState = new State(stateMachine, "editorState");
            State noneState = new State(stateMachine, "noneState");

            runtimeState.AddTranslation(noneState, () => !Application.isPlaying|| !_tempText.isActiveAndEnabled);
            debugState.AddTranslation(noneState, () => !Debug || Application.isPlaying);
            noneState.AddTranslation(runtimeState, () => Application.isPlaying&& _tempText.isActiveAndEnabled);
            noneState.AddTranslation(debugState, () => !Application.isPlaying && Debug);

            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingEditMode)
                {
                    Debug = false;
                    stateMachine.ChangeState(noneState);
                }
            };

            debugState.onStateEnter.AddListener(() =>
            {
#if UNITY_EDITOR
                EditorApplication.update += DebugUpdate;
                
#endif
                _originalText = _tempText.text;
                ReMatchText(Text);
            });
            debugState.onState.AddListener((() =>
            {
                
                RecordAnimationTime();
                if (HasDebugTextChanged())
                {
                    ReMatchText(Text);
                }
                ExecuteProcess();
            }));
            debugState.onStateExit.AddListener((() =>
            {
#if UNITY_EDITOR
                EditorApplication.update -= DebugUpdate;
#endif
                _tempText.SetText(_originalText, false);
                MatchProcesses.ProcessesList.Clear();
            }));

            runtimeState.onStateEnter.AddListener(()=>
            {
                ReMatchText(_tempText.text);
            });
            runtimeState.onState.AddListener((() =>
            {
                RecordAnimationTime();
                if (HasTMPTextChanged())
                {
                    ReMatchText(_tempText.text);
                }
                ExecuteProcess();
            }));
            stateMachine.ChangeState(noneState);
        }
#if UNITY_EDITOR
        private void DebugUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
        }
#endif

    }
}