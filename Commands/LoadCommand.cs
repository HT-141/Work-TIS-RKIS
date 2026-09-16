using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TodoApp.Exceptions;

namespace TodoApp.Commands
{
    public class LoadCommand : ICommand
    {
        private const int BarLength = 20; // количество делений прогресс-бара

        private static readonly object _consoleLock = new object();
        private static readonly Random _random = new Random();

        private readonly int _downloadsCount;
        private readonly int _size;

        public LoadCommand(int downloadsCount, int size)
        {
            _downloadsCount = downloadsCount;
            _size = size;
        }

        public void Execute()
        {
            RunAsync().Wait();
        }

        private async Task RunAsync()
        {
            // Резервируем строки под прогресс-бары
            for (int i = 0; i < _downloadsCount; i++)
                Console.WriteLine();

            // Берём стартовую строку ПОСЛЕ вывода пустых строк: если вывод дошёл до низа
            // окна консоли, буфер автоматически скроллится, и позиция "уезжает" —
            // считать startRow нужно относительно уже прокрученного экрана.
            int startRow = Console.CursorTop - _downloadsCount;
            startRow = Math.Max(0, startRow);

            var tasks = new List<Task>();
            for (int i = 0; i < _downloadsCount; i++)
            {
                int index = i;
                int row = startRow + index;
                tasks.Add(DownloadAsync(index, row));
            }

            await Task.WhenAll(tasks);

            // Переводим курсор под все прогресс-бары перед финальным сообщением
            SafeSetCursorPosition(0, startRow + _downloadsCount);
            Console.WriteLine("Все загрузки завершены.");
        }

        private async Task DownloadAsync(int index, int row)
        {
            for (int progress = 0; progress <= _size; progress++)
            {
                int percent = (int)((double)progress / _size * 100);
                DrawProgressBar(index, row, percent);

                if (progress < _size)
                    await Task.Delay(_random.Next(10, 60));
            }
        }

        private void DrawProgressBar(int index, int row, int percent)
        {
            int filled = percent * BarLength / 100;
            var bar = new StringBuilder();
            bar.Append($"Загрузка {index + 1}: [");
            bar.Append('#', filled);
            bar.Append('-', BarLength - filled);
            bar.Append($"] {percent}%");

            lock (_consoleLock)
            {
                SafeSetCursorPosition(0, row);
                string line = bar.ToString();
                try
                {
                    line = line.PadRight(Console.WindowWidth - 1);
                }
                catch (IOException) { /* вывод перенаправлен, ширина недоступна — печатаем без паддинга */ }
                Console.Write(line);
            }
        }

        private static void SafeSetCursorPosition(int left, int top)
        {
            try
            {
                // BufferHeight — реальная высота буфера консоли; если строка вышла за него
                // (например, из-за автоскролла между несколькими запусками команды),
                // ограничиваем позицию последней доступной строкой, а не бросаем исключение.
                int maxTop = Console.BufferHeight - 1;
                int safeTop = Math.Min(Math.Max(0, top), maxTop);
                Console.SetCursorPosition(left, safeTop);
            }
            catch (IOException) { /* вывод перенаправлен — позиционирование недоступно */ }
            catch (ArgumentOutOfRangeException) { /* на всякий случай подстрахуемся и здесь */ }
        }
    }
}
