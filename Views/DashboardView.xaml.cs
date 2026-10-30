using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.ViewModels;

namespace STOMS_SBV1_BOS_IME.Views
{
    public partial class DashboardView : UserControl
    {
        private bool _isDragging;
        private Point _dragStartPoint;
        private double _rackStartCanvasX;
        private double _rackStartCanvasY;
        private FrameworkElement? _draggedElement;
        private Rack? _draggedRack;

        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

        public DashboardView()
        {
            InitializeComponent();
        }

        private void RackCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.IsEditingLayout == true && sender is FrameworkElement element && element.DataContext is Rack rack)
            {
                _isDragging = true;
                _draggedElement = element;
                _draggedRack = rack;
                _dragStartPoint = e.GetPosition(this);
                _rackStartCanvasX = rack.CanvasX;
                _rackStartCanvasY = rack.CanvasY;
                _draggedElement.CaptureMouse();
                e.Handled = true;
            }
        }

        private void RackCard_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedRack != null && _draggedElement != null && ViewModel != null)
            {
                var currentPoint = e.GetPosition(this);
                var zoom = Math.Max(0.5, ViewModel.ZoomLevel);
                var deltaX = (currentPoint.X - _dragStartPoint.X) / zoom;
                var deltaY = (currentPoint.Y - _dragStartPoint.Y) / zoom;

                var newX = _rackStartCanvasX + deltaX;
                var newY = _rackStartCanvasY + deltaY;

                ViewModel.UpdateRackPosition(_draggedRack, newX, newY);
                e.Handled = true;
            }
        }

        private void RackCard_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedElement?.ReleaseMouseCapture();
                _draggedElement = null;
                _draggedRack = null;
                e.Handled = true;
            }
        }
    }
}
