using System;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum UserRole
    {
        Administrator,
        WarehouseManager,
        InventorySupervisor,
        StockClerk,
        Auditor
    }

    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string AssignedWarehouse { get; set; } = "All Warehouses";
        public DateTime LastLogin { get; set; }
        public string DisplayRole => Role switch
        {
            UserRole.Administrator => "System Administrator",
            UserRole.WarehouseManager => "Warehouse Operations Manager",
            UserRole.InventorySupervisor => "Inventory Supervisor",
            UserRole.StockClerk => "Logistics & Stock Clerk",
            UserRole.Auditor => "Inventory Quality Auditor",
            _ => "User"
        };
    }
}

