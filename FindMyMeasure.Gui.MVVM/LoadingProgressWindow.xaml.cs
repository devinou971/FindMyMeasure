using FindMyMeasure.Gui.MVVM.ViewModels;
using System.Windows;

namespace FindMyMeasure.Gui.MVVM
{
    /// <summary>
    /// Logique d'interaction pour LoadingProgressWindow.xaml
    /// </summary>
    public partial class LoadingProgressWindow : Window
    {
        public LoadingProgressWindow(ReportSelectionViewModel viewModel)
        {
            InitializeComponent();
            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());
            this.DataContext = viewModel;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.Owner != null)
                    this.Owner.IsEnabled = true;
            }
            catch { }

            this.Close();
        }
    }
}
