function createTextEditor(editor, urlInfo) {
    var _editor = editor;
    var _contentsUrl = urlInfo['file-contents'];
    var _fileId = null;
    var _isReadOnly = true;

    var loadEditorFile = async function(fileId, forceReadOnly) {
        var result = $.ajax({
            type: "GET",
            url: _contentsUrl,
            data: { "fileId": fileId },
            dataType: "json",
        });
        return result.then(
            function (data, textStatus, jqXHR) {
                var isReadOnly = data.isReadOnly || forceReadOnly;
                var text = data.text;
                var syntax = data.syntax;
                _isReadOnly = isReadOnly;
                _fileId = fileId;
                _editor.session.setMode("ace/mode/" + syntax);
                _editor.setReadOnly(isReadOnly);
                _editor.setValue(text, 1);
            }
        );

    }

    var saveEditorFile = async function(fileId) {
        var content = _editor.getValue();
        return $.ajax({
            type: "POST",
            url: _contentsUrl,
            data: JSON.stringify({ "fileId": fileId, "content": content }),
            contentType: 'application/json',
            dataType: "text",
        });
    }

    return {
        "_contentsUrl": _contentsUrl,
        "getFileId": function () {
            return _fileId;
        },
        "getFileId": function () {
            return _fileId;
        },
        "isReadOnly": function () {
            return _isReadOnly;
        },
        "loadEditorFile": loadEditorFile,
        "saveEditorFile": saveEditorFile
    };
}