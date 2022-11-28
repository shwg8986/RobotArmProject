using System.Collections.Generic;
using System.Linq;
namespace SoraConf
{
    
    [System.Serializable]
    public enum ErrorCode
    {
        NOT_SET = 0,
        CLOSE_SUCCEEDED = 1,
        CLOSE_FAILED = 2,
        INTERNAL_ERROR = 3,
        INVALID_PARAMETER = 4,
        WEBSOCKET_HANDSHAKE_FAILED = 5,
        WEBSOCKET_ONCLOSE = 6,
        WEBSOCKET_ONERROR = 7,
        PEER_CONNECTION_STATE_FAILED = 8,
        ICE_FAILED = 9,
    }
    
}
