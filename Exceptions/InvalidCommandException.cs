using System;

namespace TodoApp.Exceptions
{
    /// <summary>
    /// Выбрасывается при обращении к неизвестной команде,
    /// либо при попытке выполнить команду в недопустимом состоянии
    /// (например, undo/redo при пустом стеке).
    /// </summary>
    public class InvalidCommandException : Exception
    {
        public InvalidCommandException(string message) : base(message) { }
    }
}
