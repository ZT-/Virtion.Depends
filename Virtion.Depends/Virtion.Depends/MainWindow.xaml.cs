#define X64

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Path = System.IO.Path;


namespace Virtion.Depends
{
    public partial class MainWindow : MetroWindow
    {
#if _X64
        private const string DllPath = "x64\\PEDetours.dll";
#else
        private const string DllPath = "x86\\PEDetours.dll";
#endif

        public delegate void SymbolCallbackDelegate([MarshalAs(UnmanagedType.LPStr)] StringBuilder name);

        [DllImport(DllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetImports(SymbolCallbackDelegate bywayCallBack, SymbolCallbackDelegate symbolCallBack);

        [DllImport(DllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetExports(SymbolCallbackDelegate bywayCallBack);

        [DllImport(DllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool OpenEPFile([MarshalAs(UnmanagedType.LPStr)] string path);

        private TreeViewItem exportsItem;
        private TreeViewItem importsItem;
        private TreeViewItem currentImportItem;
        private string currentPEName;

        public MainWindow()
        {
            InitializeComponent();

#if _X64
            this.Title += "X64";
#else
            this.Title += "X86";
#endif

        }

        private void BuildTreeHeader()
        {
            this.TV_Tree.Items.Clear();
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

        private void ParserStart(string name)
        {
            this.BuildTreeHeader();
            if (OpenEPFile(name) == true)
            {
                GetImports(BywayCallback, SymbolCallback);
                GetExports(ExportsCallback);

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

        private void ExportsCallback(StringBuilder name)
        {
            this.exportsItem.IsExpanded = true;

            {
                var item = this.exportsItem.Items[0] as TreeViewItem;
                var list = item.DataContext as List<string>;
                list.Add(name.ToString());
            }

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

        private void TreeItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.LB_List.Items.Clear();
            var item = this.TV_Tree.SelectedItem as TreeViewItem;
            var list = item.DataContext as List<string>;
            foreach (string s in list)
            {
                this.LB_List.Items.Add(new ListBoxItem()
                {
                    Content = s
                });
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


    }
}
