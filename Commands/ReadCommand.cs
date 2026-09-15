using System;
using TodoApp.Exceptions;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.Commands
{
	public class ReadCommand : ICommand
	{
		private int _index;

		public ReadCommand(int index)
		{
			_index = index;
		}

		public void Execute()
		{
			var todos = AppInfo.GetCurrentTodoList();
			if (todos == null)
				throw new AuthenticationException("Вы не авторизованы. Войдите в профиль, чтобы работать с задачами.");

			if (_index < 0 || _index >= todos.Count)
				throw new TaskNotFoundException(_index);

			var item = todos[_index];

			Console.WriteLine(item.GetFullInfo());
		}
	}
}
