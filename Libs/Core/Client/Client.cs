using Core.FSM;
using Core.Network.Session;

namespace Core.Client
{
    public class Client : Session, IFSM
    {
        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        public Client() : base(false) { }

        //---------------------------------------------------------------------------------------------------
        #region FSM

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Mutex FSM.
        /// </summary>
        private readonly object m_FSMLock = new object();
        
        /// <summary>
        /// Current State.
        /// </summary>
        public FSMState m_currentState { get; set; }
        
        /// <summary>
        /// FSM.
        /// </summary>
        public FSMClass m_FSM { get; set; }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set the FSM.
        /// </summary>
        /// <param name="fsm"></param>
        public void SetFSM(FSMClass fsm)
        {
            lock (m_FSMLock)
            {
                m_FSM = fsm;
                m_currentState = m_FSM.GetState(1);
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get the FSM.
        /// </summary>
        /// <returns></returns>
        public FSMClass GetFSM()
        {
            lock (m_FSMLock)
            {
                return m_FSM;
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get current State ID.
        /// </summary>
        /// <returns></returns>
        public int GetStateID()
        {
            lock (m_FSMLock)
            {
                return m_currentState.GetID();
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// State Transition.
        /// </summary>
        /// <param name="nInput"></param>
        public void StateTransition(int nInput)
        {
            lock (m_FSMLock)
            {
                m_currentState = m_FSM.GetState(m_currentState.GetOutput(nInput));
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Force state transition to new state.
        /// </summary>
        /// <param name="nStateID"></param>
        public void ForceStateTransitionTo(int nStateID)
        {
            lock (m_FSMLock)
            {
                m_currentState = m_FSM.GetState(nStateID);
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Verify if has the current state.
        /// </summary>
        /// <param name="nStateArgs"></param>
        /// <returns></returns>
        public bool VerifyState(params int[] nStateArgs)
        {
            bool bRet = false;

            lock (m_FSMLock)
            {
                bRet = m_currentState.CheckValidState(nStateArgs);
            }

            return bRet;
        }
        #endregion

        //---------------------------------------------------------------------------------------------------
    }
}
