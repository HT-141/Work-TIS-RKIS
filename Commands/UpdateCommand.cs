using System;
using TodoApp.Exceptions;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.Commands
{
	public class UpdateCommand : IUndoableCommand
	{
		private int _index;
		private string _newText;
		private string _oldText;
		private TodoList _todos;

		public UpdateCommand(int index, string newText)
		{
			_index = index;
			_newText = newText;
		}

		public void Execute()
		{
			_todos = AppInfo.GetCurrentTodoList();
			if (_todos == null)
				throw new AuthenticationException("Вы не авторизованы. Войдите в профиль, чтобы работать с задачами.");

			if (string.IsNullOrWhiteSpace(_newText))
				throw new InvalidArgumentException("Новый текст задачи не может быть пустым.");

			if (_index < 0 || _index >= _todos.Count)
				throw new TaskNotFoundException(_index);

			var item = _todos[_index];

			_oldText = item.Text;
			_todos.UpdateItem(_index, _newText);
			// Событие OnTodoUpdated будет вызвано автоматически в TodoList.UpdateItem()

			Console.WriteLine($"Задача обновлена.");
		}

		public void Unexecute()
		{
			_todos = AppInfo.GetCurrentTodoList();
			if (_todos == null || _oldText == null) return;

			_todos.UpdateItem(_index, _oldText);
			// Событие OnTodoUpdated будет вызвано автоматически в TodoList.UpdateItem()

			Console.WriteLine("Отменено обновление задачи");
		}
	}
}
