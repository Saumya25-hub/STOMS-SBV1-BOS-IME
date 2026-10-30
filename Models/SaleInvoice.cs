using System;
using System.Collections.Generic;
using System.Linq;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum InvoiceStatus
    {
        Draft,
        Confirmed,
        Cancelled
    }

    public enum PaymentMethod
    {
        Cash,
        UPI,
        Card,
        BankTransfer,
        Credit
    }

    public class StockAllocation
    {
        public string BatchId { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string RackId { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public string ShelfId { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public decimal AllocatedBaseQuantity { get; set; }
        public string BaseUnit { get; set; } = "PCS";
        public DateTime? ExpiryDate { get; set; }

        public string FormattedAllocation =>
            $"Batch: {BatchNumber} [{RackCode} → {ShelfCode}] | {AllocatedBaseQuantity:N0} {BaseUnit}" +
            (ExpiryDate.HasValue ? $" (Exp: {ExpiryDate.Value:dd/MM/yyyy})" : "");
    }

    public class SaleInvoiceItem : ObservableObject
    {
        private string _productId = string.Empty;
        private string _productName = string.Empty;
        private string _sku = string.Empty;
        private string? _barcode;
        private decimal _quantity = 1m;
        private string _selectedUnit = "PCS";
        private decimal _unitPrice = 0m;
        private decimal _conversionFactor = 1m;
        private decimal _discountAmount = 0m;
        private decimal _gstRate = 0m;

        public string ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string Sku
        {
            get => _sku;
            set => SetProperty(ref _sku, value);
        }

        public string? Barcode
        {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, Math.Max(0.001m, value)))
                {
                    OnPropertyChanged(nameof(TotalBaseQuantity));
                    OnPropertyChanged(nameof(LineTotal));
                    OnPropertyChanged(nameof(TaxAmount));
                }
            }
        }

        public string SelectedUnit
        {
            get => _selectedUnit;
            set => SetProperty(ref _selectedUnit, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                    OnPropertyChanged(nameof(TaxAmount));
                }
            }
        }

        public decimal ConversionFactor
        {
            get => _conversionFactor;
            set
            {
                if (SetProperty(ref _conversionFactor, Math.Max(0.0001m, value)))
                {
                    OnPropertyChanged(nameof(TotalBaseQuantity));
                }
            }
        }

        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (SetProperty(ref _discountAmount, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                    OnPropertyChanged(nameof(TaxAmount));
                }
            }
        }

        public decimal GstRate
        {
            get => _gstRate;
            set
            {
                if (SetProperty(ref _gstRate, value))
                {
                    OnPropertyChanged(nameof(TaxAmount));
                }
            }
        }

        public decimal TotalBaseQuantity => Quantity * ConversionFactor;

        public decimal Subtotal => Quantity * UnitPrice;

        public decimal LineTotal => Math.Max(0m, Subtotal - DiscountAmount);

        public decimal TaxAmount => LineTotal * (GstRate / 100m);

        public decimal TotalWithTax => LineTotal + TaxAmount;

        public List<StockAllocation> Allocations { get; set; } = new();

        public string AllocationsSummary => Allocations.Count switch
        {
            0 => "Pending Allocation",
            1 => Allocations[0].FormattedAllocation,
            _ => $"{Allocations.Count} Batches: " + string.Join("; ", Allocations.Select(a => $"{a.BatchNumber}: {a.AllocatedBaseQuantity}"))
        };
    }

    public class SaleInvoice : ObservableObject
    {
        private string _invoiceId = Guid.NewGuid().ToString("N");
        private string _invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        private DateTime _invoiceDate = DateTime.Now;
        private string _customerName = "Walk-in Retail Customer";
        private string _customerPhone = string.Empty;
        private PaymentMethod _paymentMethod = PaymentMethod.Cash;
        private InvoiceStatus _status = InvoiceStatus.Draft;
        private decimal _discountAmount = 0m;
        private string _cashierName = "Saumya Chaudhari";
        private string _notes = string.Empty;

        public string InvoiceId
        {
            get => _invoiceId;
            set => SetProperty(ref _invoiceId, value);
        }

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => SetProperty(ref _invoiceDate, value);
        }

        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set => SetProperty(ref _customerPhone, value);
        }

        public PaymentMethod PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public InvoiceStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (SetProperty(ref _discountAmount, value))
                {
                    OnPropertyChanged(nameof(GrandTotal));
                }
            }
        }

        public string CashierName
        {
            get => _cashierName;
            set => SetProperty(ref _cashierName, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public List<SaleInvoiceItem> Items { get; set; } = new();

        public decimal Subtotal => Items.Sum(i => i.LineTotal);

        public decimal TotalTax => Items.Sum(i => i.TaxAmount);

        public decimal GrandTotal => Math.Max(0m, Subtotal + TotalTax - DiscountAmount);

        public int TotalItemsCount => Items.Count;

        public decimal TotalQuantitySold => Items.Sum(i => i.Quantity);

        public void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(TotalTax));
            OnPropertyChanged(nameof(GrandTotal));
            OnPropertyChanged(nameof(TotalItemsCount));
            OnPropertyChanged(nameof(TotalQuantitySold));
        }
    }
}
