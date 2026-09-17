using System;

namespace TodoApp.Exceptions
{
    /// <summary>
    /// Выбрасывается, когда задача с указанным индексом не найдена в списке.
    /// </summary>
    public class TaskNotFoundException : Exception
    {
        public int Index { get; }
        public TaskNotFoundException(int index)
            : base($"Задача с индексом {index} не найдена.")
        {
            Index = index;
        }

        public TaskNotFoundException(string message) : base(message) {}
    }
}
