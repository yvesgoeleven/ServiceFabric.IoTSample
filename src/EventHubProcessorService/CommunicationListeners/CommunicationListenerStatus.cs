namespace EventHubProcessorService
{
    public enum CommunicationListenerStatus
    {
        Closed,
        Opening,
        Opened,
        Closing,
        Aborting,
        Aborted
    }
}