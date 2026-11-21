using System.Collections.Generic;

namespace STOMS_SBV1_BOS_IME.Models
{
    /// <summary>
    /// Persistent storage state model serialized to %LocalAppData%\STOMS-BOS\inventory_database.json.
    /// Preserves user products, inward stock batches, racks, warehouses, and auditable movements permanently across app restarts.
    /// </summary>
    public class InventoryStorageState
    {
        public List<Product> Products { get; set; } = new();
        public List<StockBatch> Batches { get; set; } = new();
        public List<StockSerialNumber> SerialNumbers { get; set; } = new();
        public List<StockLocationBalance> LocationBalances { get; set; } = new();
        public List<StockMovement> Movements { get; set; } = new();
        public List<StockApprovalRequest> ApprovalRequests { get; set; } = new();
        public List<Warehouse> Warehouses { get; set; } = new();
        public List<Rack> Racks { get; set; } = new();
        public int AutoBatchSequence { get; set; } = 1;
    }
}
