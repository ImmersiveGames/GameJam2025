using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityScannerRegistry
    {
        private readonly Dictionary<string, IActivityCapabilityScanner> _scannersById = new(StringComparer.Ordinal);
        private readonly List<IActivityCapabilityScanner> _orderedScanners = new();

        public IReadOnlyList<IActivityCapabilityScanner> OrderedScanners => _orderedScanners;
        public int Count => _orderedScanners.Count;

        public void Register(IActivityCapabilityScanner scanner)
        {
            if (scanner == null)
            {
                throw new ArgumentNullException(nameof(scanner));
            }

            string scannerId = scanner.ScannerId.TrimToEmpty();
            if (string.IsNullOrWhiteSpace(scannerId))
            {
                throw new InvalidOperationException("Activity capability scanner must provide a non-empty scannerId.");
            }

            if (_scannersById.ContainsKey(scannerId))
            {
                throw new InvalidOperationException($"Activity capability scanner '{scannerId}' is already registered.");
            }

            _scannersById.Add(scannerId, scanner);
            _orderedScanners.Add(scanner);
            _orderedScanners.Sort(CompareScanners);
        }

        private static int CompareScanners(IActivityCapabilityScanner left, IActivityCapabilityScanner right)
        {
            int orderCompare = left.Order.CompareTo(right.Order);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            return string.Compare(left.ScannerId, right.ScannerId, StringComparison.Ordinal);
        }
}
}
