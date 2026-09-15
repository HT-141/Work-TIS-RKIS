using System;
using TodoApp.Exceptions;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.Commands
{
	public class DeleteCommand : IUndoableCommand
	{
		private int _index;
		private TodoItem _deletedItem;
		private TodoList _todos;

		public DeleteCommand(int index)
		{
			_index = index;
		}

		public void Execute()
		{
			_todos = AppInfo.GetCurrentTodoList();
			if (_todos == null)
				throw new AuthenticationException("Вы не авторизованы. Войдите в профиль, чтобы работать с задачами.");

			if (_index < 0 || _index >= _todos.Count)
				throw new TaskNotFoundException(_index);

			_deletedItem = _todos[_index];

			_todos.Delete(_index);
			// Событие OnTodoDeleted будет вызвано автоматически в TodoList.Delete()

			Console.WriteLine($"Задача удалена: {_deletedItem.Text}");
		}

		public void Unexecute()
		{
			_todos = AppInfo.GetCurrentTodoList();
			if (_todos == null || _deletedItem == null) return;

			_todos.Add(_deletedItem);
			// Событие OnTodoAdded будет вызвано автоматически в TodoList.Add()

			Console.WriteLine("Отменено удаление задачи");
		}
	}
}
