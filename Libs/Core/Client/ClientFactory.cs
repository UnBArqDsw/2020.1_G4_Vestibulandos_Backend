using Core.FSM;
using Core.Network;

namespace Core.Client
{
    public class ClientFactory<TActorType, TFsmType> : IClientFactory
        where TFsmType : FSMClass, new()
        where TActorType : Core.Client.Client, new()
    {
        protected TFsmType m_spFSM;

        //---------------------------------------------------------------------------------------------------
        public ClientFactory()
        {
            m_spFSM = new TFsmType();
        }

        //---------------------------------------------------------------------------------------------------
        public static void DelActor(TActorType actor)
        {
            // TODO : ....
            actor?.Dispose();
            actor = null;
        }

        //---------------------------------------------------------------------------------------------------
        public T CreateClient<T>(int iFSMIndex) where T : Core.Client.Client
        {
            TActorType act = new TActorType();

            if (act != null)
            {
                act.SetFSM(m_spFSM);
                act.StateTransition(iFSMIndex);
            }

            return act as T;
        }
    }
}
