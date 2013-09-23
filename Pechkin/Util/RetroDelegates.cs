using System;

namespace Pechkin.Util
{
    internal delegate TResult Func<TResult>();
    internal delegate TResult Func<T, TResult>(T t);
}
