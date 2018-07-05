using Infrastructure;

namespace Client
{
    class ClientConfig : BaseEndpointConfig
    {
        public ClientConfig() :
            base("NSB.Client", false)
        {
        }
    }
}
