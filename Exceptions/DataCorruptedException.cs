using System;

namespace TodoApp.Exceptions
{
    /// <summary>
    /// Выбрасывается, когда данные не удалось расшифровать или разобрать:
    /// повреждённый файл, неверный ключ шифрования, некорректный формат строки.
    /// </summary>
    public class DataCorruptedException : Exception
    {
        public DataCorruptedException(string message) : base(message) { }
        public DataCorruptedException(string message, Exception inner) : base(message, inner) { }
    }
}
