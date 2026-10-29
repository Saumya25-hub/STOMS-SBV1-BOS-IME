using System;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum MovementType
    {
        StockIn,
        StockOut,
        Transfer,
        Return,
        Damage,
        Adjustment
    }

    public enum MovementStatus
    {
        Completed,
        PendingApproval,
        InTransit,
        Rejected
    }

    public class StockMovement
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public MovementType Type { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Pcs";
        public string SourceLocation { get; set; } = string.Empty;
        public string DestinationLocation { get; set; } = string.Empty;
        public string InitiatedBy { get; set; } = string.Empty;
        public MovementStatus Status { get; set; }
        public string Remarks { get; set; } = string.Empty;

        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm");

        public string SignedQuantityDisplay => Type switch
        {
            MovementType.StockIn => $"+{Quantity:N0} {Unit}",
            MovementType.StockOut => $"-{Quantity:N0} {Unit}",
            MovementType.Return => $"+{Quantity:N0} {Unit}",
            MovementType.Damage => $"-{Quantity:N0} {Unit}",
            _ => $"{Quantity:N0} {Unit}"
        };

        public string TypeDisplay => Type switch
        {
            MovementType.StockIn => "Stock In / Receipt",
            MovementType.StockOut => "Stock Out / Issue",
            MovementType.Transfer => "Warehouse Transfer",
            MovementType.Return => "Customer Return",
            MovementType.Damage => "Damage / Scrap",
            MovementType.Adjustment => "Stock Adjustment",
            _ => Type.ToString()
        };

        public string StatusDisplay => Status switch
        {
            MovementStatus.Completed => "Completed",
            MovementStatus.PendingApproval => "Pending Approval",
            MovementStatus.InTransit => "In Transit",
            MovementStatus.Rejected => "Rejected",
            _ => Status.ToString()
        };
    }
}

