using System;
using System.Collections.Generic;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class StockRelocationDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Pcs";
        public string? BatchId { get; set; }
        public string? BatchNumber { get; set; }
        public List<string> SerialNumbers { get; set; } = new();

        public string SourceWarehouseId { get; set; } = string.Empty;
        public string SourceRackCode { get; set; } = string.Empty;
        public string SourceLocationCode { get; set; } = string.Empty;

        public string DestinationWarehouseId { get; set; } = string.Empty;
        public string DestinationRackCode { get; set; } = string.Empty;
        public string DestinationLocationCode { get; set; } = string.Empty;

        public string Reason { get; set; } = "Warehouse Slotting Optimization";
        public string Reference { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = "Current User";
    }

    public class StockRelocationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal RemainingSourceQuantity { get; set; }
        public decimal NewDestinationQuantity { get; set; }
    }
}
