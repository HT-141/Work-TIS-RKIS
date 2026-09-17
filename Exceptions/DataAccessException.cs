using System;

namespace TodoApp.Exceptions
{
    /// <summary>
    /// Выбрасывается при ошибке доступа к файлам хранилища данных:
    /// файл недоступен, нет прав, диск занят другим процессом и т.д.
    /// </summary>
    public class DataAccessException : Exception
    {
        public DataAccessException(string message) : base(message) { }
        public DataAccessException(string message, Exception inner) : base(message, inner) { }
    }
}
