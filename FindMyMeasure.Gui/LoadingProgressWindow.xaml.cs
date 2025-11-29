using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FindMyMeasure.Gui
{
    /// <summary>
    /// Logique d'interaction pour LoadingProgressWindow.xaml
    /// </summary>
    public partial class LoadingProgressWindow : Window
    {
        public LoadingProgressWindow()
        {
            InitializeComponent();
            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());

        }

        public void SetProgress(double progress)
        {
            pbMain.Value = progress;
        }

        public void MarkAsCompleted()
        {
            lMain.Content = "Analysis Completed";
            pbMain.Value = pbMain.Maximum;
            btnOk.IsEnabled = true;
        }

        public void AddLog(string log)
        {
            if(string.IsNullOrEmpty(log))
                return;
            this.lbLog.Items.Add(log);
            if(lbLog.Items.Count > 0)
            {
                lbLog.ScrollIntoView(lbLog.Items[lbLog.Items.Count - 1]);
            }
        }

        public void SetIndeterminate(bool indeterminate)
        {
            pbMain.IsIndeterminate = indeterminate;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.Owner != null)
                    this.Owner.IsEnabled = true;
            }
            catch {  }

            this.Close();
        }
    }
}
