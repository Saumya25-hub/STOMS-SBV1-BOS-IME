using System;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum MovementDateRange
    {
        AllTime,
        Today,
        ThisWeek,
        ThisMonth
    }

    public class DashboardFilterCriteria : ObservableObject
    {
        private string? _selectedWarehouseId;
        private string? _selectedRackId;
        private string? _searchQuery;
        private StockStatus? _statusFilter;
        private MovementDateRange _dateRange = MovementDateRange.AllTime;

        public string? SelectedWarehouseId
        {
            get => _selectedWarehouseId;
            set => SetProperty(ref _selectedWarehouseId, value);
        }

        public string? SelectedRackId
        {
            get => _selectedRackId;
            set => SetProperty(ref _selectedRackId, value);
        }

        public string? SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public StockStatus? StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        public MovementDateRange DateRange
        {
            get => _dateRange;
            set => SetProperty(ref _dateRange, value);
        }

        public bool IsFiltered =>
            !string.IsNullOrWhiteSpace(SelectedWarehouseId) ||
            !string.IsNullOrWhiteSpace(SelectedRackId) ||
            !string.IsNullOrWhiteSpace(SearchQuery) ||
            StatusFilter.HasValue ||
            DateRange != MovementDateRange.AllTime;

        public void Reset()
        {
            SelectedWarehouseId = null;
            SelectedRackId = null;
            SearchQuery = null;
            StatusFilter = null;
            DateRange = MovementDateRange.AllTime;
        }
    }
}

