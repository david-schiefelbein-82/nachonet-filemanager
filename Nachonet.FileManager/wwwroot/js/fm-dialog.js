function fmDialogUuid() {
    return ([1e7] + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, c =>
        (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
    );
}

function fileManagerDialog(jqueryObj) {
    var obj = jqueryObj.data("fm-dialog");
    if (typeof obj === "object")
        return obj;

    var _jqueryObj = jqueryObj;

    var showFmDialog = function() {
        $('body').append($('<div/>', {
            'class': 'fm-dialog-background'
        }));

        if (_jqueryObj.hasClass("fm-dialog-top")) {
            _jqueryObj.removeClass("fm-dialog-top-hidden");
            _jqueryObj.addClass("fm-dialog-top-shown");
        } else if (_jqueryObj.hasClass("fm-dialog-left")) {
            _jqueryObj.removeClass("fm-dialog-left-hidden");
            _jqueryObj.addClass("fm-dialog-left-shown");
        } else if (_jqueryObj.hasClass("fm-dialog")) {
            _jqueryObj.removeClass("fm-dialog-hidden");
            _jqueryObj.addClass("fm-dialog-shown");
        }

        _jqueryObj.trigger("shown", []);

        $(".fm-dialog-background").on("click", function () {
            hideFmDialog();
        });
    }

    var hideFmDialog = function() {
        $('.fm-dialog-background').remove();

        if (_jqueryObj.hasClass("fm-dialog-top")) {
            _jqueryObj.addClass("fm-dialog-top-hidden");
            _jqueryObj.removeClass("fm-dialog-top-shown");
        } else if (_jqueryObj.hasClass("fm-dialog-left")) {
            _jqueryObj.addClass("fm-dialog-left-hidden");
            _jqueryObj.removeClass("fm-dialog-left-shown");
        } else if (_jqueryObj.hasClass("fm-dialog")) {
            _jqueryObj.removeClass("fm-dialog-shown");
            _jqueryObj.addClass("fm-dialog-hidden");
        }

        $("body").removeClass("body-noscrollbars");
        _jqueryObj.trigger("hidden", []);
    }

    obj = {
        id: fmDialogUuid(),
        _jqueryObj: _jqueryObj,
        show: showFmDialog,
        hide: hideFmDialog,
    };

    _jqueryObj.data("fm-dialog", obj)
    return obj;
}