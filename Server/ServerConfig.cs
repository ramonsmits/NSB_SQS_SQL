using Infrastructure;

namespace Server
{
    class ServerConfig : BaseEndpointConfig
    {
        public ServerConfig() :
            base("NSB.Server", false)
        {
        }
    }
}
