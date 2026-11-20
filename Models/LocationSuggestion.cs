using System;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class LocationSuggestionRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Pcs";
        public string? TargetWarehouseId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsPerishable { get; set; }
        public string PreferredZone { get; set; } = string.Empty;
    }

    public class LocationSuggestion
    {
        public string LocationId { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string RackId { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public string SectionCode { get; set; } = string.Empty;
        public StorageLocationType LocationType { get; set; } = StorageLocationType.Section;

        public int MatchScore { get; set; }
        public string SuggestionReason { get; set; } = string.Empty;
        public decimal RemainingCapacity { get; set; }
        public string CapacityUnit { get; set; } = "kg";
        public double CurrentOccupancyPercentage { get; set; }
        public bool IsDefaultLocation { get; set; }
        public bool HasExistingProductStock { get; set; }

        public string DisplayText => $"{WarehouseName} | Rack {RackCode} → Shelf {LocationCode} (Score: {MatchScore}/100)";
        public string ScoreBadge => $"{MatchScore}% Match";
    }
}
