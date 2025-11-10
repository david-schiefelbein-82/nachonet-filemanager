
function createLogger(clientDebugging) {
    var _clientDebugging = clientDebugging;

    var logDebug = function(msg) {
        if (_clientDebugging) {
            console.log("[DEBUG] " + msg);
        }
    }

    var obj = {
        ClientDebugging: _clientDebugging,
        debug: logDebug,
    };
    return obj;
}