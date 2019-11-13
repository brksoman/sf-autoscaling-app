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
using Microsoft.ServiceFabric.Data;
using System.Fabric.Description;
using Microsoft.ServiceFabric.Data.Collections;

namespace VotingData
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class VotingData : StatefulService
    {
        private static readonly int OperationsCount = 10_000;

        public VotingData(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatefulServiceContext>(serviceContext)
                                            .AddSingleton<IReliableStateManager>(this.StateManager))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                this.CreateTasks();
                Thread.Sleep(1_000);
            }
        }

        private async void CreateTasks()
        {
            var dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("load");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var frequency = await dictionary.TryGetValueAsync(tx, "frequency");

                if (frequency.HasValue)
                {
                    Enumerable.Range(0, frequency.Value).Select(x =>
#pragma warning disable CS4014 // Task is not awaited because its purpose is just to increase CPU usage
                        Task.Run(workload))
#pragma warning restore CS4014
                    .GetEnumerator();
                }
            }
        }

        private readonly Action workload = new Action(() =>
        {
            var random = new Random();
            Enumerable
                .Range(0, OperationsCount)
                .Select(num =>
                    num * random.Next(0, OperationsCount))
                .GetEnumerator();
        });

        private async void InitAutoscale_Test()
        {
            // Implemented through ApplicationManifest.xml (Voting/ApplicationPackageRoot/)
            // TODO Where to call this?

            using (var client = new FabricClient())
            {
                var description = new StatefulServiceDescription()
                {
                    ServiceName = new Uri("fabric:/Voting/VotingData"),
                    ServiceTypeName = "VotingDataType",
                    HasPersistedState = true,

                    // TODO More setup?

                    ServicePackageActivationMode = ServicePackageActivationMode.ExclusiveProcess
                };

                description.ScalingPolicies.Add(
                    new ScalingPolicyDescription(
                        new PartitionInstanceCountScaleMechanism()
                        {
                            MaxInstanceCount = 6,
                            MinInstanceCount = 1,
                            ScaleIncrement = 1
                        },
                        new AveragePartitionLoadScalingTrigger()
                        {
                            MetricName = "servicefabric:/_CpuCores",
                            ScaleInterval = TimeSpan.FromSeconds(20),
                            LowerLoadThreshold = 1.0,
                            UpperLoadThreshold = 2.0
                        }));

                await client.ServiceManager.CreateServiceAsync(description);
            }
        }
    }
}
