using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ActivityMonitor.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            ProgramRecorder.Instance.RecordRunningProgramSnapshot();
            ProcessDataGrid.DataContext = TrackedPrograms.Instance.Index;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ProcessDataGrid.Items.Refresh();

            foreach (var item in ProcessDataGrid.Items)
            {
                Console.WriteLine(item);
            }
        }
    }
}
