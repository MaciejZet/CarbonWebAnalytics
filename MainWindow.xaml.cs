using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CarbonWebAnalytics
{
    public partial class MainWindow : Window
    {
        // Lista rekordów pobieranych z bazy (symulacja magazynu danych)
        private List<WebPageRecord> _records = new List<WebPageRecord>();
        // Przykładowy współczynnik emisji CO₂ w gramach na KB
        private const double CO2_FACTOR = 0.03; // 30 g CO₂ na 1 KB
        // Przykładowa wartość emisji CO₂ dla 10 000 użytkowników
        // Łańcuch połączenia do bazy SQLite
        private string connectionString = "Data Source=WebPages.db;Version=3;";

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase(); // Inicjalizacja / utworzenie tabeli, jeśli nie istnieje
            LoadRecords();
        }

        // Inicjalizacja bazy danych (tworzy tabelę, jeśli jej nie ma)
        private void InitializeDatabase()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string sql = "CREATE TABLE IF NOT EXISTS WebPages (" +
                                 "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                 "Url TEXT, " +
                                 "PageSizeKb REAL, " +
                                 "GeneratedCO2 REAL)";
                    using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd inicjalizacji bazy danych: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Ładowanie rekordów z bazy danych
        private void LoadRecords()
        {
            try
            {
                _records.Clear();
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM WebPages";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var record = new WebPageRecord
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Url = reader["Url"].ToString(),
                                PageSizeKb = Convert.ToDouble(reader["PageSizeKb"]),
                                GeneratedCO2 = Convert.ToDouble(reader["GeneratedCO2"])
                            };
                            _records.Add(record);
                        }
                    }
                }
                dgRecords.ItemsSource = null;
                dgRecords.ItemsSource = _records;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd przy ładowaniu rekordów: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Przycisk "Analizuj" – symulacja analizy strony
        private void btnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = txtUrl.Text.Trim();
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("Wprowadź poprawny URL.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(txtPageSize.Text, out double pageSize))
                {
                    MessageBox.Show("Wprowadź poprawny rozmiar w KB.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Obliczenie emisji CO₂ dla 10 000 użytkowników
                double generatedCO2 = pageSize * CO2_FACTOR * 10000;
                MessageBox.Show($"Strona: {url}\nRozmiar: {pageSize} KB\nGenerowane CO₂: {generatedCO2} g",
                                "Wynik analizy", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas analizy: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Przycisk "Dodaj" – dodawanie nowego rekordu
        // oraz aktualizacja danych w bazie
        // (dodawanie rekordu do bazy danych)
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = txtUrl.Text.Trim();
                if (string.IsNullOrEmpty(url) || !double.TryParse(txtPageSize.Text, out double pageSize))
                {
                    MessageBox.Show("Uzupełnij wszystkie pola poprawnie.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                double generatedCO2 = pageSize * CO2_FACTOR * 10000;
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string insertQuery = "INSERT INTO WebPages (Url, PageSizeKb, GeneratedCO2) VALUES (@Url, @PageSizeKb, @GeneratedCO2)";
                    using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Url", url);
                        cmd.Parameters.AddWithValue("@PageSizeKb", pageSize);
                        cmd.Parameters.AddWithValue("@GeneratedCO2", generatedCO2);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas dodawania rekordu: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Przycisk "Edytuj" – edycja zaznaczonego rekordu
        // oraz aktualizacja danych w bazie
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgRecords.SelectedItem == null)
                {
                    MessageBox.Show("Wybierz rekord do edycji.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                WebPageRecord record = dgRecords.SelectedItem as WebPageRecord;
                string url = txtUrl.Text.Trim();
                
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("URL nie może być pusty.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (!double.TryParse(txtPageSize.Text, out double pageSize))
                {
                    MessageBox.Show("Wprowadź poprawny rozmiar w KB.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                double generatedCO2 = pageSize * CO2_FACTOR * 10000;
                
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery = "UPDATE WebPages SET Url=@Url, PageSizeKb=@PageSizeKb, GeneratedCO2=@GeneratedCO2 WHERE Id=@Id";
                    using (SQLiteCommand cmd = new SQLiteCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Url", url);
                        cmd.Parameters.AddWithValue("@PageSizeKb", pageSize);
                        cmd.Parameters.AddWithValue("@GeneratedCO2", generatedCO2);
                        cmd.Parameters.AddWithValue("@Id", record.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                
                MessageBox.Show("Rekord został zaktualizowany.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas edycji rekordu: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Przycisk "Usuń" – usuwanie zaznaczonego rekordu
        // oraz resetowanie sekwencji Id w bazie danych
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgRecords.SelectedItem == null)
                {
                    MessageBox.Show("Wybierz rekord do usunięcia.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                WebPageRecord record = dgRecords.SelectedItem as WebPageRecord;
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string deleteQuery = "DELETE FROM WebPages WHERE Id=@Id";
                    using (SQLiteCommand cmd = new SQLiteCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", record.Id);
                        cmd.ExecuteNonQuery();
                    }
                    
                    // Reset SQLite
                    // sekwencji Id po usunięciu rekordu
                    using (SQLiteCommand cmd = new SQLiteCommand(conn))
                    {
                        // Sprawdzanie czy jakieś rekordy są w tabeli
                        // (czyli czy nie jest pusta)
                        cmd.CommandText = "SELECT COUNT(*) FROM WebPages";
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        if (count == 0)
                        {
                            // Jeżeli table jest pusta, usuwamy sekwencję
                            // (czyli resetujemy licznik Id)
                            cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name='WebPages'";
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            // Jeżeli table nie jest pusta, resetujemy licznik do ostatniego Id
                            // (czyli do Id największego rekordu)
                            cmd.CommandText = "UPDATE sqlite_sequence SET seq = (SELECT MAX(Id) FROM WebPages) WHERE name = 'WebPages'";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas usuwania rekordu: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Przycisk "Szukaj" – wyszukiwanie rekordów wg URL
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string searchUrl = txtUrl.Text.Trim().ToLower();
                var filtered = _records.Where(r => r.Url.ToLower().Contains(searchUrl)).ToList();
                dgRecords.ItemsSource = null;
                dgRecords.ItemsSource = filtered;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas wyszukiwania: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Przycisk "Raport" – obliczenie i wyświetlenie średnich wartości
        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_records.Count == 0)
                {
                    MessageBox.Show("Brak rekordów do raportu.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                double avgPageSize = _records.Average(r => r.PageSizeKb);
                double avgCO2 = _records.Average(r => r.GeneratedCO2);
                MessageBox.Show($"Średni rozmiar stron: {avgPageSize:F2} KB\nŚrednia emisja CO₂: {avgCO2:F2} g",
                                "Raport", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas generowania raportu: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Obsługa zmiany zaznaczenia w DataGrid
        private void dgRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRecords.SelectedItem is WebPageRecord selectedRecord)
            {
                txtUrl.Text = selectedRecord.Url;
                txtPageSize.Text = selectedRecord.PageSizeKb.ToString();
            }
        }
    }

    // Klasa reprezentująca rekord ze strony
    public class WebPageRecord
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public double PageSizeKb { get; set; }
        public double GeneratedCO2 { get; set; }
    }
}
