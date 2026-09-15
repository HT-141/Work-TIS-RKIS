using System;

namespace TodoApp.Exceptions
{
    /// <summary>
    /// Выбрасывается при ошибке авторизации: неверный логин/пароль,
    /// либо попытка работать с задачами без входа в профиль.
    /// </summary>
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
    }
}
