using System;

namespace ATLab.Services
{
    /// <summary>Runs an action when disposed, for scoped state changes with using.</summary>
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