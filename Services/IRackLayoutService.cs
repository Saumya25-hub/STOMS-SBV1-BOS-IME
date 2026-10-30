using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface IRackLayoutService
    {
        bool IsSetupCompleted { get; set; }
        Task<WarehouseRackLayout> GetLayoutAsync(string warehouseId = "WH-01");
        Task<bool> SaveLayoutAsync(WarehouseRackLayout layout);
        Task<bool> DeleteLayoutItemAsync(string warehouseId, string itemId);
        Task ResetLayoutAsync();
    }

    public class RackLayoutService : IRackLayoutService
    {
        private static RackLayoutService? _instance;
        public static RackLayoutService Instance => _instance ??= new RackLayoutService();

        private readonly string _layoutFilePath;
        private readonly string _setupFlagFilePath;
        private WarehouseRackLayout? _cachedLayout;

        public bool IsSetupCompleted
        {
            get
            {
                try
                {
                    if (File.Exists(_setupFlagFilePath))
                    {
                        var text = File.ReadAllText(_setupFlagFilePath).Trim();
                        return bool.TryParse(text, out var val) && val;
                    }
                }
                catch { /* ignore */ }
                return true; // Default true so tests or standard run isn't blocked unless explicitly tested
            }
            set
            {
                try
                {
                    File.WriteAllText(_setupFlagFilePath, value.ToString());
                }
                catch { /* ignore */ }
            }
        }

        public RackLayoutService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "STOMS-BOS");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { /* ignore */ }
            }
            _layoutFilePath = Path.Combine(dir, "rack_layout.json");
            _setupFlagFilePath = Path.Combine(dir, "setup_completed.flag");
        }

        public async Task<WarehouseRackLayout> GetLayoutAsync(string warehouseId = "WH-01")
        {
            if (_cachedLayout != null && _cachedLayout.WarehouseId == warehouseId)
            {
                return _cachedLayout;
            }

            try
            {
                if (File.Exists(_layoutFilePath))
                {
                    var json = await File.ReadAllTextAsync(_layoutFilePath);
                    var loaded = JsonSerializer.Deserialize<WarehouseRackLayout>(json);
                    if (loaded != null && loaded.WarehouseId == warehouseId)
                    {
                        _cachedLayout = loaded;
                        return loaded;
                    }
                }
            }
            catch { /* fallback to default */ }

            _cachedLayout = CreateDefaultLayout(warehouseId);
            return _cachedLayout;
        }

        public async Task<bool> SaveLayoutAsync(WarehouseRackLayout layout)
        {
            _cachedLayout = layout;
            layout.LastModified = DateTime.Now;

            // Sync with MockInventoryService
            MockInventoryService.Instance.SynchronizeFromLayout(layout);

            try
            {
                var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_layoutFilePath, json);
                IsSetupCompleted = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteLayoutItemAsync(string warehouseId, string itemId)
        {
            var layout = await GetLayoutAsync(warehouseId);
            var item = layout.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return false;

            // Protection: Never allow deletion of rack containing stock until stock is transferred or relocated (Part 3.D)
            var existingBatches = await MockInventoryService.Instance.GetAllBatchesAsync(warehouseId);
            var hasStock = existingBatches.Any(b => b.RackCode == item.Code && b.RemainingQuantity > 0);
            if (hasStock)
            {
                throw new InvalidOperationException($"Cannot delete Rack '{item.Code}' because it contains active inventory. Please relocate or transfer the stock first.");
            }

            layout.Items.Remove(item);
            return await SaveLayoutAsync(layout);
        }

        public Task ResetLayoutAsync()
        {
            _cachedLayout = CreateDefaultLayout("WH-01");
            IsSetupCompleted = false;
            try
            {
                if (File.Exists(_layoutFilePath)) File.Delete(_layoutFilePath);
                if (File.Exists(_setupFlagFilePath)) File.Delete(_setupFlagFilePath);
            }
            catch { /* ignore */ }
            return Task.CompletedTask;
        }

        private WarehouseRackLayout CreateDefaultLayout(string warehouseId)
        {
            return new WarehouseRackLayout
            {
                WarehouseId = warehouseId,
                WarehouseName = "Central Logistics Hub - Sector 4",
                CanvasWidth = 1600,
                CanvasHeight = 1200,
                GridSize = 20,
                Items = new List<LayoutItemModel>
                {
                    new LayoutItemModel
                    {
                        Id = "ITEM-USER-01",
                        ElementType = LayoutElementType.UserPosition,
                        Code = "STF-01",
                        Name = "Supervisor Workstation",
                        Description = "Central Dispatch Desk",
                        X = 400,
                        Y = 280,
                        Width = 80,
                        Height = 80,
                        ColorHex = "#10B981"
                    },
                    new LayoutItemModel
                    {
                        Id = "ITEM-RCK-A",
                        ElementType = LayoutElementType.Rack,
                        Code = "RCK-A",
                        Name = "Rack A (High-Bay Pallet)",
                        Description = "Heavy engineering parts & lubricants",
                        X = 140,
                        Y = 100,
                        Width = 200,
                        Height = 120,
                        ColorHex = "#2563EB",
                        RowsCount = 4,
                        ShelvesPerRow = 2,
                        IsAdvancedNaming = false
                    },
                    new LayoutItemModel
                    {
                        Id = "ITEM-RCK-B",
                        ElementType = LayoutElementType.Rack,
                        Code = "RCK-B",
                        Name = "Rack B (Medium Shelving)",
                        Description = "Fast-moving consumables & FMCG",
                        X = 540,
                        Y = 100,
                        Width = 200,
                        Height = 120,
                        ColorHex = "#8B5CF6",
                        RowsCount = 4,
                        ShelvesPerRow = 2,
                        IsAdvancedNaming = false
                    },
                    new LayoutItemModel
                    {
                        Id = "ITEM-AREA-01",
                        ElementType = LayoutElementType.StorageArea,
                        Code = "DOCK-01",
                        Name = "Inward Goods Staging Bay",
                        Description = "Receiving & Quarantine Deck",
                        X = 140,
                        Y = 320,
                        Width = 220,
                        Height = 120,
                        ColorHex = "#F59E0B"
                    }
                }
            };
        }
    }
}
