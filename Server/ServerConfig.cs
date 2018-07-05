using Infrastructure;

namespace Server
{
    class ServerConfig : BaseEndpointConfig
    {
        public ServerConfig() :
            this(null, false)
        {
        }

        public ServerConfig(string endpointName, bool isSendOnly) :
            base(endpointName, isSendOnly)
        {
        }
    }
}
