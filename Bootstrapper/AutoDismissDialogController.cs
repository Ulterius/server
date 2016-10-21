using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;


namespace Bootstrapper
{
    public class AutoDismissDialogController
    {
        public async Task Show(string message, Window owner)
        {
            if (!owner.IsLoaded)
            {
                await owner.Dispatcher.InvokeAsync(async () => await ShowInternal(message), DispatcherPriority.Background);
            }
            else
                await ShowInternal(message);
        }

        private static async Task ShowInternal(string message)
        {
            var view = new AutoDismissDialog();
            var dialogContent = new DialogContent
            {
                Content = view
            };

            AutoDismissDialogViewModel autoDismissDialogViewModel = null;
            var closed = false;
            await DialogHost.Show(dialogContent, (DialogOpenedEventHandler)((sender, args) =>
            {
                autoDismissDialogViewModel = new AutoDismissDialogViewModel(10 * 1000, message, () =>
                {
                    dialogContent.Dispatcher.InvokeAsync(() =>
                    {
                        if (!closed) args.Session.Close();
                    });
                });
                view.DataContext = autoDismissDialogViewModel;
            }));
            closed = true;

            autoDismissDialogViewModel?.Dispose();
        }
    }
}