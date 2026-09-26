namespace KMA.Gameplay
{
    public interface IRetryResultPreviewPanel : IResultPreviewPanel
    {
        void ConfigureRetry(int remainingLives);
        void SetActionPending(bool pending, string error);
        bool RetryAvailable { get; }
    }

    public static class ResultPanelActions
    {
        public const string Continue = "Continue";
        public const string Retry = "Retry";
    }
}
