using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLib.Exceptions;
public class RemoteConnectionException : Exception
{
    public RemoteConnectionException() { }
    public RemoteConnectionException(string message) : base(message) { }
    public RemoteConnectionException(string message, Exception inner) : base(message, inner) { }
}
