using System;
using System.Windows.Controls;

namespace Virtion.Depends
{
    public partial class SymbolItem : UserControl
    {
        public string Module
        {
            get { return this.L_Module.Content.ToString(); }
            set { this.L_Module.Content = value; }
        }
        public string Symbol
        {
            get { return this.L_Symbol.Content.ToString(); }
            set
            {
                this.L_Symbol.Content = value;
            }
        }

        public SymbolItem()
        {
            InitializeComponent();
        }

        public bool Match(string s)
        {
            if (this.Symbol.IndexOf(s, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
            return true;
        }

    }
}
