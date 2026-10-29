namespace STOMS_SBV1_BOS_IME.Models
{
    public class Warehouse
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CityLocation { get; set; } = string.Empty;
        public int TotalRacksCount { get; set; }
        public decimal? TotalCapacity { get; set; }
        public decimal OccupiedCapacity { get; set; }
        public string Unit { get; set; } = "Tons";
        public bool IsActive { get; set; } = true;

        public string DisplayName => $"{Code} — {Name} ({CityLocation})";
    }
}

