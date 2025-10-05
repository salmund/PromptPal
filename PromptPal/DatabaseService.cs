using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace PromptPal
{

    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=prompts.db";

        public void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS prompts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT NOT NULL,
                    content TEXT NOT NULL,
                    category TEXT,
                    tags TEXT
                );
            ";
                command.ExecuteNonQuery();
            }
        }

        public void AddPrompt(Prompt prompt)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Vérifier si un prompt avec le même titre existe déjà
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM prompts WHERE title = @title";
                checkCommand.Parameters.AddWithValue("@title", prompt.Title);
                var count = (long)checkCommand.ExecuteScalar();

                if (count == 0)
                {
                    var command = connection.CreateCommand();
                    command.CommandText =
                        "INSERT INTO prompts (title, content, category, tags) VALUES (@title, @content, @category, @tags)";
                    command.Parameters.AddWithValue("@title", prompt.Title);
                    command.Parameters.AddWithValue("@content", prompt.Content);
                    command.Parameters.AddWithValue("@category", prompt.Category ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@tags", prompt.Tags ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Prompt> SearchPrompts(string searchText)
        {
            var results = new List<Prompt>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                // Recherche insensible à la casse dans le titre et le contenu
                command.CommandText = @"
                SELECT id, title, content, category, tags
                FROM prompts
                WHERE LOWER(title) LIKE @search OR LOWER(content) LIKE @search OR LOWER(tags) LIKE @search
                ORDER BY title ASC
                LIMIT 10"; // Limitez les résultats pour la performance

                command.Parameters.AddWithValue("@search", $"%{searchText.ToLower()}%");

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Prompt
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Content = reader.GetString(2),
                            Category = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Tags = reader.IsDBNull(4) ? null : reader.GetString(4)
                        });
                    }
                }
            }
            return results;
        }
    }
}