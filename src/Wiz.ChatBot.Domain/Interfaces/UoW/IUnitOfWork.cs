using System;

namespace Wiz.ChatBot.Domain.Interfaces.UoW
{
    public interface IUnitOfWork : IDisposable
    {
        int Commit();
        void BeginTransaction();
        void BeginCommit();
        void BeginRollback();
    }
}
