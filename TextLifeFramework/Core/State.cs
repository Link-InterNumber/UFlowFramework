/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace EnjoyGameClub.TextLifeFramework.Core
{
    [Serializable]
    public class State
    {
        public StateMachine stateMachine;
        public UnityEvent onStateEnter = new();
        public UnityEvent onStateExit = new();
        public UnityEvent onState = new();
        public Dictionary<State, Func<bool>> dict = new();
        public string Name;

        public State(StateMachine stateMachine, string name)
        {
            Name = name;
            this.stateMachine = stateMachine;
        }

        public void AddTranslation(State targetState, Func<bool> f)
        {
            dict ??= new();
            if (dict.ContainsKey(targetState))
            {
                dict[targetState] = f;
            }
            else
            {
                dict.Add(targetState, f);
            }
        }

        public void Enter()
        {
            onStateEnter?.Invoke();
        }

        public void Exit()
        {
            onStateExit?.Invoke();
        }

        public void Update()
        {
            onState?.Invoke();
            foreach (var singleTrans in dict.Where(singleTrans => singleTrans.Value.Invoke()))
            {
                stateMachine.ChangeState(singleTrans.Key);
                return;
            }
        }
    }
}