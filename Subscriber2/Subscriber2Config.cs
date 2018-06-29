using Infrastructure;
using NServiceBus;

namespace Subscriber2
{
    class Subscriber2Config : BaseEndpointConfig
    {
        public Subscriber2Config() :
            this(null, false)
        {
        }
        public Subscriber2Config(string endpointName, bool isSendOnly) :
            base(endpointName, isSendOnly)
        {
        }

        public override EndpointConfiguration BuildConfig()
        {
            var config = base.BuildConfig();
            return config;
        }
    }
}
