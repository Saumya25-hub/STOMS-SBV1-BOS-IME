using System;
using System.Collections.Generic;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class StockReceiveDto
    {
        public string ProductId { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Pcs";
        public string? BatchNumber { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public List<string> SerialNumbers { get; set; } = new();
        public string WarehouseId { get; set; } = string.Empty;
        public string RackId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string? CompartmentCode { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = "Current User";
    }

    public class StockIssueDto
    {
        public string ProductId { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Pcs";
        public string WarehouseId { get; set; } = string.Empty;
        public string RackId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public List<string> SelectedSerialNumbers { get; set; } = new();
        public string Reason { get; set; } = "Production Dispatch";
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string IssuedBy { get; set; } = "Current User";
        public bool RequiresApproval { get; set; }
    }

    public class StockReceiveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public decimal NewPhysicalStock { get; set; }
        public decimal NewAvailableStock { get; set; }
    }

    public class StockIssueResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public bool IsPendingApproval { get; set; }
        public string? ApprovalRequestId { get; set; }
        public decimal NewPhysicalStock { get; set; }
        public decimal NewAvailableStock { get; set; }
    }

    public class ProductFilterCriteria
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string Category { get; set; } = "All Categories";
        public ProductTrackingType? TrackingType { get; set; }
        public StockStatus? Status { get; set; }
        public string WarehouseId { get; set; } = "All Warehouses";
        public bool? IsActive { get; set; } = true;
    }
}
