using System;
using System.Linq;
using TodoApp.Commands;
using TodoApp.Exceptions;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp
{
    class Program
    {
        // FileManager теперь обычный (не статический) класс, реализующий IDataStorage.
        // Program хранит единственный экземпляр и передаёт ему только те данные,
        // которые нужно сохранить/загрузить — сам FileManager ничего не знает про AppInfo.
        private static readonly IDataStorage _storage = new FileManager();

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Clear();

            AppInfo.Profiles = _storage.LoadProfiles().ToList();

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

            var profile = AppInfo.Profiles.FirstOrDefault(p => p.Login == login && p.Password == password);

            if (profile == null)
                throw new AuthenticationException("Неверный логин или пароль.");

            AppInfo.CurrentProfile = profile;
            AppInfo.UserTodos[profile.Id] = new TodoList();

            foreach (var item in _storage.LoadTodos(profile.Id))
                AppInfo.UserTodos[profile.Id].Add(item);

            SubscribeToTodoEvents(profile.Id, AppInfo.UserTodos[profile.Id]);

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
            _storage.SaveProfiles(AppInfo.Profiles);

            AppInfo.CurrentProfile = profile;
            AppInfo.UserTodos[profile.Id] = new TodoList();
            _storage.SaveTodos(profile.Id, AppInfo.UserTodos[profile.Id].GetAll());

            SubscribeToTodoEvents(profile.Id, AppInfo.UserTodos[profile.Id]);

            AppInfo.ClearUndoRedo();
            return true;
        }

        // Program явно знает, для какого userId нужно сохранять список задач —
        // поэтому FileManager может остаться "глупым" и не заглядывать в AppInfo.
        private static void SubscribeToTodoEvents(Guid userId, TodoList todoList)
        {
            void SaveCurrentTodos(TodoItem _) => _storage.SaveTodos(userId, todoList.GetAll());

            todoList.OnTodoAdded += SaveCurrentTodos;
            todoList.OnTodoDeleted += SaveCurrentTodos;
            todoList.OnTodoUpdated += SaveCurrentTodos;
            todoList.OnStatusChanged += SaveCurrentTodos;
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
                catch (DataAccessException ex)
                {
                    Console.WriteLine($"Ошибка доступа к данным: {ex.Message}");
                }
                catch (DataCorruptedException ex)
                {
                    Console.WriteLine($"Ошибка данных: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
                }
            }
        }
    }
}
