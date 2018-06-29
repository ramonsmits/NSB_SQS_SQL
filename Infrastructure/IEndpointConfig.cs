using NServiceBus;

namespace Infrastructure
{
    public interface IEndpointConfig
    {
        EndpointConfiguration BuildConfig();
    }
}
