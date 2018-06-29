using Infrastructure;
using NServiceBus;

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

        public override EndpointConfiguration BuildConfig()
        {
            return base.BuildConfig();
        }
    }
}
