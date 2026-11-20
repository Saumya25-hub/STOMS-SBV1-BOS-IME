using System;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class Warehouse
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string CityLocation { get; set; } = string.Empty;
        public string WarehouseType { get; set; } = "Main Distribution Center";
        public double? TotalFloorArea { get; set; } // in sq. meters
        public double LayoutWidth { get; set; } = 1200;
        public double LayoutHeight { get; set; } = 800;
        public int TotalRacksCount { get; set; }
        public decimal? TotalCapacity { get; set; }
        public decimal OccupiedCapacity { get; set; }
        public string Unit { get; set; } = "Tons";
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
        public int LayoutRevision { get; set; } = 1;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = "Admin";

        public string DisplayName => $"{Code} — {Name} ({CityLocation})";
        public string StatusDisplay => IsActive ? "Active Facility" : "Deactivated";
    }
}


