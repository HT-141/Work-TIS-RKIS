
using System;
using System.IO;
using System.Linq;
using TodoApp.Commands;
using TodoApp.Exceptions;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp
{
    class Program
    {
		static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Clear();

            FileManager.EnsureDataDirectory();

            AppInfo.Profiles = FileManager.LoadAllProfiles();

            MainLoop();
        }

        private static bool SelectOrCreateProfile()
        {
            while (true)
            {
                Console.WriteLine("Войти в существующий профиль? [y/n]");
                Console.Write("> ");

                string choice = Console.ReadLine()?.ToLower() ?? "n";

                if (choice == "y")
                {
                    try
                    {
                        return LoginProfile();
                    }
                    catch (AuthenticationException ex)
                    {
                        Console.WriteLine($"Ошибка авторизации: {ex.Message}");
                        return false;
                    }
                    catch (ProfileNotFoundException ex)
                    {
                        Console.WriteLine($"Ошибка: {ex.Message}");
                        return false;
                    }
                }
                else if (choice == "n")
                {
                    try
                    {
                        return CreateProfile();
                    }
                    catch (DuplicateLoginException ex)
                    {
                        Console.WriteLine($"Ошибка регистрации: {ex.Message}");
                        return false;
                    }
                    catch (InvalidArgumentException ex)
                    {
                        Console.WriteLine($"Ошибка ввода: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Пожалуйста, введите 'y' или 'n'");
                }
            }
        }

        private static bool LoginProfile()
        {
            if (AppInfo.Profiles.Count == 0)
                throw new ProfileNotFoundException("Нет сохранённых профилей. Пожалуйста, создайте новый.");

            Console.Write("Логин: ");
            string login = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(login))
                throw new InvalidArgumentException("Логин не может быть пустым.");

            Console.Write("Пароль: ");
            string password = Console.ReadLine() ?? "";

            var profile = FileManager.LoadProfile(login, password);

            if (profile == null)
                throw new AuthenticationException("Неверный логин или пароль.");

            AppInfo.CurrentProfile = profile;

            string todoPath = FileManager.GetTodoFilePath(profile.Id);
            if (File.Exists(todoPath))
            {
                AppInfo.UserTodos[profile.Id] = FileManager.LoadTodos(todoPath);
            }
            else
            {
                AppInfo.UserTodos[profile.Id] = new TodoList();
                FileManager.SaveTodos(AppInfo.UserTodos[profile.Id], todoPath);
            }

            var todoList = AppInfo.UserTodos[profile.Id];
            SubscribeToTodoEvents(todoList);

			AppInfo.ClearUndoRedo();
            return true;
        }

        private static bool CreateProfile()
        {
            Console.Write("Логин: ");
            string login = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(login))
                throw new InvalidArgumentException("Логин не может быть пустым.");

            if (AppInfo.Profiles.Any(p => p.Login == login))
                throw new DuplicateLoginException(login);

            Console.Write("Пароль: ");
            string password = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidArgumentException("Пароль не может быть пустым.");

            Console.Write("Имя: ");
            string firstName = Console.ReadLine() ?? "";

            Console.Write("Фамилия: ");
            string lastName = Console.ReadLine() ?? "";

            Console.Write("Год рождения: ");
            string birthYearInput = Console.ReadLine() ?? "";

            if (!int.TryParse(birthYearInput, out int birthYear))
                throw new InvalidArgumentException($"Неверный формат года: '{birthYearInput}'.");

            int currentYear = DateTime.Now.Year;
            if (birthYear < 1900 || birthYear > currentYear)
                throw new InvalidArgumentException(
                    $"Год рождения должен быть в диапазоне от 1900 до {currentYear}. Получено: {birthYear}.");

            var profile = new Profile(login, password, firstName, lastName, birthYear);
            AppInfo.Profiles.Add(profile);
            FileManager.SaveProfile(profile);

            AppInfo.CurrentProfile = profile;
            AppInfo.UserTodos[profile.Id] = new TodoList();

            string todoPath = FileManager.GetTodoFilePath(profile.Id);
            FileManager.SaveTodos(AppInfo.UserTodos[profile.Id], todoPath);

            var todoList = AppInfo.UserTodos[profile.Id];
            SubscribeToTodoEvents(todoList);

			AppInfo.ClearUndoRedo();
            return true;
        }

        private static void SubscribeToTodoEvents(TodoList todoList)
        {
            todoList.OnTodoAdded += FileManager.SaveTodoList;
            todoList.OnTodoDeleted += FileManager.SaveTodoList;
            todoList.OnTodoUpdated += FileManager.SaveTodoList;
            todoList.OnStatusChanged += FileManager.SaveTodoList;
		}

		private static void MainLoop()
        {
            while (true)
            {
				if (AppInfo.CurrentProfile is null)
				{
					if (!SelectOrCreateProfile())
					{
						continue;
					}

					Console.WriteLine($"\nДобро пожаловать, {AppInfo.CurrentProfile?.FirstName}!\n");
				}

				Console.Write("> ");
                string input = Console.ReadLine() ?? "";

                if (input.ToLower() == "exit")
                {
                    Console.WriteLine("До свидания!");
                    break;
                }

                try
                {
                    ICommand command = CommandParser.Parse(input);
                    command.Execute();

                    if (command is IUndoableCommand undoableCmd)
                    {
                        AppInfo.UndoStack.Push(undoableCmd);
                        AppInfo.RedoStack.Clear();
                    }
                }
                catch (TaskNotFoundException ex)
                {
                    Console.WriteLine($"Ошибка задачи: {ex.Message}");
                }
                catch (AuthenticationException ex)
                {
                    Console.WriteLine($"Ошибка авторизации: {ex.Message}");
                }
                catch (ProfileNotFoundException ex)
                {
                    Console.WriteLine($"Ошибка профиля: {ex.Message}");
                }
                catch (DuplicateLoginException ex)
                {
                    Console.WriteLine($"Ошибка регистрации: {ex.Message}");
                }
                catch (InvalidCommandException ex)
                {
                    Console.WriteLine($"Ошибка команды: {ex.Message}");
                }
                catch (InvalidArgumentException ex)
                {
                    Console.WriteLine($"Ошибка аргумента: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
                }
            }
        }
    }
}
