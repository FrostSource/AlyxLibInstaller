namespace AlyxLib.Exceptions;
public class RemoteConnectionException : Exception
{
    public RemoteConnectionException() { }
    public RemoteConnectionException(string message) : base(message) { }
    public RemoteConnectionException(string message, Exception inner) : base(message, inner) { }
}
