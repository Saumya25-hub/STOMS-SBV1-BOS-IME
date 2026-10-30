using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;
using STOMS_SBV1_BOS_IME.Views;
using Xunit;

namespace STOMS_SBV1_BOS_IME.Tests
{
    public class WindowVisualRenderingTests
    {
        [Fact]
        public void Render_SplashWindow_ToPng()
        {
            var t = new Thread(() =>
            {
                var startupSvc = new StartupService();
                var splashVm = new SplashViewModel(startupSvc)
                {
                    ProgressPercentage = 60,
                    StatusText = "Verifying local relational storage & ledger integrity...",
                    StepTitle = "Relational Data Safety"
                };

                var window = new SplashWindow(splashVm);
                var content = (FrameworkElement)window.Content;
                window.MainCard.Opacity = 1.0;
                window.LogoContainer.Opacity = 1.0;
                window.TitleContainer.Opacity = 1.0;
                window.SubtitleContainer.Opacity = 1.0;
                window.TaglineContainer.Opacity = 1.0;
                window.LoadingSection.Opacity = 1.0;
                window.RealProgressFill.Width = 316.0;
                window.BootSegmentTranslate.X = 180.0;

                content.Measure(new Size(680, 470));
                content.Arrange(new Rect(0, 0, 680, 470));
                content.UpdateLayout();
                window.UpdateClipping();

                var rtb = new RenderTargetBitmap(680, 470, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(content);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Previews");
                Directory.CreateDirectory(outDir);
                string previewPath = Path.Combine(outDir, "splash_screen_preview.png");

                using var fs = File.Create(previewPath);
                encoder.Save(fs);
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [Fact]
        public void Render_LoginWindow_ToPng()
        {
            var t = new Thread(() =>
            {
                var authSvc = new AuthenticationService();
                var loginVm = new LoginViewModel(authSvc);
                var window = new MainWindow(loginVm);

                var content = (FrameworkElement)window.Content;
                content.Measure(new Size(1030, 650));
                content.Arrange(new Rect(0, 0, 1030, 650));
                content.UpdateLayout();
                window.UpdateClipping();

                var rtb = new RenderTargetBitmap(1030, 650, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(content);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Previews");
                Directory.CreateDirectory(outDir);
                string previewPath = Path.Combine(outDir, "login_page_preview.png");

                using var fs = File.Create(previewPath);
                encoder.Save(fs);
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }
    }
}
