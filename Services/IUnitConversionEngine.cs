using System;
using System.Collections.Generic;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface IUnitConversionEngine
    {
        decimal ConvertToBaseUnits(Product product, decimal quantity, string unitName);
        decimal ConvertFromBaseUnits(Product product, decimal baseQuantity, string targetUnitName);
        PackagingBreakdown CalculateBreakdown(Product product, decimal baseQuantity);
        decimal GetDefaultConversionFactor(string baseUnit, string targetUnit);
    }

    public class UnitConversionEngine : IUnitConversionEngine
    {
        private static UnitConversionEngine? _instance;
        public static UnitConversionEngine Instance => _instance ??= new UnitConversionEngine();

        public decimal ConvertToBaseUnits(Product product, decimal quantity, string unitName)
        {
            if (string.Equals(product.Unit, unitName, StringComparison.OrdinalIgnoreCase))
            {
                return quantity;
            }

            var configuredUnit = product.FindUnit(unitName);
            if (configuredUnit != null && configuredUnit.ConversionFactor > 0)
            {
                return quantity * configuredUnit.ConversionFactor;
            }

            // Standard fallback based on metric names
            var defaultFactor = GetDefaultConversionFactor(product.Unit, unitName);
            return quantity * defaultFactor;
        }

        public decimal ConvertFromBaseUnits(Product product, decimal baseQuantity, string targetUnitName)
        {
            if (string.Equals(product.Unit, targetUnitName, StringComparison.OrdinalIgnoreCase))
            {
                return baseQuantity;
            }

            var configuredUnit = product.FindUnit(targetUnitName);
            if (configuredUnit != null && configuredUnit.ConversionFactor > 0)
            {
                return baseQuantity / configuredUnit.ConversionFactor;
            }

            var defaultFactor = GetDefaultConversionFactor(product.Unit, targetUnitName);
            return defaultFactor > 0 ? baseQuantity / defaultFactor : baseQuantity;
        }

        public PackagingBreakdown CalculateBreakdown(Product product, decimal baseQuantity)
        {
            var packagingUnit = !string.IsNullOrWhiteSpace(product.PackagingUnit) ? product.PackagingUnit : product.Unit;
            var factor = product.ConversionFactor > 0 ? product.ConversionFactor : 1m;

            return new PackagingBreakdown
            {
                TotalBaseQuantity = baseQuantity,
                BaseUnit = product.Unit,
                PackagingUnit = packagingUnit,
                ConversionFactor = factor
            };
        }

        public decimal GetDefaultConversionFactor(string baseUnit, string targetUnit)
        {
            var b = baseUnit?.ToUpperInvariant().Trim() ?? "";
            var t = targetUnit?.ToUpperInvariant().Trim() ?? "";

            if (b == t) return 1m;

            // Weight
            if (b == "GM" && t == "KG") return 1000m;
            if (b == "KG" && t == "GM") return 0.001m;
            if (b == "KG" && t == "TON") return 1000m;

            // Volume
            if (b == "ML" && t == "LTR") return 1000m;
            if (b == "LTR" && t == "ML") return 0.001m;

            // Count / Packaging
            if (b == "PCS" && t == "DOZEN") return 12m;
            if (b == "PCS" && t == "BOX") return 24m;
            if (b == "BOX" && t == "CARTON") return 10m;
            if (b == "PCS" && t == "CARTON") return 240m;

            return 1m;
        }
    }
}
