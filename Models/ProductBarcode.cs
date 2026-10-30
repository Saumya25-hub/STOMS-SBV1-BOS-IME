using System;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum BarcodeFormat
    {
        Code128,
        Ean13,
        UpcA,
        QrCode
    }

    public class ProductBarcode : ObservableObject
    {
        private string _code = string.Empty;
        private BarcodeFormat _format = BarcodeFormat.Code128;
        private string _unit = "PCS";
        private bool _isPrimary = true;
        private DateTime _assignedAt = DateTime.Now;

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value?.Trim() ?? string.Empty);
        }

        public BarcodeFormat Format
        {
            get => _format;
            set => SetProperty(ref _format, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public bool IsPrimary
        {
            get => _isPrimary;
            set => SetProperty(ref _isPrimary, value);
        }

        public DateTime AssignedAt
        {
            get => _assignedAt;
            set => SetProperty(ref _assignedAt, value);
        }

        public string FormatName => Format.ToString();
    }
}
