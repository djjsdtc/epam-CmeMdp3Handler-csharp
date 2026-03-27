namespace Epam.CmeMdp3Handler.Control
{
    /// <summary>
    /// This class keeps the securities in the in-memory array.
    /// The array is created with initial capacity and increased in case of overflow.
    /// </summary>
    public class InMemoryEventController : IEventController
    {
        private const int InitialLogSize = 64;
        private const int IncrementLogSize = 32;
        private int[] _logContainer = new int[InitialLogSize];

        public void LogSecurity(int securityId)
        {
            for (int i = 0; i < _logContainer.Length; i++)
            {
                int securityInLog = _logContainer[i];
                if (securityInLog > 0)
                {
                    if (securityInLog == securityId) return;
                }
                else
                {
                    _logContainer[i] = securityId;
                    return;
                }
            }
            int idx = _logContainer.Length;
            ResizeLogContainer();
            _logContainer[idx] = securityId;
        }

        public void Commit(EventCommitFunction eventCommitFunction)
        {
            for (int i = 0; i < _logContainer.Length; i++)
            {
                if (_logContainer[i] > 0)
                    eventCommitFunction(_logContainer[i]);
                else
                    break;
            }
            Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < _logContainer.Length; i++)
                _logContainer[i] = 0;
        }

        private void ResizeLogContainer()
        {
            var newContainer = new int[_logContainer.Length + IncrementLogSize];
            for (int i = 0; i < _logContainer.Length; i++)
                newContainer[i] = _logContainer[i];
            _logContainer = newContainer;
        }
    }
}
