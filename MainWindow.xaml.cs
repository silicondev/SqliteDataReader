using Microsoft.Win32;
using SqliteDataReader.Respository;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

namespace SqliteDataReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _defaultLimit = 100;
        private Dictionary<string, int> _tableViewLimits = new Dictionary<string, int>();
        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                LoadingLbl.Visibility = _isLoading ? Visibility.Visible : Visibility.Hidden;
            }
        }
        public string SelectedTable { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            IsLoading = false;
            TablesLabelGrid.Visibility = Visibility.Hidden;
            RowsLabelGrid.Visibility = Visibility.Hidden;
        }

        private SqliteRepository _repo;

        private void FilePathPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Database files (.db)|*.db"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
                FilePathTxt.Text = dialog.FileName;
        }

        private async void ReadBtn_Click(object sender, RoutedEventArgs e)
        {
            _repo = new SqliteRepository(new FileInfo(FilePathTxt.Text));
            var tables = await _repo.GetTables();

            _tableViewLimits.Clear();
            foreach (string table in tables)
            {
                _tableViewLimits.Add(table, _defaultLimit);
            }

            if (tables.Length > 0)
            {
                TableViewList.ItemsSource = tables;
                TableViewList.SelectedIndex = 0;
                TablesLabelGrid.Visibility = Visibility.Visible;
                TablesLbl.Content = tables.Length;
                RefreshTableView(tables[0]);
            }
            else
                TablesLabelGrid.Visibility = Visibility.Hidden;
        }

        private void TableViewList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var list = (ListView)sender;
            SelectedTable = (string)list.SelectedItem;
            CountTxt.Text = _tableViewLimits[SelectedTable].ToString();
            RefreshTableView();
        }

        private void RefreshTableView() =>
            RefreshTableView(SelectedTable);

        private void RefreshTableView(string tableName) =>
            FillDataGrid(TableViewGrid, $"SELECT * FROM {tableName} LIMIT {CountTxt.Text}", RowsLabelGrid, RowsLbl);

        private void ExecuteQueryBtn_Click(object sender, RoutedEventArgs e) =>
            FillDataGrid(QueryGrid, QueryTxt.Text, QueryRowsLabelGrid, QueryRowsLbl);

        private void FillDataGrid(DataGrid grid, string query, Grid? rowsLabelGrid = null, Label? rowsLabel = null)
        {
            DataTable? dt = null;
            RunAsync(async () =>
            {
                try
                {
                    dt = await _repo.QueryReader(query);
                }
                catch (Exception ex)
                {
                    ex.ShowMessage();
                }
            }, () =>
            {
                if (rowsLabelGrid != null)
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        rowsLabelGrid.Visibility = Visibility.Visible;
                        if (rowsLabel != null)
                            rowsLabel.Content = dt.Rows.Count;
                    }
                    else
                        rowsLabelGrid.Visibility = Visibility.Hidden;
                }
                
                grid.DataContext = dt?.DefaultView;
            });
        }

        private void RunAsync(Func<Task> work, Action ui)
        {
            IsLoading = true;
            Task.Factory.StartNew(async () =>
            {
                await work();
                await Dispatcher.InvokeAsync(() =>
                {
                    ui();
                    IsLoading = false;
                });
            });
        }

        private void RefreshCount()
        {
            if (int.TryParse(CountTxt.Text, out int count))
            {
                if (_tableViewLimits[SelectedTable] != count)
                {
                    _tableViewLimits[SelectedTable] = count;
                    RefreshTableView();
                }
            }
            else
                CountTxt.Text = _tableViewLimits[SelectedTable].ToString();
        }

        private void CountTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == null || CountTxt == null)
                return;

            RefreshCount();
        }

        private void CountTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (new Key[] { Key.Enter, Key.Escape }.Contains(e.Key))
                RefreshCount();
        }
    }
}
