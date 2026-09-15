using System;

namespace TodoApp.Exceptions
{
    /// <summary>
    /// Выбрасывается при попытке зарегистрировать профиль
    /// с логином, который уже занят.
    /// </summary>
    public class DuplicateLoginException : Exception
    {
        public string Login { get; }

        public DuplicateLoginException(string login)
            : base($"Логин '{login}' уже занят. Выберите другой.")
        {
            Login = login;
        }
    }
}
