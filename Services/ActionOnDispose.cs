using System;

namespace ATLab.Services
{
    public sealed class ActionOnDispose : IDisposable
    {
        private readonly Action _action;

        public ActionOnDispose(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}