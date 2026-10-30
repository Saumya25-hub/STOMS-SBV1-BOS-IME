using System;
using System.Collections.Generic;

namespace STOMS_SBV1_BOS_IME.Services
{
    public enum AppLanguage
    {
        English,
        Hindi,
        Hinglish
    }

    public interface ILocalizationService
    {
        AppLanguage CurrentLanguage { get; }
        event EventHandler<AppLanguage>? LanguageChanged;
        void SetLanguage(AppLanguage language);
        string GetString(string key);
    }

    public class LocalizationService : ILocalizationService
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private AppLanguage _currentLanguage = AppLanguage.English;
        public AppLanguage CurrentLanguage => _currentLanguage;

        public event EventHandler<AppLanguage>? LanguageChanged;

        private readonly Dictionary<string, Dictionary<AppLanguage, string>> _translations = new()
        {
            ["Dashboard"] = new()
            {
                [AppLanguage.English] = "Home Dashboard",
                [AppLanguage.Hindi] = "होम डैशबोर्ड",
                [AppLanguage.Hinglish] = "Home Dashboard"
            },
            ["ProductCatalog"] = new()
            {
                [AppLanguage.English] = "Product Catalog",
                [AppLanguage.Hindi] = "उत्पाद सूची",
                [AppLanguage.Hinglish] = "Product Catalog"
            },
            ["StockBalances"] = new()
            {
                [AppLanguage.English] = "Stock Balances",
                [AppLanguage.Hindi] = "स्टॉक शेष",
                [AppLanguage.Hinglish] = "Stock Balances"
            },
            ["WarehouseRacks"] = new()
            {
                [AppLanguage.English] = "Warehouse & Racks",
                [AppLanguage.Hindi] = "गोदाम एवं रैक",
                [AppLanguage.Hinglish] = "Godam & Racks"
            },
            ["ReceiveStock"] = new()
            {
                [AppLanguage.English] = "Receive Stock (Inward)",
                [AppLanguage.Hindi] = "माल आवक (Inward)",
                [AppLanguage.Hinglish] = "Stock Receive Karein"
            },
            ["IssueStock"] = new()
            {
                [AppLanguage.English] = "Issue Stock (Outward)",
                [AppLanguage.Hindi] = "माल जावक (Outward)",
                [AppLanguage.Hinglish] = "Stock Issue Karein"
            },
            ["ProductName"] = new()
            {
                [AppLanguage.English] = "Product Name",
                [AppLanguage.Hindi] = "उत्पाद का नाम",
                [AppLanguage.Hinglish] = "Product Ka Naam"
            },
            ["AvailableStock"] = new()
            {
                [AppLanguage.English] = "Available Stock",
                [AppLanguage.Hindi] = "उपलब्ध स्टॉक",
                [AppLanguage.Hinglish] = "Kitna Stock Hai"
            },
            ["Quantity"] = new()
            {
                [AppLanguage.English] = "Quantity",
                [AppLanguage.Hindi] = "मात्रा",
                [AppLanguage.Hinglish] = "Kitni Quantity"
            },
            ["ExpiryDate"] = new()
            {
                [AppLanguage.English] = "Expiry Date",
                [AppLanguage.Hindi] = "समाप्ति तिथि",
                [AppLanguage.Hinglish] = "Expiry Kab Hai"
            },
            ["SelectRack"] = new()
            {
                [AppLanguage.English] = "Select Rack",
                [AppLanguage.Hindi] = "रैक चुनें",
                [AppLanguage.Hinglish] = "Rack Chune"
            },
            ["TotalValuation"] = new()
            {
                [AppLanguage.English] = "Total Inventory Valuation",
                [AppLanguage.Hindi] = "कुल इन्वेंट्री मूल्यांकन",
                [AppLanguage.Hinglish] = "Total Stock Valuation"
            }
        };

        public void SetLanguage(AppLanguage language)
        {
            if (_currentLanguage == language) return;
            _currentLanguage = language;
            LanguageChanged?.Invoke(this, _currentLanguage);
        }

        public string GetString(string key)
        {
            if (_translations.TryGetValue(key, out var dict) && dict.TryGetValue(_currentLanguage, out var val))
            {
                return val;
            }
            return key;
        }
    }
}

