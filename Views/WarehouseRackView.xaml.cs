using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.ViewModels;

namespace STOMS_SBV1_BOS_IME.Views
{
    public partial class WarehouseRackView : UserControl
    {
        private bool _isDragging;
        private Point _dragStartPoint;
        private double _initialCanvasX;
        private double _initialCanvasY;
        private FrameworkElement? _draggedElement;
        private Rack? _draggedRack;

        public WarehouseRackView()
        {
            InitializeComponent();
        }

        private void OnRackPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WarehouseRackViewModel vm || !vm.IsEditLayoutMode)
                return;

            if (sender is FrameworkElement element && element.DataContext is Rack rack)
            {
                _isDragging = true;
                _draggedElement = element;
                _draggedRack = rack;
                _dragStartPoint = e.GetPosition(this);
                _initialCanvasX = rack.CanvasX;
                _initialCanvasY = rack.CanvasY;
                element.CaptureMouse();
                vm.SelectedRack = rack;
                e.Handled = true;
            }
        }

        private void OnRackPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedRack == null || _draggedElement == null)
                return;

            if (DataContext is not WarehouseRackViewModel vm || !vm.IsEditLayoutMode)
            {
                _isDragging = false;
                _draggedElement?.ReleaseMouseCapture();
                return;
            }

            var currentPoint = e.GetPosition(this);
            double dx = (currentPoint.X - _dragStartPoint.X) / vm.ZoomLevel;
            double dy = (currentPoint.Y - _dragStartPoint.Y) / vm.ZoomLevel;

            double targetX = _initialCanvasX + dx;
            double targetY = _initialCanvasY + dy;

            if (vm.IsSnapToGrid)
            {
                targetX = Math.Round(targetX / 20.0) * 20.0;
                targetY = Math.Round(targetY / 20.0) * 20.0;
            }

            _draggedRack.CanvasX = Math.Max(0, targetX);
            _draggedRack.CanvasY = Math.Max(0, targetY);
            vm.HasUnsavedLayoutChanges = true;
        }

        private void OnRackPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedElement != null)
            {
                _isDragging = false;
                _draggedElement.ReleaseMouseCapture();
                _draggedElement = null;
                _draggedRack = null;
                e.Handled = true;
            }
        }
    }
}
