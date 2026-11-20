using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface ILocationSuggestionEngine
    {
        Task<List<LocationSuggestion>> SuggestLocationsAsync(
            LocationSuggestionRequest request,
            IEnumerable<Warehouse> warehouses,
            IEnumerable<Rack> racks,
            IEnumerable<Product> products);
    }

    public class LocationSuggestionEngine : ILocationSuggestionEngine
    {
        private static LocationSuggestionEngine? _instance;
        public static LocationSuggestionEngine Instance => _instance ??= new LocationSuggestionEngine();

        public Task<List<LocationSuggestion>> SuggestLocationsAsync(
            LocationSuggestionRequest request,
            IEnumerable<Warehouse> warehouses,
            IEnumerable<Rack> racks,
            IEnumerable<Product> products)
        {
            var suggestions = new List<LocationSuggestion>();
            var product = products.FirstOrDefault(p => p.Id.Equals(request.ProductId, StringComparison.OrdinalIgnoreCase) ||
                                                      p.Sku.Equals(request.ProductId, StringComparison.OrdinalIgnoreCase));

            var targetWarehouses = warehouses.Where(w => w.IsActive).ToList();
            if (!string.IsNullOrWhiteSpace(request.TargetWarehouseId))
            {
                targetWarehouses = targetWarehouses.Where(w => w.Id == request.TargetWarehouseId || w.Code == request.TargetWarehouseId).ToList();
            }

            foreach (var wh in targetWarehouses)
            {
                var whRacks = racks.Where(r => r.WarehouseId == wh.Id && r.IsActive).ToList();

                foreach (var rack in whRacks)
                {
                    // Check sections
                    foreach (var section in rack.Sections.Where(s => s.IsActive))
                    {
                        int score = 50; // Baseline score
                        var reasons = new List<string>();

                        bool isDefault = product != null &&
                            (product.DefaultWarehouseId == wh.Id || product.DefaultWarehouseName == wh.Name) &&
                            (product.DefaultRackId == rack.Id || product.DefaultRackCode == rack.RackCode) &&
                            (product.DefaultSectionId == section.Id || product.DefaultSectionCode == section.SectionCode);

                        if (isDefault)
                        {
                            score += 35;
                            reasons.Add("Configured Product Default Slot");
                        }

                        // Check existing stock affinity (grouping like SKUs together)
                        bool hasExistingStock = section.Products.Any(p => p.ProductId == request.ProductId || p.Sku == product?.Sku);
                        if (hasExistingStock)
                        {
                            score += 20;
                            reasons.Add("Consolidation: Existing SKU stock in this section");
                        }

                        // Check capacity & occupancy
                        decimal remainingCapacity = section.HasCapacityConfigured
                            ? Math.Max(0m, section.MaxCapacity!.Value - section.OccupiedCapacity)
                            : 9999m;

                        if (section.HasCapacityConfigured)
                        {
                            if (section.OccupiedCapacity >= section.MaxCapacity!.Value)
                            {
                                // Over capacity / full
                                continue; // Don't suggest full locations
                            }

                            if (remainingCapacity >= request.Quantity)
                            {
                                score += 15;
                                reasons.Add($"Sufficient capacity ({remainingCapacity:N0} {section.CapacityUnit} available)");
                            }
                            else
                            {
                                score -= 20;
                                reasons.Add("Partial capacity only");
                            }
                        }
                        else
                        {
                            reasons.Add("Flexible unconstrained capacity");
                        }

                        // Expiry handling & status check
                        if (section.WorstStatus == StockStatus.Expired || section.WorstStatus == StockStatus.Damaged)
                        {
                            // Avoid putting fresh stock in damaged/expired quarantine bays unless item is quarantined
                            score -= 30;
                            reasons.Add("Caution: Section contains quarantined or expired goods");
                        }

                        if (request.IsPerishable && section.TierLevel <= 2)
                        {
                            score += 10;
                            reasons.Add("Ground/Low tier convenient for high-turnover perishable goods");
                        }

                        // Clamp score 0 to 100
                        score = Math.Clamp(score, 10, 100);

                        suggestions.Add(new LocationSuggestion
                        {
                            LocationId = section.Id,
                            LocationCode = section.SectionCode,
                            WarehouseId = wh.Id,
                            WarehouseName = wh.Name,
                            RackId = rack.Id,
                            RackCode = rack.RackCode,
                            SectionCode = section.SectionCode,
                            LocationType = StorageLocationType.Section,
                            MatchScore = score,
                            SuggestionReason = string.Join("; ", reasons),
                            RemainingCapacity = remainingCapacity,
                            CapacityUnit = section.CapacityUnit,
                            CurrentOccupancyPercentage = section.OccupancyPercentage,
                            IsDefaultLocation = isDefault,
                            HasExistingProductStock = hasExistingStock
                        });
                    }
                }
            }

            // Return sorted by highest match score first
            return Task.FromResult(suggestions.OrderByDescending(s => s.MatchScore).Take(5).ToList());
        }
    }
}
