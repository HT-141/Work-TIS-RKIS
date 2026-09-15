using System;
using TodoApp.Exceptions;
using TodoApp.Services;

namespace TodoApp.Commands
{
    public class RedoCommand : ICommand
    {
        public void Execute()
        {
            if (AppInfo.RedoStack.Count == 0)
                throw new InvalidCommandException("Нечего повторять: стек повторов пуст.");

            var command = AppInfo.RedoStack.Pop();
            command.Execute();
            AppInfo.UndoStack.Push(command);
        }
    }
}
