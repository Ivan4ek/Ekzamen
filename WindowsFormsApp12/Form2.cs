using DataGridViewExample;
using System;
using System.Data.SQLite;
using System.Windows.Forms;

namespace WindowsFormsApp12
{
    public partial class Form2 : Form
    {
        private string connectionString = "Data Source=C:\\Users\\Sterben\\Desktop\\databaseElzamen.db;Version=3;";

        public Form2()
        {
            InitializeComponent();
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            string username = tbLog.Text;
            string password = tbPass.Text;
            if (chbUsers.Checked)
            {
                if (AuthenticateAdmin(username, password))
                {
                    MessageBox.Show("Admin login successful!");

                    string fileName = "database.db"; // название файла базы данных
                    string tableName = "qwe"; // название таблицы в базе данных
                    string[] excludedColumns = { "ID" }; // список столбцов, которые необходимо исключить из отображения в DataGridView
                    string[] widthColumns = { "25%", "35%", "25%", "15%" }; // ширина отображаемых столбцов ([value]% или [value]px или autosize)
                    bool addActionButtons = true; // добавление столбца с кнопками действий для удаления, добавления и изменения данных (True / False)
                    bool activeFilter = true; // добавление ToolStrip в Column Header для фильтрации и выборки (True / False)

                    Form1 adminForm = new Form1(fileName, tableName, excludedColumns, addActionButtons, activeFilter, widthColumns);
                    adminForm.Show();
                    this.Hide();
                }
                else if (AuthenticateUser(username, password))
                {
                    MessageBox.Show("User login successful!");

                    string fileName = "database.db"; // название файла базы данных
                    string tableName = "qwe"; // название таблицы в базе данных
                    string[] excludedColumns = { "ID" }; // список столбцов, которые необходимо исключить из отображения в DataGridView
                    string[] widthColumns = { "25%", "35%", "25%", "15%" }; // ширина отображаемых столбцов ([value]% или [value]px или autosize)
                    bool addActionButtons = false; // добавление столбца с кнопками действий для удаления, добавления и изменения данных (True / False)
                    bool activeFilter = false; // добавление ToolStrip в Column Header для фильтрации и выборки (True / False)

                    Form1 userForm = new Form1(fileName, tableName, excludedColumns, addActionButtons, activeFilter, widthColumns);
                    userForm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Invalid username or password.");
                }
            }
            else
            {
                if (AuthenticateAdmin(username, password))
                {
                    MessageBox.Show("Admin login successful!");

                    string fileName = "databaseElzamen.db"; // название файла базы данных
                    string tableName = "users"; // название таблицы в базе данных
                    string[] excludedColumns = { "ID" }; // список столбцов, которые необходимо исключить из отображения в DataGridView
                    string[] widthColumns = { }; // ширина отображаемых столбцов ([value]% или [value]px или autosize)
                    bool addActionButtons = true; // добавление столбца с кнопками действий для удаления, добавления и изменения данных (True / False)
                    bool activeFilter = true; // добавление ToolStrip в Column Header для фильтрации и выборки (True / False)

                    Form1 adminForm = new Form1(fileName, tableName, excludedColumns, addActionButtons, activeFilter, widthColumns);
                    adminForm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Invalid username or password.");
                }
            }
        }

        private bool AuthenticateAdmin(string username, string password)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM admins WHERE name=@username";
                SQLiteCommand cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", username);
                connection.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count == 1;
            }
        }

        private bool AuthenticateUser(string username, string password)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM users WHERE name=@username";
                SQLiteCommand cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", username);
                connection.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count == 1;
            }
        }

        private void btnReg_Click(object sender, EventArgs e)
        {
            string username = tbLog.Text;
            string password = tbPass.Text;

            if (RegisterUser(username, password))
            {
                MessageBox.Show("Registration successful!");
            }
            else
            {
                MessageBox.Show("Registration failed.");
            }
        }

        private bool RegisterUser(string username, string password)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                string query = "INSERT INTO users (name) VALUES (@username)";
                SQLiteCommand cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", username);
                connection.Open();
                int result = cmd.ExecuteNonQuery();
                return result == 1;
            }
        }
    }
}
