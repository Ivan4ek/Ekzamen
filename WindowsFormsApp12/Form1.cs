using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace DataGridViewExample
{
    public partial class Form1 : Form
    {
        public Form1(string filePath, string tableName, string[] excludedColumns, bool addActionButtons, bool activeFilter, string[] widthColumns)
        {
            InitializeComponent();
            new UniversalDataGridView(filePath, tableName, dataGridView1, excludedColumns, addActionButtons, activeFilter, widthColumns);
        }
    }
}
