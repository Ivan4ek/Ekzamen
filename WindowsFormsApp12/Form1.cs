using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace DataGridViewExample
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();

            string filePath = ""; // путь до базы данных
            string fileName = "database.db"; // название файла базы данных
            string tableName = "qwe"; // название таблицы в базе данных
            string[] excludedColumns = { "ID" }; // список столбцов, которые необходимо исключить из отображения в DataGridView
            string[] widthColumns = { "25%", "35%", "25%", "15%" }; // ширина отображаемых столбцов ([value]% или [value]px или autosize)
            bool addActionButtons = true; // добавление столбца с кнопками действий для удаления, добавления и изменения данных (True / False)
            bool activeFilter = true; // добавление ToolStrip в Column Header для фильтрации и выборки (True / False)

            new UniversalDataGridView(fileName, tableName, dataGridView1, excludedColumns, addActionButtons, activeFilter, widthColumns);
        }
    }
}
