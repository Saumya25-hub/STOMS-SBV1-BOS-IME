using System;
using System.Collections.Generic;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    /// <summary>
    /// Represents packaging or measurement units and their conversion ratios to the product's base unit.
    /// E.g. Base Unit = PCS; Packaging Unit = BOX with ConversionFactor = 24 (1 BOX = 24 PCS);
    /// CARTON with ConversionFactor = 240 (1 CARTON = 10 BOX = 240 PCS).
    /// </summary>
    public class ProductUnit : ObservableObject
    {
        private string _unitName = "PCS";
        private decimal _conversionFactor = 1m; // In terms of Base Unit
        private decimal _purchaseRate;
        private decimal _sellingRate;
        private decimal _mrp;
        private bool _isBaseUnit = true;

        public string UnitName
        {
            get => _unitName;
            set => SetProperty(ref _unitName, value?.ToUpperInvariant().Trim() ?? "PCS");
        }

        public decimal ConversionFactor
        {
            get => _conversionFactor;
            set => SetProperty(ref _conversionFactor, Math.Max(0.0001m, value));
        }

        public decimal PurchaseRate
        {
            get => _purchaseRate;
            set => SetProperty(ref _purchaseRate, value);
        }

        public decimal SellingRate
        {
            get => _sellingRate;
            set => SetProperty(ref _sellingRate, value);
        }

        public decimal Mrp
        {
            get => _mrp;
            set => SetProperty(ref _mrp, value);
        }

        public bool IsBaseUnit
        {
            get => _isBaseUnit;
            set => SetProperty(ref _isBaseUnit, value);
        }

        public string DisplayRatio => IsBaseUnit
            ? $"1 {UnitName} (Base Unit)"
            : $"1 {UnitName} = {ConversionFactor:G29} Base Units";
    }

    /// <summary>
    /// Breakdown of loose pieces vs full packaged boxes.
    /// E.g. 1910 PCS with 1 BOX = 24 PCS results in 79 complete BOX and 14 loose PCS.
    /// </summary>
    public class PackagingBreakdown
    {
        public decimal TotalBaseQuantity { get; set; }
        public string BaseUnit { get; set; } = "PCS";
        public string PackagingUnit { get; set; } = "BOX";
        public decimal ConversionFactor { get; set; } = 24m;

        public int FullPackages => ConversionFactor > 0 ? (int)(TotalBaseQuantity / ConversionFactor) : 0;
        public decimal LooseQuantity => ConversionFactor > 0 ? TotalBaseQuantity % ConversionFactor : TotalBaseQuantity;

        public string FormattedDescription
        {
            get
            {
                if (ConversionFactor <= 1)
                {
                    return $"{TotalBaseQuantity:N0} {BaseUnit}";
                }

                if (LooseQuantity == 0)
                {
                    return $"{FullPackages:N0} {PackagingUnit} ({TotalBaseQuantity:N0} {BaseUnit})";
                }

                return $"{FullPackages:N0} {PackagingUnit} + {LooseQuantity:G29} {BaseUnit} ({TotalBaseQuantity:N0} {BaseUnit})";
            }
        }
    }
}

