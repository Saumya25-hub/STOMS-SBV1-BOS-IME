using System.IO;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;
using Xunit;

namespace STOMS_SBV1_BOS_IME.Tests
{
    public class ImsStartupAndAuthenticationTests
    {
        [Fact]
        public void Test_LogoAsset_ExistsAndHasValidProperties()
        {
            // Verify the logo asset is located in project Assets folder
            string baseDir = Directory.GetCurrentDirectory();
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            string logoPath = Path.Combine(projectRoot, "Assets", "ims_logo.png");

            Assert.True(File.Exists(logoPath), $"Logo asset must exist at {logoPath}");

            var fileInfo = new FileInfo(logoPath);
            Assert.True(fileInfo.Length > 1000, "Logo image file must not be empty");
        }

        [Fact]
        public async Task Test_StartupService_ReportsGenuineMilestones()
        {
            var startupService = new StartupService();
            var milestones = new List<StartupMilestone>();

            var progress = new Progress<StartupMilestone>(m => milestones.Add(m));
            bool success = await startupService.RunStartupInitializationAsync(progress);

            Assert.True(success, "Startup initialization must succeed");
            Assert.NotEmpty(milestones);

            // Verify milestones cover key engineering stages
            Assert.Contains(milestones, m => m.StepTitle.Contains("Environment"));
            Assert.Contains(milestones, m => m.StepTitle.Contains("Configuration"));
            Assert.Contains(milestones, m => m.StepTitle.Contains("Relational Data Safety"));
            Assert.Contains(milestones, m => m.StepTitle.Contains("Stock Engine"));
            Assert.Contains(milestones, m => m.StepTitle.Contains("Security") && m.ProgressPercentage >= 100);

            // Verify physical files created
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDir = Path.Combine(localAppData, "STOMS_BOS_IME");
            Assert.True(Directory.Exists(Path.Combine(appDir, "Data")));
            Assert.True(File.Exists(Path.Combine(appDir, "appsettings.json")));
        }

        [Fact]
        public async Task Test_StartupService_FailureHandling_ReturnsFalseAndSetsError()
        {
            var startupService = new StartupService();
            startupService.SetSimulatedFailure(true);

            var milestones = new List<StartupMilestone>();
            var progress = new Progress<StartupMilestone>(m => milestones.Add(m));

            bool success = await startupService.RunStartupInitializationAsync(progress);

            Assert.False(success, "Startup with failure hook must return false");
            Assert.NotNull(startupService.LastErrorMessage);
            Assert.Contains("verification failed", startupService.LastErrorMessage);
            Assert.Contains(milestones, m => m.IsFailed);
        }

        [Fact]
        public async Task Test_SplashViewModel_FailureAndRetryRecovery()
        {
            var startupService = new StartupService();
            startupService.SetSimulatedFailure(true);

            var splashVm = new SplashViewModel(startupService);

            // Run initial failed attempt
            await splashVm.StartInitializationAsync();

            Assert.True(splashVm.HasFailed);
            Assert.NotNull(splashVm.ErrorMessage);

            // Trigger retry via RetryCommand (which resets failure simulation)
            splashVm.RetryCommand.Execute(null);

            // Wait briefly for async initialization to complete
            int attempts = 0;
            while (splashVm.IsInitializing && attempts++ < 30)
            {
                await Task.Delay(100);
            }

            Assert.False(splashVm.HasFailed, "After retry, HasFailed must be false");
            Assert.Equal(100, splashVm.ProgressPercentage);
        }

        [Fact]
        public async Task Test_AuthenticationService_ValidAndInvalidCredentials()
        {
            var authService = new AuthenticationService();

            // Test 1: Empty credentials fail
            var (emptySuccess, _, _) = await authService.AuthenticateAsync("", "", "WH-01");
            Assert.False(emptySuccess);

            // Test 2: Invalid password fails
            var (invalidSuccess, _, _) = await authService.AuthenticateAsync("admin", "wrong_password", "WH-01");
            Assert.False(invalidSuccess);

            // Test 3: Valid admin succeeds with Saumya Chaudhari profile
            var (adminSuccess, _, adminSession) = await authService.AuthenticateAsync("admin", "admin", "WH-01: Central Regional Hub");
            Assert.True(adminSuccess);
            Assert.NotNull(adminSession);
            Assert.Contains("Saumya Chaudhari", adminSession.FullName);
            Assert.True(adminSession.IsAuthenticated);

            // Test 4: Valid engineer Jenil succeeds
            var (jenilSuccess, _, jenilSession) = await authService.AuthenticateAsync("jenil", "jenil123", "WH-02");
            Assert.True(jenilSuccess);
            Assert.NotNull(jenilSession);
            Assert.Contains("Jenil", jenilSession.FullName);

            // Test 5: Valid engineer Hardik succeeds
            var (hardikSuccess, _, hardikSession) = await authService.AuthenticateAsync("hardik", "hardik123", "WH-03");
            Assert.True(hardikSuccess);
            Assert.NotNull(hardikSession);
            Assert.Contains("Hardik", hardikSession.FullName);
        }

        [Fact]
        public async Task Test_LoginViewModel_Execution()
        {
            var authService = new AuthenticationService();
            var loginVm = new LoginViewModel(authService)
            {
                Username = "admin",
                Password = "admin"
            };

            bool eventFired = false;
            loginVm.LoginSucceeded += (s, e) => eventFired = true;

            loginVm.LoginCommand.Execute(null);

            // Wait for completion
            int attempts = 0;
            while (loginVm.IsBusy && attempts++ < 20)
            {
                await Task.Delay(50);
            }

            Assert.True(eventFired, "LoginSucceeded event must fire");
            Assert.True(loginVm.IsSuccess);
            Assert.NotNull(loginVm.Session);
        }
    }
}
