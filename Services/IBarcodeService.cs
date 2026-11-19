using System;
using System.Linq;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public class BarcodePrintLabel
    {
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public BarcodeFormat Format { get; set; } = BarcodeFormat.Ean13;
        public decimal Price { get; set; }
        public string CurrencySymbol { get; set; } = "₹";
        public string Unit { get; set; } = "PCS";
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        public string PriceDisplay => $"{CurrencySymbol} {Price:N2} / {Unit}";
    }

    public interface IBarcodeService
    {
        string GenerateInternalBarcode(BarcodeFormat format = BarcodeFormat.Ean13, string? prefix = null);
        bool ValidateBarcode(string barcode, BarcodeFormat format);
        int CalculateEan13CheckDigit(string twelveDigits);
        BarcodePrintLabel CreatePrintLabel(Product product, string? barcode = null, string? unit = null);
    }

    public class BarcodeService : IBarcodeService
    {
        private static BarcodeService? _instance;
        public static BarcodeService Instance => _instance ??= new BarcodeService();

        private static readonly Random _random = new();

        public string GenerateInternalBarcode(BarcodeFormat format = BarcodeFormat.Ean13, string? prefix = null)
        {
            switch (format)
            {
                case BarcodeFormat.Ean13:
                    // 890 is standard prefix for GS1 India, followed by 9 random digits and check digit
                    var pfx = !string.IsNullOrWhiteSpace(prefix) && prefix.Length <= 3 ? prefix : "890";
                    var bodyDigits = new char[12];
                    for (int i = 0; i < pfx.Length; i++) bodyDigits[i] = pfx[i];
                    for (int i = pfx.Length; i < 12; i++) bodyDigits[i] = (char)('0' + _random.Next(0, 10));
                    var twelve = new string(bodyDigits);
                    var checkDigit = CalculateEan13CheckDigit(twelve);
                    return twelve + checkDigit;

                case BarcodeFormat.UpcA:
                    var upcEleven = "0" + new string(Enumerable.Range(0, 10).Select(_ => (char)('0' + _random.Next(0, 10))).ToArray());
                    var upcCheck = CalculateEan13CheckDigit("0" + upcEleven);
                    return upcEleven + upcCheck;

                case BarcodeFormat.Code128:
                default:
                    var codePfx = !string.IsNullOrWhiteSpace(prefix) ? prefix.ToUpperInvariant() : "STOMS";
                    return $"{codePfx}-{DateTime.Now:yyMMdd}-{_random.Next(1000, 9999)}";
            }
        }

        public bool ValidateBarcode(string barcode, BarcodeFormat format)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return false;
            var clean = barcode.Trim();

            if (format == BarcodeFormat.Ean13)
            {
                if (clean.Length != 13 || !clean.All(char.IsDigit)) return false;
                var twelve = clean.Substring(0, 12);
                var expectedCheck = CalculateEan13CheckDigit(twelve);
                return (clean[12] - '0') == expectedCheck;
            }

            if (format == BarcodeFormat.UpcA)
            {
                return clean.Length == 12 && clean.All(char.IsDigit);
            }

            // Code 128 / QR
            return clean.Length >= 3 && clean.Length <= 64;
        }

        public int CalculateEan13CheckDigit(string twelveDigits)
        {
            if (twelveDigits.Length < 12) twelveDigits = twelveDigits.PadRight(12, '0');
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = twelveDigits[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }
            int mod = sum % 10;
            return (mod == 0) ? 0 : 10 - mod;
        }

        public BarcodePrintLabel CreatePrintLabel(Product product, string? barcode = null, string? unit = null)
        {
            var code = !string.IsNullOrWhiteSpace(barcode) ? barcode : (product.Barcode ?? product.Sku);
            var selectedUnit = !string.IsNullOrWhiteSpace(unit) ? unit : product.Unit;
            var price = product.SellingRate;
            if (!string.Equals(selectedUnit, product.Unit, StringComparison.OrdinalIgnoreCase))
            {
                var configured = product.FindUnit(selectedUnit);
                if (configured != null && configured.SellingRate > 0)
                {
                    price = configured.SellingRate;
                }
            }

            return new BarcodePrintLabel
            {
                ProductName = product.Name,
                Sku = product.Sku,
                Barcode = code,
                Format = BarcodeFormat.Ean13,
                Price = price,
                Unit = selectedUnit
            };
        }
    }
}

