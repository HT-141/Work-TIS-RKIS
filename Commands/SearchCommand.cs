using System;
using System.Linq;
using TodoApp.Exceptions;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.Commands
{
    public class SearchCommand : ICommand
    {
        private readonly string _contains;
        private readonly string _startsWith;
        private readonly string _endsWith;
        private readonly TodoStatus? _status;
        private readonly DateTime? _from;
        private readonly DateTime? _to;
        private readonly string _sortBy;
        private readonly bool _desc;
        private readonly int? _top;

        public SearchCommand(
            string contains = null,
            string startsWith = null,
            string endsWith = null,
            TodoStatus? status = null,
            DateTime? from = null,
            DateTime? to = null,
            string sortBy = null,
            bool desc = false,
            int? top = null)
        {
            _contains = contains;
            _startsWith = startsWith;
            _endsWith = endsWith;
            _status = status;
            _from = from;
            _to = to;
            _sortBy = sortBy;
            _desc = desc;
            _top = top;
        }

        public void Execute()
        {
            var todoList = AppInfo.GetCurrentTodoList();
            if (todoList == null)
                throw new AuthenticationException("Вы не авторизованы. Войдите в профиль, чтобы работать с задачами.");

            if (todoList.Count == 0)
            {
                Console.WriteLine("Список задач пуст.");
                return;
            }

            var query = todoList.GetAll()
                .Select((item, index) => new { Item = item, Index = index })
                .AsEnumerable();

            if (!string.IsNullOrEmpty(_contains))
                query = query.Where(x => x.Item.Text.Contains(_contains, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(_startsWith))
                query = query.Where(x => x.Item.Text.StartsWith(_startsWith, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(_endsWith))
                query = query.Where(x => x.Item.Text.EndsWith(_endsWith, StringComparison.OrdinalIgnoreCase));

            if (_status.HasValue)
                query = query.Where(x => x.Item.Status == _status.Value);

            if (_from.HasValue)
                query = query.Where(x => x.Item.LastUpdate >= _from.Value);

            if (_to.HasValue)
                query = query.Where(x => x.Item.LastUpdate <= _to.Value);

            switch (_sortBy)
            {
                case "text":
                    query = _desc
                        ? query.OrderByDescending(x => x.Item.Text).ThenBy(x => x.Index)
                        : query.OrderBy(x => x.Item.Text).ThenBy(x => x.Index);
                    break;
                case "date":
                    query = _desc
                        ? query.OrderByDescending(x => x.Item.LastUpdate).ThenBy(x => x.Index)
                        : query.OrderBy(x => x.Item.LastUpdate).ThenBy(x => x.Index);
                    break;
            }

            if (_top.HasValue)
                query = query.Take(_top.Value);

            var result = query.ToList();

            if (result.Count == 0)
            {
                Console.WriteLine("Ничего не найдено");
                return;
            }

            PrintTable(result.Select(x => (x.Index, x.Item)).ToList());
        }

        private void PrintTable(System.Collections.Generic.List<(int Index, TodoItem Item)> rows)
        {
            const int indexWidth = 5;
            const int textWidth = 33;
            const int statusWidth = 15;
            const int dateWidth = 20;

            string BuildLine(char left, char mid, char right, char fill)
            {
                var widths = new[] { indexWidth, textWidth, statusWidth, dateWidth };
                var sb = new System.Text.StringBuilder();
                sb.Append(left);
                for (int i = 0; i < widths.Length; i++)
                {
                    sb.Append(new string(fill, widths[i] + 2));
                    sb.Append(i < widths.Length - 1 ? mid : right);
                }
                return sb.ToString();
            }

            var table = new System.Text.StringBuilder();
            table.AppendLine(BuildLine('╔', '╦', '╗', '═'));
            table.Append('║');
            table.Append($" {"INDEX".PadRight(indexWidth)} ║");
            table.Append($" {"TEXT".PadRight(textWidth)} ║");
            table.Append($" {"STATUS".PadRight(statusWidth)} ║");
            table.Append($" {"LASTUPDATE".PadRight(dateWidth)} ║");
            table.AppendLine();
            table.AppendLine(BuildLine('╠', '╬', '╣', '═'));

            for (int i = 0; i < rows.Count; i++)
            {
                var (index, item) = rows[i];
                table.Append('║');
                table.Append($" {index.ToString().PadRight(indexWidth)} ║");
                table.Append($" {item.GetShortInfo().PadRight(textWidth)} ║");
                table.Append($" {item.Status.ToString().PadRight(statusWidth)} ║");
                table.Append($" {item.LastUpdate.ToString("yyyy-MM-dd HH:mm").PadRight(dateWidth)} ║");
                table.AppendLine();

                if (i < rows.Count - 1)
                    table.AppendLine(BuildLine('╠', '╬', '╣', '═'));
            }

            table.AppendLine(BuildLine('╚', '╩', '╝', '═'));
            Console.WriteLine(table.ToString());
        }
    }
}