using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;

namespace SymbolCollector.Android.UITests
{
    [TestFixture]
    public class Tests
    {
        private AndroidApp _app = default!;

        
        private string GetDirectoryWithSubFolder(string subFolderName, string path)
        {
            var directory = Directory.GetParent(path);
            for (int searchLimit = 6; searchLimit >= 0; searchLimit--)
            {
                var subdirs = directory.GetDirectories();
                if (subdirs.Any(d => d.Name == subFolderName))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
            return string.Empty;
        }

        private string GetApkPath()
        {
            var path = GetDirectoryWithSubFolder("src", AppDomain.CurrentDomain.BaseDirectory);
            path = Path.Combine(path, "src/SymbolCollector.Android/bin/Release/io.sentry.symbol.collector-Signed.apk");
            return path;
        }

        [SetUp]
        public void BeforeEachTest()
        {
            var setup = ConfigureApp.Android;
            var apkPath = GetApkPath();
            if (File.Exists(apkPath))
            {
                setup = setup.ApkFile(apkPath);
                Console.WriteLine($"Using APK: {apkPath}");
            }

            // Quick feedback in debug, not running on a farm:
#if DEBUG
            else
            {
                var msg = $"APK path defined but no file exists at this path: {apkPath}";
                Console.WriteLine(msg);
                Assert.Fail(msg);
            }
#endif

            _app = setup
                .PreferIdeSettings()
                //     .InstalledApp("io.sentry.symbol.collector")
                .StartApp();
        }

        [Test]
        public void CollectSymbols()
        {
            _app.Tap(q => q.Id("btnUpload"));
            var totalWaitTimeSeconds = 40 * 60;
            var retryCounter = 200;
            var iterationTimeout = TimeSpan.FromSeconds(totalWaitTimeSeconds / retryCounter);
            while (true)
            {
                try
                {
                    _app.WaitForElement(query => query.Id("done_text"), timeout: iterationTimeout);
                    _app.Screenshot("💯");
                    break;
                }
                catch (Exception e) when (e.InnerException is TimeoutException)
                {
                    if (--retryCounter == 0)
                    {
                        _app.Screenshot("Timeout");
                        throw;
                    }

                    // Check if it failed
                    var result = _app.Query(p => p.Id("alertTitle"));
                    if (result?.Any() == true)
                    {
                        _app.Screenshot("Error");
                        throw new Exception("Error modal found, app errored.");
                    }
                }
            }
        }
    }
}
