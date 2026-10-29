namespace STOMS_SBV1_BOS_IME.Models
{
    /// <summary>
    /// Represents a real milestone report during application initialization.
    /// </summary>
    public class StartupMilestone
    {
        public double ProgressPercentage { get; set; }
        public string StepTitle { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsFailed { get; set; }
        public string? ErrorMessage { get; set; }

        public StartupMilestone(double percentage, string stepTitle, string statusMessage, bool isCompleted = false, bool isFailed = false, string? errorMessage = null)
        {
            ProgressPercentage = percentage;
            StepTitle = stepTitle;
            StatusMessage = statusMessage;
            IsCompleted = isCompleted;
            IsFailed = isFailed;
            ErrorMessage = errorMessage;
        }
    }
}
