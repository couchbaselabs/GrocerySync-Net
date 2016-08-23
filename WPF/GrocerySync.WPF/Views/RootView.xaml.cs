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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MvvmCross.Wpf.Views;

namespace GrocerySync
{
    /// <summary>
    /// Interaction logic for RootPage.xaml
    /// </summary>
    public partial class RootView : MvxWpfView
    {
        public RootView()
        {
            InitializeComponent();

            DataContextChanged += (sender, args) =>
            {
                var newModel = args.NewValue as RootViewModel;
                newModel?.RefreshSync();
            };
        }

        private void AnalyzeKey(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return || e.Key == Key.Enter) {
                e.Handled = true;
                ((RootViewModel)ViewModel).AddNewItem();
            }
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((ListView)sender).SelectedIndex = -1;
        }
    }
}
