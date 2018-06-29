using Infrastructure;
using NServiceBus;

namespace API
{
    class APIConfig : BaseEndpointConfig
    {
        public APIConfig() :
            this(null, true)
        {
        }

        public APIConfig(string endpointName, bool isSendOnly) : 
            base(endpointName, isSendOnly)
        {
        }

        public override EndpointConfiguration BuildConfig()
        {
            var config= base.BuildConfig();
            config.MakeInstanceUniquelyAddressable("1");
            return config;
        }
    }
}
