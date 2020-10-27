using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.FSM
{
    public class FSMState
    {
        /// <summary>
        /// Maximum number of states supported by this state.
        /// </summary>
        private uint m_uiNumberOfTransistions;

        /// <summary>
        /// Input array for tranistions.
        /// </summary>
        private int[] m_aiInputs;

        /// <summary>
        /// Output state array.
        /// </summary>
        private int[] m_aiOutputState;

        /// <summary>
        /// The unique ID of this state.
        /// </summary>
        private int m_iStateID;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Access the state ID.
        /// </summary>
        /// <returns></returns>
        public int GetID()
        {
            return m_iStateID;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        private FSMState() { }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Create a new instance and allocate arrays.
        /// </summary>
        /// <param name="nStateID"></param>
        /// <param name="uiTransitions"></param>
        public FSMState(int nStateID, uint uiTransitions)
        {
            // Don't allow 0 transitions.
            m_uiNumberOfTransistions = (uiTransitions == 0) ? 1 : uiTransitions;

            // Save off id and number of transitions.
            m_iStateID = nStateID;

            // Now allocate each array.
            try
            {
                m_aiInputs = new int[m_uiNumberOfTransistions];

                for (int nIndex = 0; nIndex < m_uiNumberOfTransistions; ++nIndex)
                {
                    m_aiInputs[nIndex] = 0;
                }
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                m_aiOutputState = new int[m_uiNumberOfTransistions];

                for (int nIndex = 0; nIndex < m_uiNumberOfTransistions; ++nIndex)
                {
                    m_aiOutputState[nIndex] = 0;
                }
            }
            catch (Exception)
            {
                //delete[] m_aiInputs;
                m_aiInputs = null;
                throw;
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Accept an input transition threshhold and the output state ID to associate with it.
        /// </summary>
        /// <param name="nInput"></param>
        /// <param name="nOutputID"></param>
        public void AddTransition(int nInput, int nOutputID)
        {
            // The m_aiInputs[] and m_aiOutputState[] are not sorted
            // so find the first non-zero offset in m_aiOutputState[]
            // and use that offset to store the input and OutputID
            // within the m_aiInputs[] and m_aiOutputState[].
            int nIndex = 0;

            for (nIndex = 0; nIndex < m_uiNumberOfTransistions; ++nIndex)
            {
                if (m_aiOutputState[nIndex] == 0)
                {
                    break;
                }
            }

            // Only a valid offset is used.
            if (nIndex < m_uiNumberOfTransistions)
            {
                m_aiOutputState[nIndex] = nOutputID;
                m_aiInputs[nIndex] = nInput;
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Remove an output state ID and its associated 
        /// input transition value from the arrays and zero out the slot used.
        /// </summary>
        /// <param name="iOutputID"></param>
        public void DeleteTransition(int iOutputID)
        {
            // The m_aiInputs[] and m_aiOutputState[] are not sorted
            // so find the offset of the output state ID to remove.
            int nIndex = 0;
            for (nIndex = 0; nIndex < m_uiNumberOfTransistions; ++nIndex)
            {
                if (m_aiOutputState[nIndex] == iOutputID)
                {
                    break;
                }
            }

            // Test to be sure the offset is valid.
            if (nIndex >= m_uiNumberOfTransistions)
            {
                return;
            }

            // Remove this output ID and its input transition value.
            m_aiInputs[nIndex] = 0;
            m_aiOutputState[nIndex] = 0;

            // Since the m_aiInputs[] and m_aiOutputState[] are not 
            // sorted, need to shift the remaining contents.
            for (; nIndex < (m_uiNumberOfTransistions - 1); ++nIndex)
            {
                // i => i+1
                if (m_aiOutputState[nIndex + 1] == 0)
                {
                    break;
                }

                m_aiInputs[nIndex] = m_aiInputs[nIndex + 1];
                m_aiOutputState[nIndex] = m_aiOutputState[nIndex + 1];
            }

            // Make sure the last used offset is cleared.
            m_aiInputs[nIndex] = 0;
            m_aiOutputState[nIndex] = 0;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Accepts an input transition value and finds the 
        /// input transition value stored in m_aiInputs[] that is associated
        /// with an output state ID and returns that output state ID.
        ///
        /// NOTE: this function acts as a state transition function and could
        /// be replaced with a different transition approach depending on the
        /// needs of your FSM.
        /// </summary>
        /// <param name="nInput"></param>
        /// <returns></returns>
        public int GetOutput(int nInput)
        {
            // Output state to be returned.
            int nOutputID = m_iStateID;

            // For each possible transistion.
            for (int nIndex = 0; nIndex < m_uiNumberOfTransistions; ++nIndex)
            {
                // Zeroed output state IDs indicate the end of the array.
                if (m_aiOutputState[nIndex] == 0)
                {
                    break;
                }

                // State transition function: look for a match with the input value.
                if (nInput == m_aiInputs[nIndex])
                {
                    // Output state id.
                    nOutputID = m_aiOutputState[nIndex];
                    break;
                }
            }

            // Returning either this m_iStateID to indicate no output 
            // state was matched by the input (ie. no state transition
            // can occur) or the transitioned output state ID.
            return (nOutputID);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Check current state.
        /// </summary>
        /// <param name="nStateArgs"></param>
        /// <returns></returns>
        public bool CheckValidState(params int[] nStateArgs)
        {
            return nStateArgs != null && nStateArgs.Any(t => t == m_iStateID);
        }
    }
}
