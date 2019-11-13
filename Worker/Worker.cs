using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace Worker
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class Worker : StatelessService
    {
        private static readonly int MinFrequency = 1;
        private static readonly int MaxFrequency = 100_000_000; // 10,000 for ~100% CPU usage
        private static readonly int OperationsCount = 100_000;

        public SyncValue Frequency { get; }

        public Worker(StatelessServiceContext context)
            : base(context)
        {
            Frequency = new SyncValue(MinFrequency, MaxFrequency, MinFrequency);
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                            .UseKestrel()
                            .ConfigureServices(
                                services => services
                                    .AddSingleton<SyncValue>(this.Frequency)
                                    .AddSingleton(serviceContext))
                            .UseContentRoot(Directory.GetCurrentDirectory())
                            .UseStartup<Startup>()
                            .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                            .UseUrls(url)
                            .Build();
                    }))
            };
        }

        protected override async Task RunAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                var clock = Task.Delay(TimeSpan.FromSeconds(1), token);

                using (var workCancellation = new CancellationTokenSource())
                {
                    var work = Work(workCancellation.Token);

                    await clock;

                    workCancellation.Cancel();
                    await work;
                }
            }
        }

        private Task Work(CancellationToken token)
        {
            return Task.Run(() =>
            {
                foreach (var i in Enumerable.Range(0, this.Frequency.Value))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    Task.Run(workInstance);
                }
            }, token);
        }

        /// <summary>
        /// CPU power usage.
        /// </summary>
        private readonly Action workInstance = () =>
        {
            var random = new Random();

            foreach (var number in Enumerable.Range(0, OperationsCount))
            {
                var result = number * random.Next(0, OperationsCount);
            }
        };
    }
}
