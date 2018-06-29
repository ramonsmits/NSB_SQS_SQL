using Infrastructure;
using NServiceBus;

namespace Subscriber1
{
    class Subscriber1Config : BaseEndpointConfig
    {
        public Subscriber1Config() :
            this(null, false)
        {
        }

        public Subscriber1Config(string endpointName, bool isSendOnly) : 
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
