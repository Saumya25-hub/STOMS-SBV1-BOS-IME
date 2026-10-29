using System;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface IRealtimeUpdateService
    {
        bool IsConnected { get; }
        string ConnectionStateDescription { get; }
        event EventHandler<StockMovement>? StockMovementOccurred;
        event EventHandler<InventoryAlert>? AlertPushed;
        event EventHandler? DashboardRefreshRequested;
        Task ConnectAsync(string hubUrl);
        Task DisconnectAsync();
    }

    public class RealtimeUpdateService : IRealtimeUpdateService
    {
        private static RealtimeUpdateService? _instance;
        public static RealtimeUpdateService Instance => _instance ??= new RealtimeUpdateService();

        private bool _isConnected = true;

        public bool IsConnected => _isConnected;
        public string ConnectionStateDescription => _isConnected 
            ? "SignalR: Active (Telemetry Syncing)" 
            : "SignalR: Offline (Polling Fallback)";

        public event EventHandler<StockMovement>? StockMovementOccurred;
        public event EventHandler<InventoryAlert>? AlertPushed;
        public event EventHandler? DashboardRefreshRequested;

        public Task ConnectAsync(string hubUrl)
        {
            _isConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            _isConnected = false;
            return Task.CompletedTask;
        }

        // Simulates real-time push from server when transaction is recorded on another workstation
        public void SimulateIncomingTransaction(StockMovement movement)
        {
            StockMovementOccurred?.Invoke(this, movement);
            DashboardRefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        public void SimulateIncomingAlert(InventoryAlert alert)
        {
            AlertPushed?.Invoke(this, alert);
            DashboardRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
