using Core.FSM;

namespace LoginServer.FSM
{
    //---------------------------------------------------------------------------------------------------
    /// <summary>
    /// User FSM State.
    /// </summary>
    public enum EnUserFSMState
    {
        STATE_ZERO_NO_USE,

        STATE_INIT = 1,
        STATE_CONNECTED = 2,
        STATE_EXIT = 3,
        STATE_LOGINED,

        STATE_SENTINEL
    }

    //---------------------------------------------------------------------------------------------------
    /// <summary>
    /// User FSM Input.
    /// </summary>
    public enum EnUserFSMInput
    {
        INPUT_CONNECT = 0,

        INPUT_VERIFICATION_OK,
        INPUT_EXIT_GAME,
    }

    public class UserFSM : FSMClass
    {
        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        public UserFSM()
            : base((int)EnUserFSMState.STATE_INIT)
        {
            // STATE_INIT
            FSMState state = new FSMState((int)EnUserFSMState.STATE_INIT, 1);
            state.AddTransition((int)EnUserFSMInput.INPUT_CONNECT, (int)EnUserFSMState.STATE_CONNECTED);
            if (AddState(state) == false) {
                state = null;
            }

            // STATE_CONNECTED
            state = new FSMState((int)EnUserFSMState.STATE_CONNECTED, 2);
            state.AddTransition((int)EnUserFSMInput.INPUT_VERIFICATION_OK, (int)EnUserFSMState.STATE_LOGINED);
            state.AddTransition((int)EnUserFSMInput.INPUT_EXIT_GAME, (int)EnUserFSMState.STATE_EXIT);
            if (AddState(state) == false) {
                state = null;
            }

            // STATE_LOGINED
            state = new FSMState((int)EnUserFSMState.STATE_LOGINED, 1);
            state.AddTransition((int)EnUserFSMInput.INPUT_EXIT_GAME, (int)EnUserFSMState.STATE_EXIT);
            if (AddState(state) == false) {
                state = null;
            }

            // STATE_EXIT
            state = new FSMState((int)EnUserFSMState.STATE_EXIT, 1);
            state.AddTransition((int)EnUserFSMInput.INPUT_CONNECT, (int)EnUserFSMState.STATE_CONNECTED);
            if (AddState(state) == false) {
                state = null;
            }
        }
    }
}
