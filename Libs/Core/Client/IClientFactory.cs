namespace Core.Client
{
    public interface IClientFactory
    {
        //---------------------------------------------------------------------------------------------------
        T CreateClient<T>(int iFSMIndex) where T : Core.Client.Client;
    }
}
