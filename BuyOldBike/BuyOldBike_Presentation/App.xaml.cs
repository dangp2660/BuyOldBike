using BuyOldBike_BLL.Features.Payments;
using System;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BuyOldBike_Presentation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private CancellationTokenSource? _autoRefundCts;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _autoRefundCts = new CancellationTokenSource();
            _ = Task.Run(() => RunAutoRefundJob(_autoRefundCts.Token));
        }

        private async Task RunAutoRefundJob(CancellationToken token)
        {
            var depositService = new DepositService();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    depositService.AutoRefundExpiredDeposits();
                }
                catch
                {
                    // Ignore global errors
                }

                try
                {
                    // Check every 5 minutes
                    await Task.Delay(TimeSpan.FromMinutes(5), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _autoRefundCts?.Cancel();
            _autoRefundCts?.Dispose();
            base.OnExit(e);
        }
    }
}
