namespace GameServer.FSM
{
    public interface IFSM
    {
        /// <summary>
        /// Current State.
        /// </summary>
        FSMState m_currentState { get; set; }

        /// <summary>
        /// FSM.
        /// </summary>
        FSMClass m_FSM { get; set; }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set the FSM.
        /// </summary>
        /// <param name="fsm"></param>
        void SetFSM(FSMClass fsm);

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get the FSM.
        /// </summary>
        /// <returns></returns>
        FSMClass GetFSM();

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get current state ID.
        /// </summary>
        /// <returns></returns>
        int GetStateID();

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// State transition.
        /// </summary>
        /// <param name="nInput"></param>
        void StateTransition(int nInput);

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Force state transition to new state.
        /// </summary>
        /// <param name="nStateID"></param>
        void ForceStateTransitionTo(int nStateID);
    }
}
