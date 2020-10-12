using System;
using System.Threading;
using System.Threading.Tasks;
using Firebend.AutoCrud.Core.Interfaces.Services.Entities;
using Firebend.AutoCrud.Mongo.Sample.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ILogger = DnsClient.Internal.ILogger;

namespace Firebend.AutoCrud.Mongo.Sample
{
    public class SampleHostedService : IHostedService
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IEntityCreateService<Guid, Person> _createService;
        private readonly IEntityUpdateService<Guid, Person> _updateService;
        private readonly IEntityReadService<Guid, Person> _readService;
        private readonly ILogger<SampleHostedService> _logger;
        private JsonSerializer _serializer;

        public SampleHostedService(IServiceProvider serviceProvider, ILogger<SampleHostedService> logger)
        {
            _logger = logger;
            
            using var scope = serviceProvider.CreateScope();
            _createService = scope.ServiceProvider.GetService<IEntityCreateService<Guid, Person>>();
            _updateService = scope.ServiceProvider.GetService<IEntityUpdateService<Guid, Person>>();
            _readService = scope.ServiceProvider.GetService<IEntityReadService<Guid, Person>>();
            _serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _logger.LogInformation("Starting Sample...");

            try
            {
                var entity = await _createService.CreateAsync(new Person
                {
                    FirstName = $"First Name -{DateTimeOffset.UtcNow}",
                    LastName = $"Last Name -{DateTimeOffset.UtcNow}"
                }, _cancellationTokenSource.Token);

                LogObject("Entity added....");
                entity.FirstName = $"{entity.FirstName} - updated";
                var updated = await _updateService.UpdateAsync(entity, cancellationToken);
                LogObject("Entity updated...");

                var patch = new JsonPatchDocument<Person>();
                patch.Add(x => x.FirstName, $"{updated.FirstName} - patched");
                var patched = await _updateService.PatchAsync(updated.Id, patch, cancellationToken);
                LogObject("Entity patched...");

                var read = await _readService.GetByKeyAsync(patched.Id, cancellationToken);
                LogObject("Entity Read...", read);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sample");
            }
        }

        private void LogObject(string message, object entity = null)
        {
            _logger.LogInformation(message);
            
            if (entity != null)
            {
                _serializer.Serialize(Console.Out, entity);
                Console.WriteLine();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();

            return Task.CompletedTask;
        }
    }
}