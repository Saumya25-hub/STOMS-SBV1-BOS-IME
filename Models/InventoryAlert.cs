using System;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    public enum AlertCategory
    {
        LowStock,
        NearExpiry,
        ExpiredStock,
        PendingApproval,
        SystemSync
    }

    public class InventoryAlert
    {
        public string Id { get; set; } = string.Empty;
        public AlertCategory Category { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TargetSku { get; set; }
        public string? Location { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsAcknowledged { get; set; }
        public string TargetModule { get; set; } = "Stock Management";

        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                return $"{(int)span.TotalDays}d ago";
            }
        }
    }
}

