/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */

namespace EnjoyGameClub.TextLifeFramework.Core
{
    public class StateMachine
    {
        private State _state;
        public State State => _state;

        public void ChangeState(State newState)
        {
            if (newState == null)
            {
                return;
            }

            //Exit old state.
            _state?.Exit();
            //Set new state.
            _state = newState;
            //Enter new state.
            _state.Enter();
        }

        public void UpdateState()
        {
            _state?.Update();
        }
    }
}