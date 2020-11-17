using System.Collections.Generic;

namespace Core.FSM
{
    public class FSMClass
    {
        /// <summary>
        /// Dictionary containing all states of this FSM
        /// </summary>
        private Dictionary<int, FSMState> m_dictState = new Dictionary<int, FSMState>();

        /// <summary>
        /// The m_iStateID of the current state.
        /// </summary>
        private int m_nCurrentState;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Construction method.
        /// </summary>
        public FSMClass() { }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Construction method.
        /// </summary>
        /// <param name="nStateID"></param>
        public FSMClass(int nStateID)
        {
            m_nCurrentState = nStateID;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Return the current state ID.
        /// </summary>
        /// <returns></returns>
        public int GetCurrentState()
        {
            return m_nCurrentState;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set current state.
        /// </summary>
        /// <param name="nStateID"></param>
        public void SetCurrentState(int nStateID)
        {
            m_nCurrentState = nStateID;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Perform a state transition based on input value
        /// passed and the current state, and return m_nCurrentState if there 
        /// is no matching output state for the input value, or 0 if there is
        /// some type of problem (like the current state not found).
        /// </summary>
        /// <param name="nInput"></param>
        /// <returns></returns>
        public int StateTransition(int nInput)
        {
            // The current state of the FSM must be set to have a transition.
            if (m_nCurrentState == 0)
            {
                return m_nCurrentState;
            }

            // Get the pointer to the FSMstate object that is the current state.
            FSMState pState = GetState(m_nCurrentState);

            // Check if found the state.
            if (pState == null)
            {
                // Signal that there is a problem.
                m_nCurrentState = 0;
                return m_nCurrentState;
            }

            // Now pass along the input transition value and let the FSMstate
            // do the really tough job of transitioning for the FSM, and save
            // off the output state returned as the new current state of the
            // FSM and return the output state to the calling process.
            m_nCurrentState = pState.GetOutput(nInput);

            // Return the current state.
            return m_nCurrentState;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Return the FSMstate object pointer referred to by the state ID passed.
        /// </summary>
        /// <param name="nStateID"></param>
        /// <returns></returns>
        public FSMState GetState(int nStateID)
        {
            // Try to get the value in the dictionary.
            m_dictState.TryGetValue(nStateID, out FSMState state);

            // Return the state.
            return state;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a FSMstate object pointer to the map.
        /// </summary>
        /// <param name="newState"></param>
        /// <returns></returns>
        public bool AddState(FSMState newState)
        {
            // Try to find this FSMstate in the dictionary.
            if (m_dictState.ContainsKey(newState.GetID()))
            {
                // Already has the state added in the dictionary.
                return false;
            }

            // Otherwise put the FSMstate object pointer into the map
            m_dictState.Add(newState.GetID(), newState);
            
            // Add with successfully.
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Delete a FSMstate object pointer from the map.
        /// </summary>
        /// <param name="nStateID"></param>
        public void DeleteState(int nStateID)
        {
            // Try to find this FSMstate in the map.
            m_dictState.TryGetValue(nStateID, out FSMState state);

            // confirm that the FSMstate is in the map.
            if (state != null && state.GetID() == nStateID)
            {
                // Remove it from the dictionary.
                m_dictState.Remove(nStateID);
            }
        }
    }
}
