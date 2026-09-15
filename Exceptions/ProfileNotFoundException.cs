using System;

namespace TodoApp.Exceptions
{
    /// <summary>
    /// Выбрасывается, когда профиль пользователя не найден.
    /// </summary>
    public class ProfileNotFoundException : Exception
    {
        public ProfileNotFoundException(string login)
            : base($"Профиль с логином '{login}' не найден.") { }
    }
}
