using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace DataGridViewExample
{
    public class UniversalDataGridView
    {
        #region README
        //
        //  1. При наличии исключаемых столбцов ( excludedColumns = { [список_значений] } ) и включенной опции столбца действий ( addActionButtons = True )
        //  столбцы таблицы (кроме primary key (ID и пр.)) не должны быть NOT NULL
        //  2. В таблице базы данных обязательно должен быть столбец "ID"
        //
        //          сделано: при сдвижении таблицы и перезазгузке данных, я понял, что menuStrip в столбцах согдаются много раз, необходимо в будущем очищать их при перезагрузке
        //          сделано: ширина столбцов задается в параметрах класса
        //          необходимо в методах исключения строк при выборке сделать не исключение строк, а check-uncheck у Nodes treeView, и уже от состояния его узлов исключать строки
        //          добавить поиск по базе данных с помощью comboBox + подсказки при введении значения
        //          скрыть названия столбцов в оригинальном columnHeader (под ToolStrip)
        //          при сокращении текста и изменения размера до минимального, текст не расширяется обратно при увеличении размера
        #endregion

        public SQLiteConnection sqliteConnection;
        public string tableName;
        private readonly DataGridView dataGridView;
        private readonly string[] excludedColumns;
        private readonly bool addActionButtons;
        private readonly bool activeFilter;
        private readonly List<ToolStrip> toolStrips = new List<ToolStrip>();
        private readonly ColumnWidthManager columnWidthManager;

        public UniversalDataGridView(string fileName, string tableName, DataGridView dataGridView, string[] excludedColumns = null, bool addActionButtons = false, bool activeFilter = false, string[] widthColumns = null)
        {
            this.tableName = tableName;
            this.dataGridView = dataGridView;
            this.excludedColumns = excludedColumns;
            this.addActionButtons = addActionButtons;

            sqliteConnection = new SQLiteConnection($"Data Source={fileName};Version=3;");
            sqliteConnection.Open();

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView.CellClick += DataGridView_CellClick;
            dataGridView.EditingControlShowing += DataGridView_EditingControlShowing;
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;

            this.activeFilter = activeFilter;

            // Initialize ColumnWidthManager
            columnWidthManager = new ColumnWidthManager(dataGridView, widthColumns, excludedColumns);

            LoadData();
        }

        public List<ToolStrip> ToolStrips
        {
            get { return toolStrips; }
        }

        public void LoadData()
        {
            dataGridView.Columns.Clear();
            dataGridView.Rows.Clear();

            // Удаляем старые ToolStrip
            foreach (var toolStrip in toolStrips)
            {
                dataGridView.Controls.Remove(toolStrip);
                toolStrip.Dispose();
            }
            toolStrips.Clear();

            using (SQLiteDataReader reader = GetAllData(tableName))
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetName(i);
                    if (excludedColumns != null && Array.Exists(excludedColumns, col => col == columnName))
                    {
                        if (columnName == "ID")
                        {
                            dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = columnName, Visible = false });
                        }
                        else
                        {
                            dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = columnName, Visible = false });
                        }
                        continue;
                    }
                    dataGridView.Columns.Add(columnName, columnName);
                }

                while (reader.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetName(i);
                        if (excludedColumns != null && Array.Exists(excludedColumns, col => col == columnName))
                        {
                            if (columnName == "ID")
                            {
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = reader[i] });
                            }
                            else
                            {
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = DBNull.Value });
                            }
                            continue;
                        }
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = reader[i] });
                    }
                    dataGridView.Rows.Add(row);
                }
            }

            if (addActionButtons)
            {
                DataGridViewButtonColumn buttonColumn = new DataGridViewButtonColumn
                {
                    Name = "actionColumn",
                    HeaderText = "Действие"
                };
                dataGridView.Columns.Add(buttonColumn);
                UpdateActionButtons();
            }

            if (activeFilter)
            {
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;

                    if (column.Visible == true && column.Name != "actionColumn")
                    {
                        column.HeaderCell = new CustomHeaderCellForStandartColumns(dataGridView, column, this);
                    }
                    if (column.Name == "actionColumn")
                    {
                        column.HeaderCell = new CustomHeaderCellForActionColumns(dataGridView, column, this);
                    }
                }
            }

            // Apply column widths
            columnWidthManager.ApplyColumnWidths();
        }

        public SQLiteDataReader GetAllData(string tableName)
        {
            string selectQuery = $"SELECT * FROM {tableName}";
            SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, sqliteConnection);
            return selectCommand.ExecuteReader();
        }

        private void UpdateActionButtons()
        {
            for (int i = 0; i < dataGridView.Rows.Count; i++)
            {
                if (i == dataGridView.Rows.Count - 1)
                {
                    dataGridView.Rows[i].Cells["actionColumn"].Value = "Добавить";
                }
                else
                {
                    dataGridView.Rows[i].Cells["actionColumn"].Value = "Удалить";
                }
            }
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView.Columns["actionColumn"]?.Index)
            {
                string action = dataGridView.Rows[e.RowIndex].Cells["actionColumn"].Value.ToString();
                if (action == "Добавить")
                {
                    if (IsRowValid(e.RowIndex))
                    {
                        AddData(e.RowIndex);
                        dataGridView.Rows[e.RowIndex].Cells["actionColumn"].Value = "Удалить";
                        UpdateActionButtons();
                        dataGridView.Refresh();
                        UpdateTreeViewAndComboBox();
                    }
                    else
                    {
                        MessageBox.Show("Пожалуйста, заполните все поля перед добавлением.");
                    }
                }
                else if (action == "Удалить")
                {
                    int id = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["ID"].Value);
                    DeleteData(tableName, id);
                    dataGridView.Rows.RemoveAt(e.RowIndex);
                    UpdateActionButtons();
                    dataGridView.Refresh();
                    UpdateTreeViewAndComboBox();
                }
                else if (action == "Изменить")
                {
                    if (IsRowValid(e.RowIndex))
                    {
                        int id = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["ID"].Value);
                        UpdateData(id, e.RowIndex);
                        dataGridView.Rows[e.RowIndex].Cells["actionColumn"].Value = "Удалить";
                        dataGridView.Refresh();
                        UpdateTreeViewAndComboBox();
                    }
                    else
                    {
                        MessageBox.Show("Пожалуйста, заполните все поля перед изменением.");
                    }
                }
            }
        }

        private void DataGridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (addActionButtons)
            {
                if (dataGridView.CurrentCell.ColumnIndex != dataGridView.Columns["actionColumn"]?.Index)
                {
                    if (dataGridView.CurrentCell is DataGridViewTextBoxCell)
                    {
                        if (e.Control is TextBox textBox)
                        {
                            textBox.TextChanged -= TextBox_TextChanged;
                            textBox.TextChanged += TextBox_TextChanged;
                        }
                    }
                }
            }
            else { return; }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (addActionButtons)
            {
                int rowIndex = dataGridView.CurrentCell.RowIndex;
                if (dataGridView.Rows[rowIndex].Cells["actionColumn"]?.Value.ToString() == "Удалить")
                {
                    dataGridView.Rows[rowIndex].Cells["actionColumn"].Value = "Изменить";
                }
            }
            else { return; }
        }

        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (addActionButtons)
            {
                if (e.ColumnIndex != dataGridView.Columns["actionColumn"]?.Index && e.RowIndex >= 0)
                {
                    int rowIndex = e.RowIndex;
                    if (dataGridView.Rows[rowIndex].Cells["actionColumn"].Value.ToString() == "Удалить")
                    {
                        dataGridView.Rows[rowIndex].Cells["actionColumn"].Value = "Изменить";
                    }
                }
            }
            else { return; }
        }

        private bool IsRowValid(int rowIndex)
        {
            for (int i = 1; i < dataGridView.Columns.Count - 1; i++)
            {
                if (excludedColumns != null && Array.Exists(excludedColumns, col => col == dataGridView.Columns[i].Name))
                {
                    dataGridView.Rows[rowIndex].Cells[i].Value = DBNull.Value;
                }
                else if (dataGridView.Rows[rowIndex].Cells[i].Value == null || string.IsNullOrWhiteSpace(dataGridView.Rows[rowIndex].Cells[i].Value.ToString()))
                {
                    return false;
                }
            }
            return true;
        }

        private void AddData(int rowIndex)
        {
            try
            {
                string columns = string.Join(", ", GetColumnNames(rowIndex));
                string values = string.Join(", ", GetParameterNames(rowIndex));
                string insertQuery = $@"INSERT INTO {tableName} ({columns}) VALUES ({values});";

                using (var command = new SQLiteCommand(insertQuery, sqliteConnection))
                {
                    AddParameters(command, rowIndex);
                    command.ExecuteNonQuery();
                }

                long id = sqliteConnection.LastInsertRowId;
                dataGridView.Rows[rowIndex].Cells["ID"].Value = id;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении данных: {ex.Message}");
            }
        }

        private void UpdateData(int id, int rowIndex)
        {
            try
            {
                string setClause = string.Join(", ", GetSetClause(rowIndex));
                string updateQuery = $@"UPDATE {tableName} SET {setClause} WHERE ID = @ID";

                using (var command = new SQLiteCommand(updateQuery, sqliteConnection))
                {
                    AddParameters(command, rowIndex);
                    command.Parameters.AddWithValue("@ID", id);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}");
            }
        }

        public void DeleteData(string tableName, int id)
        {
            string deleteQuery = $"DELETE FROM {tableName} WHERE ID = @ID";
            SQLiteCommand deleteCommand = new SQLiteCommand(deleteQuery, sqliteConnection);
            deleteCommand.Parameters.AddWithValue("@ID", id);
            deleteCommand.ExecuteNonQuery();
        }

        private string GetColumnNames(int rowIndex)
        {
            var columnNames = new List<string>();
            foreach (DataGridViewCell cell in dataGridView.Rows[rowIndex].Cells)
            {
                if (cell.OwningColumn.Name == "actionColumn" || cell.OwningColumn.Name == "ID") continue;
                columnNames.Add(cell.OwningColumn.Name);
            }
            return string.Join(", ", columnNames);
        }

        private string GetParameterNames(int rowIndex)
        {
            var parameterNames = new List<string>();
            foreach (DataGridViewCell cell in dataGridView.Rows[rowIndex].Cells)
            {
                if (cell.OwningColumn.Name == "actionColumn" || cell.OwningColumn.Name == "ID") continue;
                parameterNames.Add($"@{cell.OwningColumn.Name}");
            }
            return string.Join(", ", parameterNames);
        }

        private string GetSetClause(int rowIndex)
        {
            var setClause = new List<string>();
            foreach (DataGridViewCell cell in dataGridView.Rows[rowIndex].Cells)
            {
                if (cell.OwningColumn.Name == "actionColumn" || cell.OwningColumn.Name == "ID") continue;
                setClause.Add($"{cell.OwningColumn.Name} = @{cell.OwningColumn.Name}");
            }
            return string.Join(", ", setClause);
        }

        private void AddParameters(SQLiteCommand command, int rowIndex)
        {
            foreach (DataGridViewCell cell in dataGridView.Rows[rowIndex].Cells)
            {
                if (cell.OwningColumn.Name == "actionColumn" || cell.OwningColumn.Name == "ID") continue;
                command.Parameters.AddWithValue($"@{cell.OwningColumn.Name}", cell.Value ?? DBNull.Value);
            }
        }

        private void UpdateTreeViewAndComboBox()
        {
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (column.Visible == true && column.Name != "actionColumn")
                {
                    using (var headerCell = column.HeaderCell as CustomHeaderCellForStandartColumns)
                    {
                        if (headerCell != null)
                        {
                            headerCell.LoadTreeViewItems(headerCell.treeView, column, sqliteConnection, tableName);
                            headerCell.LoadComboBoxItems(headerCell.comboBox, column, sqliteConnection, tableName);
                        }
                    }
                }
            }
        }
    }

    #region Filter Helper

    public abstract class CustomHeaderCellBase : DataGridViewColumnHeaderCell
    {
        protected ToolStrip toolStrip;
        protected ToolStripLabel headerLabel;
        protected ToolTip toolTip;

        protected CustomHeaderCellBase(DataGridView dataGridView, DataGridViewColumn column)
        {
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomCenter;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(204)));
            dataGridView.ForeColor = SystemColors.WindowText;
            dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            dataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            dataGridView.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

            toolStrip = new ToolStrip
            {
                AutoSize = false,
                Height = 25,
                Dock = DockStyle.None,
                BackColor = Color.Transparent,
                RenderMode = ToolStripRenderMode.Professional,
                Renderer = new ToolStripProfessionalRenderer(new CustomToolStripColorTable())
            };

            headerLabel = new ToolStripLabel
            {
                Text = column.HeaderText,
                ForeColor = Color.Black
            };

            toolStrip.Items.Add(headerLabel);

            dataGridView.Controls.Add(toolStrip);
            dataGridView.ColumnHeadersHeight = toolStrip.Height + 1;
            toolStrip.MouseEnter += (s, ev) => toolStrip.Focus();

            toolTip = new ToolTip
            {
                ShowAlways = true,
                InitialDelay = 0,
                ReshowDelay = 0,
                AutoPopDelay = 5000
            };

            toolTip.SetToolTip(toolStrip, column.HeaderText);
        }

        protected string ShortenText(string text, int maxWidth)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                while (TextRenderer.MeasureText(text, headerLabel.Font).Width > maxWidth - 1)
                {
                    if (text.Length <= 4)
                    {
                        return text;
                    }
                    text = text.Substring(0, text.Length - 4) + "...";
                }
            }
            return text;
        }

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            // Отключаем рисование текста и фона заголовка столбца
            paintParts &= ~(DataGridViewPaintParts.ContentForeground | DataGridViewPaintParts.Background);

            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            toolStrip.Bounds = new Rectangle(cellBounds.X + 1, cellBounds.Y + 1, cellBounds.Width - 1, toolStrip.Height);
            toolStrip.Visible = true;

            headerLabel.Text = ShortenText(headerLabel.Text, toolStrip.Width - GetButtonsWidth() - 10);
        }

        protected abstract int GetButtonsWidth();
    }

    public class CustomHeaderCellForStandartColumns : CustomHeaderCellBase
    {
        private readonly ToolStripDropDownButton dropDownButtonFilter;
        private readonly ToolStripDropDownButton dropDownButtonDataSampling;
        public ToolStrip toolStripDataSampling;
        public ToolStripComboBox comboBox;
        public TreeView treeView;

        public CustomHeaderCellForStandartColumns(DataGridView dataGridView, DataGridViewColumn column, UniversalDataGridView dataGridViewInstance)
            : base(dataGridView, column)
        {
            dropDownButtonFilter = new ToolStripDropDownButton
            {
                Image = SystemIcons.Information.ToBitmap()
            };
            dropDownButtonFilter.DropDownItems.Add(new ToolStripSeparator());
            dropDownButtonFilter.DropDownItems.Add(new ToolStripLabel("Фильтр:"));
            dropDownButtonFilter.DropDownItems.Add(new ToolStripSeparator());
            dropDownButtonFilter.DropDownItems.Add("Сортировать по возрастанию", null, (sender, e) =>
            {
                dataGridView.Sort(dataGridView.Columns[column.Name], ListSortDirection.Ascending);
            });
            dropDownButtonFilter.DropDownItems.Add("Сортировать по убыванию", null, (sender, e) =>
            {
                dataGridView.Sort(dataGridView.Columns[column.Name], ListSortDirection.Descending);
            });
            dropDownButtonFilter.DropDownItems.Add(new ToolStripSeparator());

            dropDownButtonDataSampling = new ToolStripDropDownButton
            {
                Image = SystemIcons.Warning.ToBitmap()
            };

            toolStripDataSampling = new ToolStrip
            {
                AutoSize = true,
                Height = 25,
                Dock = DockStyle.None,
                BackColor = Color.Transparent,
                RenderMode = ToolStripRenderMode.Professional,
                Renderer = new ToolStripProfessionalRenderer(new CustomToolStripColorTable()),
                CanOverflow = false
            };
            toolStripDataSampling.MouseEnter += (s, ev) => toolStripDataSampling.Focus();

            toolStripDataSampling.Items.Add(new ToolStripLabel("Значение:"));

            comboBox = new ToolStripComboBox
            {
                BackColor = SystemColors.Window,
                FlatStyle = FlatStyle.Standard
            };

            toolStripDataSampling.Items.Add(comboBox);

            toolStripDataSampling.Items.Add(new ToolStripSeparator());

            ToolStripButton buttonDataSamplingExcludingThisValue = new ToolStripButton
            {
                Image = SystemIcons.Information.ToBitmap()
            };
            buttonDataSamplingExcludingThisValue.Click += (sender, e) => ButtonDataSamplingExcludingThisValue(dataGridView, column, comboBox);
            toolStripDataSampling.Items.Add(buttonDataSamplingExcludingThisValue);

            ToolStripButton buttonDataSamplingLeavingOnlyThisValue = new ToolStripButton
            {
                Image = SystemIcons.Question.ToBitmap()
            };
            buttonDataSamplingLeavingOnlyThisValue.Click += (sender, e) => ButtonDataSamplingLeavingOnlyThisValue(dataGridView, column, comboBox);
            toolStripDataSampling.Items.Add(buttonDataSamplingLeavingOnlyThisValue);

            TableLayoutPanel tableLayoutPanelForToolStripMenu = new TableLayoutPanel();
            tableLayoutPanelForToolStripMenu.ColumnCount = 1;
            tableLayoutPanelForToolStripMenu.RowCount = 1;
            tableLayoutPanelForToolStripMenu.AutoSize = false;

            tableLayoutPanelForToolStripMenu.Controls.Add(toolStripDataSampling, 0, 0);
            tableLayoutPanelForToolStripMenu.BackColor = Color.Transparent;

            ToolStripControlHost tableLayoutHostForToolStripMenu = new ToolStripControlHost(tableLayoutPanelForToolStripMenu);

            treeView = new TreeView
            {
                AutoSize = true,
                Width = toolStripDataSampling.Width,
                Height = toolStripDataSampling.Height * 7,
                Dock = DockStyle.None,
                BackColor = Color.White,
                CheckBoxes = true
            };

            treeView.Nodes.Clear();
            treeView.Nodes.Add(column.Name);
            treeView.Nodes[0].Nodes.Add("1");
            treeView.Nodes[0].Nodes.Add("2");

            ImageList stateImageList = new ImageList();
            stateImageList.Images.Add(SystemIcons.Information.ToBitmap());
            stateImageList.Images.Add(SystemIcons.Error.ToBitmap());

            treeView.StateImageList = stateImageList;

            ToolStripControlHost tableLayoutHostForTreeView = new ToolStripControlHost(treeView);

            dropDownButtonDataSampling.DropDownItems.Add(new ToolStripSeparator());
            dropDownButtonDataSampling.DropDownItems.Add(new ToolStripLabel("Выборка:"));
            dropDownButtonDataSampling.DropDownItems.Add(new ToolStripSeparator());
            dropDownButtonDataSampling.DropDownItems.Add(tableLayoutHostForToolStripMenu);
            dropDownButtonDataSampling.DropDownItems.Add(new ToolStripSeparator());
            dropDownButtonDataSampling.DropDownItems.Add(tableLayoutHostForTreeView);
            dropDownButtonDataSampling.DropDownItems.Add(new ToolStripSeparator());

            LoadTreeViewItems(treeView, column, dataGridViewInstance.sqliteConnection, dataGridViewInstance.tableName);

            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(dropDownButtonFilter);
            toolStrip.Items.Add(dropDownButtonDataSampling);
            toolStrip.Items.Add(new ToolStripSeparator());

            comboBox.DropDown += (sender, e) => LoadComboBoxItems(comboBox, column, dataGridViewInstance.sqliteConnection, dataGridViewInstance.tableName);
            dataGridView.CellValueChanged += (sender, e) => LoadTreeViewItems(treeView, column, dataGridViewInstance.sqliteConnection, dataGridViewInstance.tableName);

            // Добавляем ToolStrip в список для последующего удаления
            dataGridViewInstance.ToolStrips.Add(toolStrip);
        }

        private void ButtonDataSamplingExcludingThisValue(DataGridView dataGridView, DataGridViewColumn column, ToolStripComboBox comboBox)
        {
            if (!string.IsNullOrEmpty(comboBox.Text))
            {
                string selectedValue = comboBox.Text;

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.Cells[column.Index].Value != null && row.Cells[column.Index].Value.ToString() == selectedValue)
                    {
                        row.Visible = false;
                    }
                    else
                    {
                        row.Visible = true;
                    }
                }
            }
        }

        private void ButtonDataSamplingLeavingOnlyThisValue(DataGridView dataGridView, DataGridViewColumn column, ToolStripComboBox comboBox)
        {
            if (!string.IsNullOrEmpty(comboBox.Text))
            {
                string selectedValue = comboBox.Text;

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        if (row.Cells[column.Index].Value != null && row.Cells[column.Index].Value.ToString() == selectedValue)
                        {
                            row.Visible = true;
                        }
                        else
                        {
                            row.Visible = false;
                        }
                    }
                }
            }
        }

        public void LoadTreeViewItems(TreeView treeView, DataGridViewColumn column, SQLiteConnection sqliteConnection, string tableName)
        {
            try
            {
                if (column.Visible == true && column.Name != "actionColumn")
                {
                    string selectQuery = $"SELECT DISTINCT {column.Name} FROM {tableName}";
                    using (SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, sqliteConnection))
                    {
                        using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                        {
                            var items = new List<object>();
                            while (reader.Read())
                            {
                                items.Add(reader[column.Name].ToString());
                            }

                            treeView.Nodes[0].Nodes.Clear();
                            foreach (var item in items)
                            {
                                treeView.Nodes[0].Nodes.Add(item.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных в TreeView: {ex.Message}");
            }
        }

        public void LoadComboBoxItems(ToolStripComboBox comboBox, DataGridViewColumn column, SQLiteConnection sqliteConnection, string tableName)
        {
            try
            {
                if (column.Visible == true && column.Name != "actionColumn")
                {
                    string selectQuery = $"SELECT DISTINCT {column.Name} FROM {tableName}";
                    using (SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, sqliteConnection))
                    {
                        using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                        {
                            var items = new List<object>();
                            while (reader.Read())
                            {
                                items.Add(reader[column.Name].ToString());
                            }
                            comboBox.Items.Clear();
                            comboBox.Items.AddRange(items.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных в ComboBox: {ex.Message}");
            }
        }

        protected override int GetButtonsWidth()
        {
            return dropDownButtonFilter.Width + dropDownButtonDataSampling.Width;
        }
    }

    public class CustomHeaderCellForActionColumns : CustomHeaderCellBase
    {
        private readonly ToolStripButton buttonUpdateData;
        private readonly ToolStripSplitButton buttonAllDataSampling;
        private readonly ToolStripButton buttonFUCK;

        private readonly ToolStrip toolStripAllDataSamplingExcludingThisValue;
        private readonly ToolStrip toolStripAllDataSamplingLeavingOnlyThisValue;
        private DataGridView globalDataGridView;
        private List<DataGridViewColumn> globalColumns;
        private List<ToolStripComboBox> globalComboBoxes;

        public CustomHeaderCellForActionColumns(DataGridView dataGridView, DataGridViewColumn column, UniversalDataGridView dataGridViewInstance)
            : base(dataGridView, column)
        {

            List<DataGridViewColumn> columns = new List<DataGridViewColumn>();
            List<ToolStripComboBox> comboBoxes = new List<ToolStripComboBox>();

            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                if (col.Visible && col.Name != "actionColumn")
                {
                    columns.Add(col);
                    using (var headerCell = col.HeaderCell as CustomHeaderCellForStandartColumns)
                    {
                        if (headerCell != null)
                        {
                            comboBoxes.Add(headerCell.comboBox);
                        }
                    }
                }
            }

            buttonAllDataSampling = new ToolStripSplitButton
            {
                Image = SystemIcons.Warning.ToBitmap()
            };
            buttonAllDataSampling.DropDownItems.Add(new ToolStripSeparator());
            buttonAllDataSampling.DropDownItems.Add(new ToolStripLabel("Общая выборка:"));
            buttonAllDataSampling.DropDownItems.Add(new ToolStripSeparator());

            toolStripAllDataSamplingExcludingThisValue = new ToolStrip
            {
                AutoSize = true,
                Height = 25,
                Dock = DockStyle.None,
                BackColor = Color.Transparent,
                RenderMode = ToolStripRenderMode.Professional,
                Renderer = new ToolStripProfessionalRenderer(new CustomToolStripColorTable()),
                CanOverflow = false
            };
            toolStripAllDataSamplingExcludingThisValue.MouseEnter -= (s, ev) => toolStripAllDataSamplingExcludingThisValue.Focus();
            toolStripAllDataSamplingExcludingThisValue.MouseEnter += (s, ev) => toolStripAllDataSamplingExcludingThisValue.Focus();
            toolStripAllDataSamplingExcludingThisValue.Items.Add(new ToolStripLabel("Исключить:"));
            ToolStripButton buttonAllDataSamplingExcludingThisValue = new ToolStripButton
            {
                Image = SystemIcons.Information.ToBitmap()
            };
            buttonAllDataSamplingExcludingThisValue.Click -= (sender, e) => ButtonDataSamplingExcludingThisValues(dataGridView, columns, comboBoxes);
            buttonAllDataSamplingExcludingThisValue.Click += (sender, e) => ButtonDataSamplingExcludingThisValues(dataGridView, columns, comboBoxes);
            toolStripAllDataSamplingExcludingThisValue.Items.Add(buttonAllDataSamplingExcludingThisValue);
            TableLayoutPanel tableLayoutPanelForToolStripMenuExcludingThisValue = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 1,
                AutoSize = false,
                Size = toolStripAllDataSamplingExcludingThisValue.Size - new Size(25, 0),
                BackColor = Color.Transparent
            };
            tableLayoutPanelForToolStripMenuExcludingThisValue.Controls.Add(toolStripAllDataSamplingExcludingThisValue, 0, 0);
            ToolStripControlHost tableLayoutHostForToolStripMenuExcludingThisValue = new ToolStripControlHost(tableLayoutPanelForToolStripMenuExcludingThisValue);
            buttonAllDataSampling.DropDownItems.Add(tableLayoutHostForToolStripMenuExcludingThisValue);
            buttonAllDataSampling.DropDownItems.Add(new ToolStripSeparator());
            toolStripAllDataSamplingLeavingOnlyThisValue = new ToolStrip
            {
                AutoSize = true,
                Height = 25,
                Dock = DockStyle.None,
                BackColor = Color.Transparent,
                RenderMode = ToolStripRenderMode.Professional,
                Renderer = new ToolStripProfessionalRenderer(new CustomToolStripColorTable()),
                CanOverflow = false
            };
            toolStripAllDataSamplingLeavingOnlyThisValue.MouseEnter -= (s, ev) => toolStripAllDataSamplingLeavingOnlyThisValue.Focus();
            toolStripAllDataSamplingLeavingOnlyThisValue.MouseEnter += (s, ev) => toolStripAllDataSamplingLeavingOnlyThisValue.Focus();
            toolStripAllDataSamplingLeavingOnlyThisValue.Items.Add(new ToolStripLabel("Выбрать:"));
            ToolStripButton buttonAllDataSamplingLeavingOnlyThisValue = new ToolStripButton
            {
                Image = SystemIcons.Question.ToBitmap()
            };
            buttonAllDataSamplingLeavingOnlyThisValue.Click -= (sender, e) => ButtonDataSamplingLeavingOnlyThisValues(dataGridView, columns, comboBoxes);
            buttonAllDataSamplingLeavingOnlyThisValue.Click += (sender, e) => ButtonDataSamplingLeavingOnlyThisValues(dataGridView, columns, comboBoxes);
            toolStripAllDataSamplingLeavingOnlyThisValue.Items.Add(buttonAllDataSamplingLeavingOnlyThisValue);
            TableLayoutPanel tableLayoutPanelForToolStripMenuLeavingOnlyThisValue = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 1,
                AutoSize = false,
                Size = toolStripAllDataSamplingLeavingOnlyThisValue.Size - new Size(25, 0),
                BackColor = Color.Transparent
            };
            tableLayoutPanelForToolStripMenuLeavingOnlyThisValue.Controls.Add(toolStripAllDataSamplingLeavingOnlyThisValue, 0, 0);
            ToolStripControlHost tableLayoutHostForToolStripMenuLeavingOnlyThisValue = new ToolStripControlHost(tableLayoutPanelForToolStripMenuLeavingOnlyThisValue);

            buttonAllDataSampling.DropDownItems.Add(tableLayoutHostForToolStripMenuLeavingOnlyThisValue);
            buttonAllDataSampling.DropDownItems.Add(new ToolStripSeparator());

            buttonUpdateData = new ToolStripButton
            {
                Image = SystemIcons.Error.ToBitmap()
            };

            buttonFUCK = new ToolStripButton
            {
                Image = SystemIcons.Warning.ToBitmap()
            };

            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(buttonAllDataSampling);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(buttonUpdateData);
            toolStrip.Items.Add(buttonFUCK);
            toolStrip.Items.Add(new ToolStripSeparator());

            buttonUpdateData.Click -= (sender, e) => dataGridViewInstance.LoadData();
            buttonUpdateData.Click += (sender, e) => dataGridViewInstance.LoadData();

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Добавляем ToolStrip в список для последующего удаления
            dataGridViewInstance.ToolStrips.Add(toolStrip);
        }

        protected override int GetButtonsWidth()
        {
            return buttonUpdateData.Width + buttonAllDataSampling.Width + buttonFUCK.Width;
        }

        private void ButtonDataSamplingExcludingThisValues(DataGridView dataGridView, List<DataGridViewColumn> columns, List<ToolStripComboBox> comboBoxes)
        {
            globalDataGridView = dataGridView;
            globalColumns = columns;
            globalComboBoxes = comboBoxes;

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (!row.IsNewRow)
                {
                    bool match = true;
                    for (int i = 0; i < columns.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(comboBoxes[i].Text))
                        {
                            string selectedValue = comboBoxes[i].Text;
                            if (row.Cells[columns[i].Index].Value == null || row.Cells[columns[i].Index].Value.ToString() != selectedValue)
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                    row.Visible = !match;
                }
            }

            buttonAllDataSampling.Image?.Dispose();
            buttonAllDataSampling.Image = SystemIcons.Information.ToBitmap();

            buttonAllDataSampling.ButtonClick -= ButtonDataSamplingExcludingThisValuesHandler;
            buttonAllDataSampling.ButtonClick += ButtonDataSamplingExcludingThisValuesHandler;
        }

        private void ButtonDataSamplingLeavingOnlyThisValues(DataGridView dataGridView, List<DataGridViewColumn> columns, List<ToolStripComboBox> comboBoxes)
        {
            globalDataGridView = dataGridView;
            globalColumns = columns;
            globalComboBoxes = comboBoxes;

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                bool includeRow = true;

                for (int i = 0; i < columns.Count; i++)
                {
                    if (!string.IsNullOrEmpty(comboBoxes[i].Text))
                    {
                        if (!row.IsNewRow)
                        {
                            string selectedValue = comboBoxes[i].Text;
                            if (row.Cells[columns[i].Index].Value == null || row.Cells[columns[i].Index].Value.ToString() != selectedValue)
                            {
                                includeRow = false;
                                break;
                            }
                        }
                    }
                }
                row.Visible = includeRow;
            }

            buttonAllDataSampling.Image?.Dispose();
            buttonAllDataSampling.Image = SystemIcons.Question.ToBitmap();

            buttonAllDataSampling.ButtonClick -= ButtonDataSamplingLeavingOnlyThisValuesHandler;
            buttonAllDataSampling.ButtonClick += ButtonDataSamplingLeavingOnlyThisValuesHandler;
        }

        private void ButtonDataSamplingExcludingThisValuesHandler(object sender, EventArgs e)
        {
            ButtonDataSamplingExcludingThisValues(globalDataGridView, globalColumns, globalComboBoxes);
        }

        private void ButtonDataSamplingLeavingOnlyThisValuesHandler(object sender, EventArgs e)
        {
            ButtonDataSamplingLeavingOnlyThisValues(globalDataGridView, globalColumns, globalComboBoxes);
        }

    }

    public class CustomToolStripColorTable : ProfessionalColorTable
    {
        public override Color ToolStripBorder => Color.Transparent;
        public override Color ToolStripContentPanelGradientBegin => Color.Transparent;
        public override Color ToolStripContentPanelGradientEnd => Color.Transparent;
        public override Color ToolStripGradientBegin => Color.Transparent;
        public override Color ToolStripGradientEnd => Color.Transparent;
        public override Color ToolStripGradientMiddle => Color.Transparent;
        public override Color ToolStripPanelGradientBegin => Color.Transparent;
        public override Color ToolStripPanelGradientEnd => Color.Transparent;
    }

    #endregion

    public class ColumnWidthManager
    {
        private readonly DataGridView dataGridView;
        private readonly string[] widthColumns;
        private readonly string[] excludedColumns;

        public ColumnWidthManager(DataGridView dataGridView, string[] widthColumns, string[] excludedColumns)
        {
            this.dataGridView = dataGridView;
            this.widthColumns = widthColumns;
            this.excludedColumns = excludedColumns;
        }

        public void ApplyColumnWidths()
        {
            if (widthColumns == null || widthColumns.Length == 0) return;

            int totalPercentageWidth = 0;
            int totalPixelWidth = 0;
            int autoSizeColumnCount = 0;

            // First pass: Calculate total width for percentage and pixel columns
            int widthIndex = 0;
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (excludedColumns != null && Array.Exists(excludedColumns, col => col == column.Name))
                {
                    continue;
                }

                if (widthIndex < widthColumns.Length)
                {
                    string widthValue = widthColumns[widthIndex];
                    if (widthValue.EndsWith("%"))
                    {
                        if (int.TryParse(widthValue.TrimEnd('%'), out int percentage))
                        {
                            totalPercentageWidth += percentage;
                        }
                    }
                    else if (widthValue.EndsWith("px"))
                    {
                        if (int.TryParse(widthValue.TrimEnd('x', 'p'), out int pixels))
                        {
                            totalPixelWidth += pixels;
                        }
                    }
                    else if (widthValue.ToLower() == "autosize")
                    {
                        autoSizeColumnCount++;
                    }
                    widthIndex++;
                }
            }

            // Calculate remaining width for autosize columns
            int remainingWidth = dataGridView.Width - totalPixelWidth - (dataGridView.Width * totalPercentageWidth) / 100;
            int autoSizeWidth = autoSizeColumnCount > 0 ? remainingWidth / autoSizeColumnCount : 0;

            // Second pass: Apply widths to columns
            widthIndex = 0;
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (excludedColumns != null && Array.Exists(excludedColumns, col => col == column.Name))
                {
                    continue;
                }

                if (widthIndex < widthColumns.Length)
                {
                    string widthValue = widthColumns[widthIndex];
                    if (widthValue.EndsWith("%"))
                    {
                        if (int.TryParse(widthValue.TrimEnd('%'), out int percentage))
                        {
                            column.Width = (dataGridView.Width * percentage) / 100;
                        }
                    }
                    else if (widthValue.EndsWith("px"))
                    {
                        if (int.TryParse(widthValue.TrimEnd('x', 'p'), out int pixels))
                        {
                            column.Width = pixels;
                        }
                    }
                    else if (widthValue.ToLower() == "autosize")
                    {
                        column.Width = autoSizeWidth;
                    }
                    widthIndex++;
                }
            }
        }
    }
}
