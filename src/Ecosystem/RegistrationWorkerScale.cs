using System;

namespace WildFarming.Ecosystem
{
    /// <summary>Resolves registration worker count and scales per-worker throughput budgets.</summary>
    internal static class RegistrationWorkerScale
    {
        public const int MaxWorkers = 8;

        public static int Resolve(int configuredWorkerCount)
        {
            if (configuredWorkerCount <= 0)
            {
                configuredWorkerCount = Environment.ProcessorCount;
            }

            if (configuredWorkerCount < 1) configuredWorkerCount = 1;
            if (configuredWorkerCount > MaxWorkers) configuredWorkerCount = MaxWorkers;
            return configuredWorkerCount;
        }

        public static int Scale(int perWorker, int configuredWorkerCount)
        {
            if (perWorker <= 0) return 0;
            return perWorker * Resolve(configuredWorkerCount);
        }

        /// <summary>Convert a legacy absolute total to a per-worker config value.</summary>
        public static int ToPerWorker(int absoluteTotal, int configuredWorkerCount)
        {
            if (absoluteTotal <= 0) return absoluteTotal;

            int workers = Resolve(configuredWorkerCount);
            if (workers <= 1) return absoluteTotal;

            return Math.Max(1, absoluteTotal / workers);
        }
    }
}
