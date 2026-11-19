using System;
using System.Collections.Generic;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum LayoutElementType
    {
        Rack,
        UserPosition,
        Shelf,
        StorageArea,
        Label
    }

    public class LayoutItemModel : ObservableObject
    {
        private string _id = Guid.NewGuid().ToString("N");
        private LayoutElementType _elementType = LayoutElementType.Rack;
        private string _code = "RCK-A";
        private string _name = "Rack A";
        private string _description = string.Empty;
        private double _x = 100;
        private double _y = 100;
        private double _width = 160;
        private double _height = 90;
        private string _colorHex = "#2563EB";
        private int _rowsCount = 4;
        private int _shelvesPerRow = 2;
        private bool _isAdvancedNaming = false;
        private bool _hasRows = true;
        private bool _isSelected = false;
        private string _zone = "Central Zone";

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public LayoutElementType ElementType
        {
            get => _elementType;
            set => SetProperty(ref _elementType, value);
        }

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, Math.Max(40, value));
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, Math.Max(30, value));
        }

        public string ColorHex
        {
            get => _colorHex;
            set => SetProperty(ref _colorHex, value);
        }

        public int RowsCount
        {
            get => _rowsCount;
            set => SetProperty(ref _rowsCount, Math.Max(1, value));
        }

        public int ShelvesPerRow
        {
            get => _shelvesPerRow;
            set => SetProperty(ref _shelvesPerRow, Math.Max(1, value));
        }

        public bool IsAdvancedNaming
        {
            get => _isAdvancedNaming;
            set => SetProperty(ref _isAdvancedNaming, value);
        }

        public bool HasRows
        {
            get => _hasRows;
            set => SetProperty(ref _hasRows, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string Zone
        {
            get => _zone;
            set => SetProperty(ref _zone, value);
        }

        public int TotalShelvesCount => HasRows ? RowsCount * ShelvesPerRow : ShelvesPerRow;

        /// <summary>
        /// Generates list of shelf codes based on simple (A1..A8) or advanced (A-R1-S1) naming.
        /// </summary>
        public List<string> GenerateShelfCodes()
        {
            var list = new List<string>();
            var prefix = string.IsNullOrWhiteSpace(Code) ? "R" : Code.Trim();

            if (!HasRows)
            {
                for (int s = 1; s <= ShelvesPerRow; s++)
                {
                    list.Add($"{prefix}{s}");
                }
                return list;
            }

            if (IsAdvancedNaming)
            {
                for (int r = 1; r <= RowsCount; r++)
                {
                    for (int s = 1; s <= ShelvesPerRow; s++)
                    {
                        list.Add($"{prefix}-R{r}-S{s}");
                    }
                }
            }
            else
            {
                int counter = 1;
                for (int r = 1; r <= RowsCount; r++)
                {
                    for (int s = 1; s <= ShelvesPerRow; s++)
                    {
                        list.Add($"{prefix}{counter++}");
                    }
                }
            }

            return list;
        }
    }

    public class WarehouseRackLayout
    {
        public string WarehouseId { get; set; } = "WH-01";
        public string WarehouseName { get; set; } = "Main Warehouse";
        public double CanvasWidth { get; set; } = 1600;
        public double CanvasHeight { get; set; } = 1200;
        public double GridSize { get; set; } = 20;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public List<LayoutItemModel> Items { get; set; } = new();
    }
}

