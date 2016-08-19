using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Path = System.IO.Path;

namespace Virtion.Depends
{
    public partial class MainWindow : MetroWindow
    {
        private const string DllPath = "PEDetours.dll";
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public delegate void SymbolCallbackDelegate([MarshalAs(UnmanagedType.LPStr)] StringBuilder name);

        [DllImport(DllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetImports(SymbolCallbackDelegate bywayCallBack, SymbolCallbackDelegate symbolCallBack);

        [DllImport(DllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetExports(SymbolCallbackDelegate symbolCallBack);

        [DllImport(DllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool OpenEPFile([MarshalAs(UnmanagedType.LPStr)] string path);

        private TreeViewItem exportsItem;
        private TreeViewItem importsItem;
        private TreeViewItem currentImportItem;
        private TreeViewItem allSymbelItem;

        private List<SymbolItem> SymbolList;

        private string currentPEName;

        private static SymbolCallbackDelegate exportsCallbackDelegate;

        public MainWindow()
        {
            InitializeComponent();

#if _X64
            this.Title += "X64";
            this.B_Other.Content = "X86 Depends";
#else
            this.Title += "X86";
            this.B_Other.Content = "X64 Depends";
#endif

        }

        private void BuildTreeHeader()
        {
            this.TV_Tree.Items.Clear();
            this.allSymbelItem = new TreeViewItem()
            {
                Header = "All",
                Background = new SolidColorBrush(Colors.Transparent)
            };
            this.allSymbelItem.MouseLeftButtonUp += AllSymbolItem_Click;
            this.TV_Tree.Items.Add(allSymbelItem);


            this.exportsItem = new TreeViewItem()
            {
                Header = "Exports",
                Background = new SolidColorBrush(Colors.Transparent)
            };

            var item = new TreeViewItem()
            {
                Header = this.currentPEName,
                Background = new SolidColorBrush(Colors.Transparent),
                DataContext = new List<string>()
            };
            item.MouseLeftButtonUp += this.TreeItem_MouseLeftButtonUp;
            this.exportsItem.Items.Add(item);

            this.importsItem = new TreeViewItem()
            {
                Header = "Imports",
                Background = new SolidColorBrush(Colors.Transparent)
            };

            this.TV_Tree.Items.Add(this.exportsItem);
            this.TV_Tree.Items.Add(this.importsItem);

        }

        private void GetSymbolList()
        {
            this.SymbolList = new List<SymbolItem>();
            if (exportsItem != null)
            {
                var moduleItem = exportsItem.Items[0] as TreeViewItem;
                var module = moduleItem.Header.ToString();
                var list = moduleItem.DataContext as List<string>;
                if (list != null)
                {
                    foreach (var symbol in list)
                    {
                        var item = new SymbolItem()
                        {
                            Module = module,
                            Symbol = symbol
                        };
                        this.SymbolList.Add(item);
                        this.LB_List.Items.Add(item);
                    }
                }
            }
            if (importsItem != null)
            {
                foreach (var moduleItem in importsItem.Items)
                {
                    var module = (moduleItem as TreeViewItem).Header.ToString();
                    var list = (moduleItem as TreeViewItem).DataContext as List<string>;
                    if (list != null)
                    {
                        foreach (var symbol in list)
                        {
                            var item = new SymbolItem()
                            {
                                Module = module,
                                Symbol = symbol
                            };
                            this.SymbolList.Add(item);
                            this.LB_List.Items.Add(item);
                        }
                    }
                }
            }
        }

        private void ParserStart(string name)
        {
            this.SymbolList = new List<SymbolItem>();
            this.BuildTreeHeader();

            if (OpenEPFile(name) == true)
            {

                GetImports(BywayCallback, SymbolCallback);

                exportsCallbackDelegate = new SymbolCallbackDelegate(ExportsCallback);
                GetExports(exportsCallbackDelegate);

#if _X64
                this.TB_Tip.Text = currentPEName + " is a x64 PE file";
#else
                this.TB_Tip.Text = currentPEName + " is a x86 PE file";
#endif
                this.B_InfoPlane.Background =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007acc"));
            }

            if (this.importsItem.Items.Count == 0)
            {
                this.TV_Tree.Items.Clear();
#if _X64
                this.TB_Tip.Text = "Not a x64 PE file";
#else
                this.TB_Tip.Text = "Not a x86 PE file";
#endif
                this.B_InfoPlane.Background =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#990000"));
            }
            else
            {
                this.GetSymbolList();
                this.allSymbelItem.DataContext = this.SymbolList;

                if (this.IsDotNet(name) == true)
                {
                    this.TB_Tip.Text += " Is .Net Assembly";
                }
            }
        }

        private void ExportsCallback(StringBuilder name)
        {
            this.exportsItem.IsExpanded = true;
            var item = this.exportsItem.Items[0] as TreeViewItem;
            var list = item.DataContext as List<string>;
            list.Add(name.ToString());
        }

        private void SymbolCallback(StringBuilder name)
        {
            var list = this.currentImportItem.DataContext as List<string>;
            list.Add(name.ToString());
        }

        private void BywayCallback(StringBuilder name)
        {
            this.importsItem.IsExpanded = true;
            TreeViewItem item = new TreeViewItem()
            {
                Header = name,
                DataContext = new List<string>(),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            item.MouseLeftButtonUp += TreeItem_MouseLeftButtonUp;
            currentImportItem = item;
            this.importsItem.Items.Add(item);
        }

        private bool IsDotNet(string path)
        {
            try
            {
                AppDomain dom = AppDomain.CreateDomain("some");
                AssemblyName assemblyName = new AssemblyName();
                assemblyName.CodeBase = path;
                Assembly assembly = dom.Load(assemblyName);
                Type[] types = assembly.GetTypes();
                AppDomain.Unload(dom);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private Config ReadConfig()
        {
            if (File.Exists(App.CurrentPath + "Config") == true)
            {
                string s = File.ReadAllText(App.CurrentPath + "Config");
                try
                {
                    return JsonConvert.DeserializeObject<Config>(s);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }

        private void StartProcess(string path)
        {
            ProcessStartInfo info = new ProcessStartInfo(path);
            info.UseShellExecute = true;
            info.WindowStyle = ProcessWindowStyle.Normal;
            Process.Start(info);
        }

        private void B_Open_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.currentPEName = dialog.SafeFileName;
                ParserStart(dialog.FileName);
            }
        }

        private void B_Other_OnClick(object sender, RoutedEventArgs e)
        {
            var config = ReadConfig();
            if (config != null && string.IsNullOrEmpty(config.OtherVersionPath) == false)
            {
            }
            else
            {
                System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
                dialog.InitialDirectory = App.CurrentPath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (config == null)
                    {
                        config = new Config();
                    }
                    config.OtherVersionPath = dialog.FileName;
                    string s = JsonConvert.SerializeObject(config);
                    File.WriteAllText(App.CurrentPath + "Config", s);
                }
                else
                {
                    return;
                }
            }
            this.StartProcess(config.OtherVersionPath);
            Thread.Sleep(500);
            App.Current.Shutdown(0);
        }

        private void TreeItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.LB_List.Items.Clear();
            var item = this.TV_Tree.SelectedItem as TreeViewItem;
            if (item.DataContext is List<string>)
            {
                var list = item.DataContext as List<string>;
                foreach (string s in list)
                {
                    this.LB_List.Items.Add(new ListBoxItem()
                    {
                        Content = s
                    });
                }
            }
        }

        private void AllSymbolItem_Click(object sender, MouseButtonEventArgs e)
        {
            this.LB_List.Items.Clear();
            foreach (var i in this.SymbolList)
            {
                this.LB_List.Items.Add(i);
            }
        }

        private void Window_PreviewDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            var text = e.Data.GetData(DataFormats.FileDrop) as string[];
            this.currentPEName = Path.GetFileName(text[0]);
            ParserStart(text[0]);
        }

        private void TB_Search_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            //if (this.LB_List.Items[0] is string)
            //{
            //    string[] array = new string[this.LB_List.Items.Count];
            //    this.LB_List.Items.CopyTo(array, 0);
            //    this.LB_List.Items.Clear();
            //    foreach (var s in array)
            //    {
            //        if (s.IndexOf(this.TB_Search.Text, StringComparison.OrdinalIgnoreCase) >= 0)
            //        {
            //            this.LB_List.Items.Add(s);
            //        }
            //    }
            //    return;
            //}
            if (this.TB_Search.Text == "")
            {
                return;
            }
            if (this.LB_List.Items[0] is SymbolItem)
            {
                this.LB_List.Items.Clear();
                var array = this.SymbolList;
                foreach (var i in array)
                {
                    if (i.Match(this.TB_Search.Text) == true)
                    {
                        this.LB_List.Items.Add(i);
                    }
                }
            }
        }

    }
}
