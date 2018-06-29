using Infrastructure;

namespace Client
{
    class ClientConfig : BaseEndpointConfig
    {
        public ClientConfig() :
            this(null, false)
        {
        }

        public ClientConfig(string endpointName, bool isSendOnly) : 
            base(endpointName, isSendOnly)
        {
        }
    }
}
