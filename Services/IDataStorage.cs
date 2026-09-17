using System;
using System.Collections.Generic;
using TodoApp.Models;

namespace TodoApp.Services
{
    /// <summary>
    /// Абстракция хранилища данных приложения. Позволяет подменить файловое
    /// хранилище на БД или мок-реализацию для юнит-тестов без изменения
    /// остального кода приложения.
    /// </summary>
    public interface IDataStorage
    {
        void SaveProfiles(IEnumerable<Profile> profiles);
        IEnumerable<Profile> LoadProfiles();

        void SaveTodos(Guid userId, IEnumerable<TodoItem> todos);
        IEnumerable<TodoItem> LoadTodos(Guid userId);
    }
}
