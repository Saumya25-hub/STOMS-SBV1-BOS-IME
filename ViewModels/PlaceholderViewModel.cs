using System.Collections.Generic;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Common;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.ViewModels
{
    public class PlaceholderViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public string ModuleName { get; }
        public string ModuleCode { get; }
        public string PlannedRelease { get; }
        public string Description { get; }
        public List<string> UpcomingFeatures { get; }

        public ICommand ReturnToDashboardCommand { get; }

        public PlaceholderViewModel(
            string moduleName,
            string moduleCode,
            string plannedRelease,
            string description,
            List<string> upcomingFeatures,
            INavigationService? navigationService = null)
        {
            _navigationService = navigationService ?? NavigationService.Instance;
            ModuleName = moduleName;
            ModuleCode = moduleCode;
            PlannedRelease = plannedRelease;
            Description = description;
            UpcomingFeatures = upcomingFeatures;

            ReturnToDashboardCommand = new RelayCommand(() => _navigationService.NavigateTo("Dashboard"));
        }
    }
}

