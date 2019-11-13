using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ScalingService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class ScalingService : StatelessService
    {
        private static readonly int OperationsCount = 1_000;

        private static readonly int MinFrequency = 1;
        private static readonly int MaxFrequency = 100;

        public int frequency = 5;

        public Task<int> Frequency
        {
            get => Task.FromResult(frequency);
        }

        public Task SetFrequency(int value)
        {
            return Task.Run(() => frequency =
                value <= MinFrequency ? MinFrequency :
                value >= MaxFrequency ? MaxFrequency :
                value);
        }

        public ScalingService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                this.CreateTasks();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private void CreateTasks()
        {
            Enumerable.Range(0, frequency)
                .Select(x =>
#pragma warning disable CS4014 // Task is not awaited because its purpose is just to increase CPU usage
                    Task.Run(workload))
#pragma warning restore CS4014
                .GetEnumerator();
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

    }
}
