﻿using System;
using System.Threading;
using Firebend.AutoCrud.Core.Extensions;
using Firebend.AutoCrud.Mongo.Configuration;
using Firebend.AutoCrud.Mongo.Sample.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Firebend.AutoCrud.Mongo.Sample
{
    class Program
    {
        public static void Main(string[] args)
        {
            var cancellationToken = new CancellationTokenSource();
            
            using var host = CreateHostBuilder(args).Build();
            host.StartAsync(cancellationToken.Token).ContinueWith(task =>
            {
                Console.WriteLine("Sample complete. type 'quit' to exit.");
            }, cancellationToken.Token);

            while (!Console.ReadLine().Equals("quit", StringComparison.InvariantCultureIgnoreCase))
            {
                
            }
            
            Console.WriteLine("Quiting....");
            cancellationToken.Cancel();
            Console.WriteLine("Done!");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, builder) =>
                {
                    if (hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddUserSecrets("Firebend.AutoCrud");
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    new MongoEntityCrudGenerator(services, hostContext.Configuration.GetConnectionString("Mongo"))
                        .AddBuilder<Person, Guid>(person =>
                                person.WithDefaultDatabase("Samples")
                                .WithCollection("People")
                                .WithCrud()
                        ).Generate(services);
                    
                    services.AddHostedService<SampleHostedService>();
                    
                });
    }
}