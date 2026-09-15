using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TodoApp.Commands;
using TodoApp.Exceptions;
using TodoApp.Models;

namespace TodoApp.Services
{
    public static class CommandParser
    {
        private static Dictionary<string, Func<string[], ICommand>> _commandHandlers;

        static CommandParser()
        {
            InitializeHandlers();
        }

        private static void InitializeHandlers()
        {
            _commandHandlers = new Dictionary<string, Func<string[], ICommand>>
            {
                ["help"] = args => new HelpCommand(),
                ["profile"] = args => ParseProfileCommand(args),
                ["add"] = args => ParseAddCommand(args),
                ["view"] = args => ParseViewCommand(args),
                ["read"] = args => ParseReadCommand(args),
                ["status"] = args => ParseStatusCommand(args),
                ["update"] = args => ParseUpdateCommand(args),
                ["delete"] = args => ParseDeleteCommand(args),
                ["undo"] = args => new UndoCommand(),
                ["redo"] = args => new RedoCommand(),
                ["search"] = args => ParseSearchCommand(args),
            };
        }

        public static ICommand Parse(string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString))
            {
                return new HelpCommand();
            }

            var parts = SplitCommand(inputString);
            if (parts.Length == 0)
                return new HelpCommand();

            string command = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            if (!_commandHandlers.ContainsKey(command))
                throw new InvalidCommandException($"Неизвестная команда: '{command}'. Введите 'help' для справки.");

            return _commandHandlers[command](args);
        }

        private static ICommand ParseProfileCommand(string[] args)
        {
            bool logout = args.Any(a => a == "-o" || a == "--out");
            return new ProfileCommand(logout);
        }

        private static ICommand ParseAddCommand(string[] args)
        {
            bool isMultiline = args.Any(a => a == "-m" || a == "--multiline");

            if (isMultiline)
            {
                return new AddCommand("", true);
            }

            string text = string.Join(" ", args);
            text = text.Trim('"');

            return new AddCommand(text, false);
        }

        private static ICommand ParseViewCommand(string[] args)
        {
            bool showIndex = args.Any(a => a == "-i" || a == "--index");
            bool showStatus = args.Any(a => a == "-s" || a == "--status");
            bool showDate = args.Any(a => a == "-d" || a == "--update-date");
            bool showAll = args.Any(a => a == "-a" || a == "--all");

            if (showAll)
                return new ViewCommand(true, true, true);

            return new ViewCommand(showIndex, showStatus, showDate);
        }

        private static ICommand ParseReadCommand(string[] args)
        {
            if (args.Length == 0)
                throw new InvalidArgumentException("Используйте: read <индекс>");

            if (!int.TryParse(args[0], out int index))
                throw new InvalidArgumentException($"Индекс должен быть числом. Получено: '{args[0]}'");

            return new ReadCommand(index);
        }

        private static ICommand ParseStatusCommand(string[] args)
        {
            if (args.Length < 2)
                throw new InvalidArgumentException("Используйте: status <индекс> <статус>");

            if (!int.TryParse(args[0], out int index))
                throw new InvalidArgumentException($"Индекс должен быть числом. Получено: '{args[0]}'");

            string statusStr = args[1].ToLower();
            if (!Enum.TryParse<TodoStatus>(statusStr, ignoreCase: true, out var status))
                throw new InvalidArgumentException(
                    $"Неизвестный статус: '{args[1]}'. Доступные: NotStarted, InProgress, Completed, Postponed, Failed");

            return new StatusCommand(index, status);
        }

        private static ICommand ParseUpdateCommand(string[] args)
        {
            if (args.Length < 2)
                throw new InvalidArgumentException("Используйте: update <индекс> \"новый текст\"");

            if (!int.TryParse(args[0], out int index))
                throw new InvalidArgumentException($"Индекс должен быть числом. Получено: '{args[0]}'");

            string newText = string.Join(" ", args.Skip(1)).Trim('"');
            return new UpdateCommand(index, newText);
        }

        private static ICommand ParseDeleteCommand(string[] args)
        {
            if (args.Length == 0)
                throw new InvalidArgumentException("Используйте: delete <индекс>");

            if (!int.TryParse(args[0], out int index))
                throw new InvalidArgumentException($"Индекс должен быть числом. Получено: '{args[0]}'");

            return new DeleteCommand(index);
        }

        private static ICommand ParseSearchCommand(string[] args)
        {
            string contains = null, startsWith = null, endsWith = null, sortBy = null;
            TodoStatus? status = null;
            DateTime? from = null, to = null;
            bool desc = false;
            int? top = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--contains":
                        if (i + 1 >= args.Length)
                            throw new InvalidArgumentException("Флаг --contains требует значение.");
                        contains = args[++i];
                        break;

                    case "--starts-with":
                        if (i + 1 >= args.Length)
                            throw new InvalidArgumentException("Флаг --starts-with требует значение.");
                        startsWith = args[++i];
                        break;

                    case "--ends-with":
                        if (i + 1 >= args.Length)
                            throw new InvalidArgumentException("Флаг --ends-with требует значение.");
                        endsWith = args[++i];
                        break;

                    case "--status":
                        if (i + 1 >= args.Length)
                            throw new InvalidArgumentException("Флаг --status требует значение.");

                        if (!Enum.TryParse<TodoStatus>(args[++i], ignoreCase: true, out var parsedStatus))
                            throw new InvalidArgumentException(
                                $"Неизвестный статус: '{args[i]}'. Доступные: NotStarted, InProgress, Completed, Postponed, Failed");

                        status = parsedStatus;
                        break;

                    case "--from":
                        if (i + 1 >= args.Length)
                            throw new InvalidArgumentException("Флаг --from требует значение.");

                        if (!DateTime.TryParse(args[++i], out var parsedFrom))
                            throw new InvalidArgumentException(
                                $"Некорректная дата в --from: '{args[i]}'. Формат: yyyy-MM-dd");

                        from = parsedFrom;
                        break;

                    case "--to":
                        if (i + 1 >= args.Length)
                            throw new InvalidArgumentException("Флаг --to требует значение.");

                        if (!DateTime.TryParse(args[++i], out var parsedTo))
                            throw new InvalidArgumentException(
                                $"Некорректная дата в --to: '{args[i]}'. Формат: yyyy-MM-dd");

                        to = parsedTo;
                        break;

                    case "--sort":
                        if (i + 1 >= args.Length)
                            throw new InvalidArgumentException("Флаг --sort требует значение.");
                        sortBy = args[++i].ToLower();
                        break;

                    case "--desc":
                        desc = true;
                        break;

                    case "--top":
                        if (i + 1 >= args.Length)
                            throw new InvalidArgumentException("Флаг --top требует значение.");

                        if (!int.TryParse(args[++i], out var parsedTop))
                            throw new InvalidArgumentException($"--top требует число, получено: '{args[i]}'");

                        top = parsedTop;
                        break;

                    default:
                        throw new InvalidArgumentException($"Неизвестный флаг: '{args[i]}'");
                }
            }

            return new SearchCommand(contains, startsWith, endsWith, status, from, to, sortBy, desc, top);
        }

        private static string[] SplitCommand(string input)
        {
            var result = new List<string>();
            var regex = new Regex(@"[^\s""]+|""([^""]*)""");
            var matches = regex.Matches(input);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                {
                    result.Add(match.Groups[1].Value);
                }
                else
                {
                    result.Add(match.Value);
                }
            }

            return result.ToArray();
        }
    }
}
