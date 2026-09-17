using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TodoApp.Exceptions;
using TodoApp.Models;

namespace TodoApp.Services
{
    /// <summary>
    /// Файловое хранилище данных приложения. Полностью работает через потоки
    /// (FileStream/BufferedStream/CryptoStream/StreamReader/StreamWriter),
    /// не использует File.ReadAllLines/WriteAllLines и не зависит от AppInfo —
    /// все данные передаются через параметры методов.
    /// </summary>
    public class FileManager : IDataStorage
    {
        private const string DataDir = "data";
        private const string ProfilesFileName = "profiles.dat";
        private const int BufferSize = 8192;

        // Ключ и IV централизованы и не генерируются при каждом сохранении,
        // как того требует задание. В реальном приложении их стоило бы
        // хранить отдельно от исходного кода (переменные окружения, secure storage),
        // но для учебного проекта фиксированных значений достаточно.
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("A1B2C3D4E5F60718293A4B5C6D7E8F90");
        private static readonly byte[] AesIV = Encoding.UTF8.GetBytes("0F1E2D3C4B5A6978");

        private readonly string _dataDir;

        public FileManager(string dataDir = DataDir)
        {
            _dataDir = dataDir;
            EnsureDataDirectory();
        }

        private void EnsureDataDirectory()
        {
            if (!Directory.Exists(_dataDir))
                Directory.CreateDirectory(_dataDir);
        }

        // ---------- IDataStorage: профили ----------

        public void SaveProfiles(IEnumerable<Profile> profiles)
        {
            string filePath = Path.Combine(_dataDir, ProfilesFileName);

            var lines = profiles.Select(p =>
                $"{p.Id};{Escape(p.Login)};{Escape(p.Password)};{Escape(p.FirstName)};{Escape(p.LastName)};{p.BirthYear}");

            WriteEncryptedLines(filePath, lines);
        }

        public IEnumerable<Profile> LoadProfiles()
        {
            string filePath = Path.Combine(_dataDir, ProfilesFileName);

            if (!File.Exists(filePath))
                return new List<Profile>();

            var profiles = new List<Profile>();

            foreach (var line in ReadDecryptedLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = SplitLine(line);
                if (parts.Count != 6)
                    throw new DataCorruptedException($"Некорректная строка профиля: ожидалось 6 полей, получено {parts.Count}.");

                if (!Guid.TryParse(parts[0], out var id))
                    throw new DataCorruptedException($"Некорректный идентификатор профиля: '{parts[0]}'.");

                if (!int.TryParse(parts[5], out var birthYear))
                    throw new DataCorruptedException($"Некорректный год рождения профиля: '{parts[5]}'.");

                profiles.Add(new Profile
                {
                    Id = id,
                    Login = Unescape(parts[1]),
                    Password = Unescape(parts[2]),
                    FirstName = Unescape(parts[3]),
                    LastName = Unescape(parts[4]),
                    BirthYear = birthYear
                });
            }

            return profiles;
        }

        // ---------- IDataStorage: задачи ----------

        public void SaveTodos(Guid userId, IEnumerable<TodoItem> todos)
        {
            string filePath = GetTodoFilePath(userId);

            var lines = todos.Select(item =>
                $"{Escape(item.Text)};{item.Status};{item.LastUpdate:yyyy-MM-ddTHH:mm:ss}");

            WriteEncryptedLines(filePath, lines);
        }

        public IEnumerable<TodoItem> LoadTodos(Guid userId)
        {
            string filePath = GetTodoFilePath(userId);

            if (!File.Exists(filePath))
                return new List<TodoItem>();

            var items = new List<TodoItem>();

            foreach (var line in ReadDecryptedLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = SplitLine(line);
                if (parts.Count != 3)
                    throw new DataCorruptedException($"Некорректная строка задачи: ожидалось 3 поля, получено {parts.Count}.");

                string text = Unescape(parts[0]);

                if (!Enum.TryParse<TodoStatus>(parts[1], out var status))
                    throw new DataCorruptedException($"Некорректный статус задачи: '{parts[1]}'.");

                if (!DateTime.TryParse(parts[2], out var lastUpdate))
                    throw new DataCorruptedException($"Некорректная дата задачи: '{parts[2]}'.");

                items.Add(new TodoItem(text) { Status = status, LastUpdate = lastUpdate });
            }

            return items;
        }

        public string GetTodoFilePath(Guid userId)
        {
            return Path.Combine(_dataDir, $"todos_{userId}.dat");
        }

        // ---------- Запись/чтение через цепочку потоков с шифрованием ----------

        /// <summary>
        /// Записывает строки в файл через цепочку:
        /// FileStream → BufferedStream → CryptoStream(Encrypt) → StreamWriter.
        /// </summary>
        private void WriteEncryptedLines(string filePath, IEnumerable<string> lines)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = AesKey;
                aes.IV = AesIV;

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);
                using var bufferedStream = new BufferedStream(fileStream, BufferSize);
                using var cryptoStream = new CryptoStream(bufferedStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                using var writer = new StreamWriter(cryptoStream, Encoding.UTF8);

                foreach (var line in lines)
                    writer.WriteLine(line);

                // Явный Flush гарантирует, что все данные (включая финальный блок
                // шифрования) попадут в файл до выхода из using-цепочки.
                writer.Flush();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new DataAccessException($"Нет доступа к файлу '{filePath}'.", ex);
            }
            catch (IOException ex)
            {
                throw new DataAccessException($"Ошибка доступа к файлу '{filePath}'. Возможно, он используется другим процессом.", ex);
            }
        }

        /// <summary>
        /// Читает строки из файла через цепочку:
        /// FileStream → BufferedStream → CryptoStream(Decrypt) → StreamReader.
        /// </summary>
        private IEnumerable<string> ReadDecryptedLines(string filePath)
        {
            var lines = new List<string>();

            try
            {
                using var aes = Aes.Create();
                aes.Key = AesKey;
                aes.IV = AesIV;

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
                using var bufferedStream = new BufferedStream(fileStream, BufferSize);
                using var cryptoStream = new CryptoStream(bufferedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream, Encoding.UTF8);

                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new DataAccessException($"Нет доступа к файлу '{filePath}'.", ex);
            }
            catch (IOException ex)
            {
                throw new DataAccessException($"Ошибка доступа к файлу '{filePath}'. Возможно, он используется другим процессом.", ex);
            }
            catch (CryptographicException ex)
            {
                throw new DataCorruptedException($"Не удалось расшифровать файл '{filePath}'. Файл повреждён или ключ шифрования не совпадает.", ex);
            }

            return lines;
        }

        // ---------- Экранирование разделителя ';' ----------

        private static string Escape(string text)
        {
            if (text == null)
                return string.Empty;

            return "\"" + text.Replace("\"", "\"\"").Replace("\n", "\\n") + "\"";
        }

        private static string Unescape(string text)
        {
            return text.Trim('"').Replace("\\n", "\n").Replace("\"\"", "\"");
        }

        private static List<string> SplitLine(string line)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (c == ';' && !inQuotes)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            parts.Add(current.ToString());
            return parts;
        }
    }
}
