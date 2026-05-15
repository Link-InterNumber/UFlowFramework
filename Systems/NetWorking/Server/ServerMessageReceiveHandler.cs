namespace PowerCellStudio
{
    public delegate void ServerMessageReceiveHandler<T>(GameSession session, T message);
}