namespace Epam.CmeMdp3Handler.Control
{
    public interface IEventController
    {
        void LogSecurity(int securityId);
        void Commit(EventCommitFunction eventCommitFunction);
        void Reset();
    }
}
