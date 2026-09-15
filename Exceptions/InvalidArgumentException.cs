using System;

namespace TodoApp.Exceptions
{
    /// <summary>
    /// Выбрасывается при некорректном аргументе команды:
    /// неверный формат числа, даты, неизвестный флаг и т.д.
    /// </summary>
    public class InvalidArgumentException : Exception
    {
        public InvalidArgumentException(string message) : base(message) { }
    }
}
