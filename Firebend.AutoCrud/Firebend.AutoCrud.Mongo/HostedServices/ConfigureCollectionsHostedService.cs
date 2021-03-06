using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebend.AutoCrud.Mongo.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Firebend.AutoCrud.Mongo.HostedServices
{
    public class ConfigureCollectionsHostedService : IHostedService
    {
        private readonly ILogger<ConfigureCollectionsHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ConfigureCollectionsHostedService(IServiceProvider serviceProvider, ILogger<ConfigureCollectionsHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var collections = scope.ServiceProvider.GetService<IEnumerable<IConfigureCollection>>();

            if (collections != null)
            {
                _logger.LogDebug("Configuring Mongo Collections...");

                var configureTasks = collections.Select(x => x.ConfigureAsync(cancellationToken));

                await Task.WhenAll(configureTasks).ConfigureAwait(false);

                _logger.LogDebug("Finished Configuring Mongo Collections.");
            }
            else
            {
                _logger.LogError("No Collections to Configure, but Mongo still registered.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}