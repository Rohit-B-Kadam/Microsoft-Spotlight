using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace Spotlight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel vm;
        public MainWindow()
        {
            InitializeComponent();
            vm = new MainViewModel();
            this.Loaded += (s, e) => DataContext = vm;
        }

        private void StartSearch(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (vm.ButtonContent.Equals("Search"))
            {
                //updating status
                vm.SearchStatus = "Searching...";
                //Clear the Previous search list
                vm.ListOfFoundFile.Clear();
                vm.ButtonContent = "Stop";
                vm.SearchTheFile();
            }
            else
            {
                vm.StopSearching();
                vm.ButtonContent = "Search";
                if (vm.ListOfFoundFile.Count != 0)
                    vm.SearchStatus = "no. Item " + vm.ListOfFoundFile.Count;
                else
                    vm.SearchStatus = "Not Found";
            }
        }

        private void txtFilePath_KeyUp(object sender, KeyEventArgs e)
        {
            if (txtFilePath.Text.Length == 0)
                vm.EnableSearchBtn = false;
            else
                vm.EnableSearchBtn = true;
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataItem di = (sender as ListView).SelectedItem as DataItem;
            MessageBoxResult result = MessageBox.Show("Do you want to open this file "+di.FilePath , "Open File",MessageBoxButton.YesNo);

            if(result == MessageBoxResult.No)
            {
                // file will not open
            }
            else
            {
                
                System.Diagnostics.Process.Start("explorer.exe", $@"/select, {di.FilePath}");
            }
        }
    }

    //MVVM Business logic
    public class MainViewModel : INotifyPropertyChanged
    {
        //Important
        private void Notify(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        // File Path
        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                Notify("FilePath");
            }
        }

        
        private string _buttonContent;
        public string ButtonContent
        {
            get { return _buttonContent; }
            set
            {
                _buttonContent = value;
                Notify("ButtonContent");
            }
        }

        private ObservableCollection<CheckBox> _checkBoxList;
        public ObservableCollection<CheckBox> CheckBoxList
        {
            get { return _checkBoxList; }
            set
            {
                _checkBoxList = value;
                Notify("CheckBoxList");
            }
        }

        
        private ObservableCollection<DataItem> _listOfFoundFile;
        public ObservableCollection<DataItem> ListOfFoundFile
        {
            get { return _listOfFoundFile; }
            set
            {
                _listOfFoundFile = value;
                Notify("ListOfFoundFile");
            }
        }

        private string _searchStatus;
        public string SearchStatus
        {
            get { return _searchStatus; }
            set
            {
                _searchStatus = value;
                Notify("SearchStatus");
            }
        }

        private bool _enableSearchBtn;
        public bool EnableSearchBtn
        {
            get { return _enableSearchBtn; }
            set
            {
                _enableSearchBtn = value;
                Notify("EnableSearchBtn");
            }
        }

        
        public Dispatcher _current;
        public Helper hObj;
        public List<Thread> thread;
        public MainViewModel()
        {
            _current = Dispatcher.CurrentDispatcher;
            hObj = new Helper();
            CheckBoxList = new ObservableCollection<CheckBox>();
            ListOfFoundFile = new ObservableCollection<DataItem>();
            LoadComboBox();

            FilePath = "";
            ButtonContent = "Search";
            SearchStatus = "";
            EnableSearchBtn = false;
            thread = new List<Thread>();
        }

        public void LoadComboBox()
        {
            CheckBox cb;
            foreach (var item in hObj.DriveList)
            {
                cb = new CheckBox();
                cb.Content = item;
                cb.IsChecked = true;
                CheckBoxList.Add(cb);
            }
        }

        public void AddToFoundFile(DataItem diTemp)
        {
            _current.BeginInvoke(new Action(() =>
            {
                ListOfFoundFile.Add(diTemp);
                SearchStatus = "Searching... no. Item " + ListOfFoundFile.Count;
            }));
        }

        public void SearchTheFile()
        {
            List<string> selectedCb = new List<string>();

            foreach (var cb in CheckBoxList)
            {
                if ((bool)cb.IsChecked)
                {
                    selectedCb.Add(cb.Content.ToString());
                }
            }
            
            foreach (var cb in selectedCb)
            {

                Thread t1 = new Thread(
                        () =>
                        {
                            SearchFileIn($@"{cb}");
                            foreach (var t in thread)
                            {
                                if (t.Name.Equals(Thread.CurrentThread.Name))
                                {
                                    thread.Remove(t);
                                    break;
                                }
                            }

                            if(thread.Count == 0)
                            {
                                _current.BeginInvoke(new Action(() =>
                                {
                                    ButtonContent = "Search";

                                    if (ListOfFoundFile.Count != 0)
                                        SearchStatus = "no. Item " + ListOfFoundFile.Count;
                                    else
                                        SearchStatus = "Not Found";
                                }));
                            }
                        });

                thread.Add(t1);
                t1.Start();
                t1.Name = cb;
                
            }
        }

        public void SearchFileIn(string FolderName)
        {
            DirectoryInfo di = new DirectoryInfo($@"{FolderName}");
            DataItem diTemp;

            try
            {
                // Determine whether the directory exists.
                if (di.Exists)
                {
                    // Indicate that the directory already exists.

                    foreach (var directory in di.GetDirectories())
                    {
                        if( !di.Name.Equals(@"$Recycle.Bin") && !di.Name.Equals(@"$Recycle.Bin"))
                            SearchFileIn(directory.FullName);
                    }

                    foreach (var file in di.GetFiles())
                    {
                        //Check if file is equal to given file
                        if (file.Name.Contains(FilePath))
                        //if(file.FullName.Contains(FilePath))
                        {
                            diTemp = new DataItem();
                            diTemp.FilePath = file.FullName;

                            AddToFoundFile(diTemp);
                        }
                    }
                }

            }
            catch (UnauthorizedAccessException)
            { }
            catch (ThreadAbortException)
            { }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "The process failed");
            }
            finally { }
        }

        public void StopSearching()
        {
            foreach (var t in thread)
            {
                if (t.IsAlive)
                    t.Abort();
            }
            thread.Clear();
        }
    }

}

