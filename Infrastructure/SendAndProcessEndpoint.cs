﻿using NServiceBus;
using System;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class SendAndProcessEndpoint<T>
          where T : BaseEndpointConfig
    {
        readonly BaseEndpointConfig _endPointConfig;
        EndpointConfiguration _nsbConfig;
        IEndpointInstance _endpointInstance;

        public SendAndProcessEndpoint(T config)
        {
            if (config == null)
                throw new ArgumentNullException("Endpoint configuration cannot be null");

            _endPointConfig = config;
        }

        public virtual void Initialize()
        {
            _nsbConfig = _endPointConfig.BuildConfig();
        }

        public virtual async Task StartEndpoint()
        {
            _endpointInstance = await Endpoint.Start(_nsbConfig).ConfigureAwait(false);
        }

        public virtual void StopEndpoint()
        {
            if (_endpointInstance != null)
                _endpointInstance.Stop().GetAwaiter().GetResult();
        }

        public virtual Task SendMessage(ICommand message)
        {
            return _endpointInstance != null
                ? _endpointInstance.Send(message) 
                : Task.CompletedTask;
        }
    }
}
