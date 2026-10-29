using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;

namespace STOMS_SBV1_BOS_IME.Tests
{
    [TestClass]
    public class StartupAndAuthTests
    {
        [TestMethod]
        public void Test_LogoAsset_ExistsAndHasValidProperties()
        {
            string baseDir = AppContext.BaseDirectory;
            // Traverse up to find workspace Assets folder
            string? projectRoot = baseDir;
            for (int i = 0; i < 5 && projectRoot != null && !Directory.Exists(Path.Combine(projectRoot, "Assets")); i++)
            {
                projectRoot = Directory.GetParent(projectRoot)?.FullName;
            }

            Assert.IsNotNull(projectRoot, "Project root must be discoverable");
            string logoPath = Path.Combine(projectRoot, "Assets", "ims_logo.png");

            Assert.IsTrue(File.Exists(logoPath), $"Logo asset must exist at {logoPath}");

            var fileInfo = new FileInfo(logoPath);
            Assert.IsGreaterThan(1000, fileInfo.Length, "Logo image file must not be empty");
        }

        [TestMethod]
        public async Task Test_StartupService_ReportsGenuineMilestones()
        {
            var startupService = new StartupService();
            var milestones = new List<StartupMilestone>();

            var progress = new Progress<StartupMilestone>(m => milestones.Add(m));
            bool success = await startupService.RunStartupInitializationAsync(progress);

            Assert.IsTrue(success, "Startup initialization must succeed");
            Assert.IsNotEmpty(milestones, "Milestones must not be empty");

            // Verify milestones cover key engineering stages
            Assert.IsTrue(milestones.Any(m => m.StepTitle.Contains("Environment")));
            Assert.IsTrue(milestones.Any(m => m.StepTitle.Contains("Configuration")));
            Assert.IsTrue(milestones.Any(m => m.StepTitle.Contains("Relational Data Safety")));
            Assert.IsTrue(milestones.Any(m => m.StepTitle.Contains("Stock Engine")));
            Assert.IsTrue(milestones.Any(m => m.StepTitle.Contains("Security") && m.ProgressPercentage >= 100));

            // Verify physical files created
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDir = Path.Combine(localAppData, "STOMS_BOS_IME");
            Assert.IsTrue(Directory.Exists(Path.Combine(appDir, "Data")));
            Assert.IsTrue(File.Exists(Path.Combine(appDir, "appsettings.json")));
        }

        [TestMethod]
        public async Task Test_StartupService_FailureHandling_ReturnsFalseAndSetsError()
        {
            var startupService = new StartupService();
            startupService.SetSimulatedFailure(true);

            var milestones = new List<StartupMilestone>();
            var progress = new Progress<StartupMilestone>(m => milestones.Add(m));

            bool success = await startupService.RunStartupInitializationAsync(progress);

            Assert.IsFalse(success, "Startup with failure hook must return false");
            Assert.IsNotNull(startupService.LastErrorMessage);
            StringAssert.Contains(startupService.LastErrorMessage, "verification failed");
            Assert.IsTrue(milestones.Any(m => m.IsFailed));
        }

        [TestMethod]
        public async Task Test_SplashViewModel_FailureAndRetryRecovery()
        {
            var startupService = new StartupService();
            startupService.SetSimulatedFailure(true);

            var splashVm = new SplashViewModel(startupService);

            // Run initial failed attempt
            await splashVm.StartInitializationAsync();

            Assert.IsTrue(splashVm.HasFailed);
            Assert.IsNotNull(splashVm.ErrorMessage);

            // Trigger retry via RetryCommand (which resets failure simulation)
            splashVm.RetryCommand.Execute(null);

            // Wait briefly for async initialization to complete
            int attempts = 0;
            while (splashVm.IsInitializing && attempts++ < 30)
            {
                await Task.Delay(100);
            }

            Assert.IsFalse(splashVm.HasFailed, "After retry, HasFailed must be false");
            Assert.AreEqual(100.0, splashVm.ProgressPercentage);
        }

        [TestMethod]
        public async Task Test_AuthenticationService_ValidAndInvalidCredentials()
        {
            var authService = new AuthenticationService();

            // Test 1: Empty credentials fail
            var (emptySuccess, _, _) = await authService.AuthenticateAsync("", "", "WH-01");
            Assert.IsFalse(emptySuccess);

            // Test 2: Invalid password fails
            var (invalidSuccess, _, _) = await authService.AuthenticateAsync("admin", "wrong_password", "WH-01");
            Assert.IsFalse(invalidSuccess);

            // Test 3: Valid admin succeeds with Saumya Chaudhari profile
            var (adminSuccess, _, adminSession) = await authService.AuthenticateAsync("admin", "admin", "WH-01: Central Regional Hub");
            Assert.IsTrue(adminSuccess);
            Assert.IsNotNull(adminSession);
            StringAssert.Contains(adminSession.FullName, "Saumya Chaudhari");
            Assert.IsTrue(adminSession.IsAuthenticated);

            // Test 4: Valid engineer Jenil succeeds
            var (jenilSuccess, _, jenilSession) = await authService.AuthenticateAsync("jenil", "jenil123", "WH-02");
            Assert.IsTrue(jenilSuccess);
            Assert.IsNotNull(jenilSession);
            StringAssert.Contains(jenilSession.FullName, "Jenil");

            // Test 5: Valid engineer Hardik succeeds
            var (hardikSuccess, _, hardikSession) = await authService.AuthenticateAsync("hardik", "hardik123", "WH-03");
            Assert.IsTrue(hardikSuccess);
            Assert.IsNotNull(hardikSession);
            StringAssert.Contains(hardikSession.FullName, "Hardik");
        }

        [TestMethod]
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

            Assert.IsTrue(eventFired, "LoginSucceeded event must fire");
            Assert.IsTrue(loginVm.IsSuccess);
            Assert.IsNotNull(loginVm.Session);
        }
    }
}

