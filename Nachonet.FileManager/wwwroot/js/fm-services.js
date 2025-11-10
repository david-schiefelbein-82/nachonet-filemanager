function createServiceManager(urlInfo) {

    var _refreshUrl = urlInfo['refresh-url'];
    var _startUrl = urlInfo['start-url'];
    var _stopUrl = urlInfo['stop-url'];

    function _fmStartService(serviceName) {
        return $.ajax({
            type: "POST",
            url: _startUrl,
            data: { "serviceName": serviceName },
            dataType: "json",
        });
    }

    function _fmStopService(serviceName) {
        return $.ajax({
            type: "POST",
            url: _stopUrl,
            data: { "serviceName": serviceName },
            dataType: "json",
        });
    }

    function _refreshServices() {
        return $.ajax({
            type: "GET",
            url: _refreshUrl,
            contentType: 'application/json',
            dataType: "json",
        });
    }

    return {
        "startService": _fmStartService,
        "stopService": _fmStopService,
        "refresh": _refreshServices,
    };
}