// TODO: these are all global variables - bad practice.  Move them into a closure
// although I note this js is intertwined with the directory page... it is not a
// reusable module.
var _urlGetFolderContents;
var _urlCutFiles;
var _urlCopyFiles;
var _urlPasteFiles;
var _urlPasteResults;
var _urlDeleteFiles;
var _urlRenameFile;
var _urlNewFolder;
var _urlDownloadFiles;
var _urlFileInfo;
var _urlFileStream;

var _selectedFileId = null;
var _currentFolder;
var _logger;
var _textEditor;
var _menuFileTree;
var _dialogUpload;
var _dialogTextView;
var _dialogImageView;
var _dialogAudioView;
var _dialogVideoView;

function initFileFolder(logger, urlInfo) {
    _logger = logger;
    _urlGetFolderContents = urlInfo["get-folder-contents"];
    _urlCutFiles = urlInfo["cut-files"];
    _urlCopyFiles = urlInfo["copy-files"];
    _urlPasteFiles = urlInfo["paste-files"];
    _urlPasteResults = urlInfo["paste-results"];
    _urlDeleteFiles = urlInfo["delete-files"];
    _urlRenameFile = urlInfo["rename-file"];
    _urlNewFolder = urlInfo["new-folder"];
    _urlDownloadFiles = urlInfo["download-files"];
    _urlFileInfo = urlInfo["file-info"];
    _urlFileStream = urlInfo["file-stream"];
}

function refreshFolder() {
    loadFolderAsync(_currentFolder, "refresh", "");
}

function getCurrentFolderId() {
    return _currentFolder;
}

function onPageLoaded(textEditor) {
    _textEditor = textEditor;
    _menuFileTree = fileManagerDialog($("#panel-tree"));
    _dialogUpload = fileManagerDialog($("#panel-upload"));
    _dialogTextView = fileManagerDialog($("#dialog-text-view"));
    _dialogImageView = fileManagerDialog($("#dialog-image-view"));
    _dialogAudioView = fileManagerDialog($("#dialog-audio-view"));
    _dialogVideoView = fileManagerDialog($("#dialog-video-view"));

    // this sends a request to the server to render the current folder.
    // Since we dont specify a folder the last rendered one is used.  This means if someone 
    // refreshes the page within the existing session the app will not reset their folder to root
    loadFolderAsync("", "doc-ready", "");

    $('#tree-folders').on("changed.jstree", function (e, data) {
        if (data.selected.length > 0)
            loadFolderAsync(data.selected[0], "tree-changed");
    });

    $("#btn-new-folder").on("click", function () {
        createNewFolder();
    });
    $("#btn-cut").on("click", function () {
        cutSelectedFiles();
    });
    $("#btn-copy").on("click", function () {
        copySelectedFiles();
    });
    $("#btn-paste").on("click", function () {
        pasteFiles();
    });
    $("#btn-rename").on("click", function () {
        renameSelectedFile();
    });
    $("#btn-delete").on("click", function () {
        deleteSelectedFiles();
    });
    $("#btn-delete-confirm").on("click", function () {
        deleteConfirmedSelectedFiles();
    });
    $("#btn-select-none").on("click", function () {
        selectNoFiles();
    });
    $("#btn-select-all").on("click", function () {
        selectAllFiles();
    });
    $("#btn-download").on("click", function () {
        downloadSelectedFiles();
    });
    $(".btn-download").on("click", function () {
        downloadFileItems([$("#fileinfo-id").val()]);
    });
    $("#btn-layout-tiles").on("click", function () {
        loadFolderAsync(_currentFolder, "btn-layout-tiles click", "tiles");
    });
    $("#btn-layout-list").on("click", function () {
        loadFolderAsync(_currentFolder, "btn-layout-list click", "list");
    });
    $("#btn-close-panel-tree").on("click", function () {
        _menuFileTree.hide();
    });
    $("#btn-show-panel-tree").on("click", function () {
        _menuFileTree.show();
    });
    $("#btn-fileinfo-view").on("click", function () {
        var fileId = $("#fileinfo-id").val();
        $('#modal-fileinfo').modal('hide');
        viewFile(fileId, "Text", true);
    });
    $("#btn-text-edit-hide").on("click", function () {
        _dialogTextView.hide();
    });
    $("#btn-image-view-hide").on("click", function () {
        _dialogImageView.hide();
    });
    $("#btn-audio-view-hide").on("click", function () {
        _dialogAudioView.hide();
    });
    $("#btn-video-view-hide").on("click", function () {
        _dialogVideoView.hide();
    });
    $("#btn-text-edit-save").on("click", function () {
        _dialogTextView.hide();
        var fileId = $("#fileinfo-id").val();
        console.log("btn-text-edit-save: readonly: " + _textEditor.isReadOnly() + " fileId: " + _textEditor.getFileId());
        $('#modal-fileinfo').modal('hide');
        if (!_textEditor.isReadOnly()) {
            _textEditor.saveEditorFile(fileId)
                .then(
                    function (data, textStatus, jqXHR) {
                        // save success
                    }
                ).catch(function (jqXHR, textStatus, errorThrown) {
                    showError(jqXHR, "save file");
                });
        }
    });

    $('#edit-name').on('input', function (e) {
        var val = $(this).val();
        var err = validateFileName(val.trim());
        if (err != null) {
            $(this).addClass("form-control-warning");
            $("#edit-name-validation").html(err);
            $("#btn-submit-rename").prop('disabled', true);
        } else {
            $(this).removeClass("form-control-warning");
            $("#edit-name-validation").html("");
            $("#btn-submit-rename").prop('disabled', false);
        }
    });

    $("#dialog-audio-view").on('hidden', function () {
        // cancel any playing video
        // $("#video-player")[0].pause();
        $("#audio-player").prop('src', "#");
    });
    $("#dialog-video-view").on('hidden', function () {
        // cancel any playing video
        // $("#video-player")[0].pause();
        $("#video-player").prop('src', "#");
    });

    $("#modal-rename").on('shown.bs.modal', function () {
        var textBox = $(this).find('#edit-name');

        textBox.focus();
        textBox.select();
    });
    $("#form-rename").submit(function () {
        var val = $("#edit-name").val();
        var err = validateFileName(val.trim());
        if (err == null)
            commitRenameSelectedFile();

        return false; // return false to cancel form action
    });
}

function viewFile(fileId, fileType, isReadOnly) {
    if (fileType == "Image") {
        _dialogImageView.show();
        var url = _urlFileStream + "?fileId=" + encodeURIComponent(fileId);
        $("#image-viewer").prop('src', url);
        return true;
    } else if (fileType == "Video") {
        _dialogVideoView.show();
        var url = _urlFileStream + "?fileId=" + encodeURIComponent(fileId);
        $("#video-player").prop('src', url);
        return true;
    } else if (fileType == "Audio") {
        _dialogAudioView.show();
        var url = _urlFileStream + "?fileId=" + encodeURIComponent(fileId);
        $("#audio-player").prop('src', url);
        return true;
    } else if (fileType == "Text") {
        $("#btn-text-edit-save").prop('disabled', true);
        _dialogTextView.show();
        var loadResult = _textEditor.loadEditorFile(fileId, isReadOnly);
        loadResult.then(function () {
            $("#btn-text-edit-hide").html("Close");
            if (!_textEditor.isReadOnly()) {
                $("#label-text-read-only").hide();
                $("#btn-text-edit-save").prop('disabled', false);
            } else {
                $("#label-text-read-only").show();
            }
        })

        return true;
    } else {
        return false;
    }
}

function showError(jqXHR, operation) {
    var error = null;
    if (typeof jqXHR.responseText == 'string')
        error = jqXHR.responseText;

    if (error == null || error.length == 0) {
        if (jqXHR.status == 400)
            error = jqXHR.status + " Bad Request";
        else if (jqXHR.status == 401)
            error = jqXHR.status + " Unauthorized";
        else if (jqXHR.status == 403)
            error = jqXHR.status + " Forbidden";
        else if (jqXHR.status == 404)
            error = jqXHR.status + " Not Found";
        else if (jqXHR.status == 405)
            error = jqXHR.status + " Method Not Allowed";
        else
            error = jqXHR.status;
    }

    showAlert("danger", error);
}

function refreshNodeInTreeView(dataId) {
    var tree = $('#tree-folders').jstree(true);
    var node = dataId == "/" ? "#" : dataId;
    tree.refresh_node(node);
}

function selectNodeInTreeView(dataId, dataParent) {
    var tree = $('#tree-folders').jstree(true);
    var suppressEvent = true;

    if (dataParent == "/") {
        // the parent is the root, but since the treeivew doesn't show the root we need a special case
        tree.deselect_all(true);
        tree.select_node(dataId, suppressEvent, true);
    } else {
        var isOpen = tree.is_open(dataParent);
        if (!isOpen) {
            // parent node is not open - open it first then select it once opened
            tree.open_node(dataParent, function (a, b) {
                tree._open_to(dataParent);
                tree.deselect_all(true);
                tree.select_node(dataId, suppressEvent, true);
            });
        } else {
            // node is already open - select it
            tree._open_to(dataParent);
            tree.deselect_all(true);
            tree.select_node(dataId, suppressEvent, true);
        }
    }
}

/// returns an array of folders in the format:
/// [ { id: "/", name: "/" }, { id: "/path/", name: "path/" } ]
///
function getFolderParts(id) {
    let result = [];
    if (id.length > 0 && id[0] == '/') {
        result.push({ "id": "/", "name": "/" });
        id = id.substring(1);
    }

    let index = 0;
    let posn = 0;
    let itemId = "/";
    while ((index = id.indexOf('/', posn)) >= 0) {
        let name = id.substring(posn, index + 1);
        itemId += name;
        result.push({ "id": itemId, "name": name.substring(0, name.length - 1) });
        posn = index + 1;
    }

    return result;
}

//
// sets the folder path in the top nav menu (not the treeview).
//
function setFolderPath(id) {
    _currentFolder = id;
    let parts = getFolderParts(id);
    var dataId = "";

    var parent = $('<div />');

    var nav = $('<nav />', { "style": "--bs-breadcrumb-divider: '>';", "aria-label": "breadcrumb" });
    parent.append(nav);
    var ol = $('<ol />', { "class": "breadcrumb" });
    nav.append(ol);
    var li = $('<breadcrumb-item />', { "class": "breadcrumb-item" });
    ol.append(li);
    var btn = $('<a />', { "href": "#", "data-id": "/", "class": "folder-nav-link" });
    btn.append("Home");
    li.append(btn);

    // Note: i=0 is the root directory - already processed this
    for (var i = 1; i < parts.length; ++i) {
        let folder = parts[i];
        var li = $('<breadcrumb-item />', { "class": "breadcrumb-item" });
        ol.append(li);
        var btn = $('<a />', { "href": "", "data-id": folder.id, "class": "folder-nav-link" });
        btn.append(folder.name);
        li.append(btn);
    }

    var html = parent.html();

    $("#folder-path").html(html);
    $(".folder-nav-link").on("click", function (event) {
        var fileId = $(this).attr("data-id");
        var parentFileId = getParentFileId(fileId);
        selectNodeInTreeView(fileId, parentFileId)
        loadFolderAsync(fileId, "nav-link", true);
        event.preventDefault();
    });
}

function getSelectedFileIds() {
    var items = [];
    $(".file-chk:checked").each(function () {
        var dataId = $(this).attr("value");
        items.push(dataId);
    });

    return items;
}

function getLastSelectedFileId() {
    var allCheckBoxes = $(".file-chk:checked");
    if (allCheckBoxes.length <= 0)
        return null;

    var value = allCheckBoxes.last().attr("value");
    return value;
}

function selectFiles(firstNodeId, lastNodeId, checked) {
    var items = [];
    var reachedFirst = firstNodeId == null;
    $(".file-chk").each(function () {
        var dataId = $(this).attr("value");
        if (!reachedFirst)
            reachedFirst = firstNodeId == dataId;

        if (reachedFirst) {
            $(this).prop('checked', checked);
            items.push(dataId);
        }

        if (lastNodeId != null && lastNodeId == dataId) {
            return false;
        }
    });

    return items;
}

function createNewFolder() {
    $("#edit-id").val(_currentFolder + "new folder/");
    $("#edit-name").val("");
    $("#edit-name").removeClass("form-control-warning");
    $("#edit-name-validation").html("");
    $("#btn-submit-rename").prop('disabled', true);
    $('#modal-rename').modal('show');
}

function cutSelectedFiles() {
    var items = getSelectedFileIds();

    if (items.length == 0) {
        showAlert("danger", "no items selected");
        return;
    }

    $.ajax({
        type: "POST",
        url: _urlCutFiles,
        data: { "fileIds": items },
        traditional: true,
        dataType: "json",
    }).then(
        function (data, textStatus, jqXHR) {
            if (data.result == "Success") {
                showAlert("success", data.message);
            } else {
                showAlert("danger", data.message);
            }
        }
    ).catch(function (jqXHR, textStatus, errorThrown) {
        showError(jqXHR, "cut");
    });
}

function copySelectedFiles() {
    var items = getSelectedFileIds();

    if (items.length == 0) {
        showAlert("danger", "no items selected");
        return;
    }

    $.ajax({
        type: "POST",
        url: _urlCopyFiles,
        data: { "fileIds": items },
        traditional: true,
        dataType: "json",
    }).then(
        function (data, textStatus, jqXHR) {
            if (data.result == "Success") {
                showAlert("success", data.message);
            } else {
                showAlert("danger", data.message);
            }
        }
    ).catch(function (jqXHR, textStatus, errorThrown) {
        showError(jqXHR, "copy");
    });
}

function renameSelectedFile() {
    var item = _selectedFileId;

    if (item == null) {
        showAlert("danger", "no items selected");
        return;
    }

    $("#edit-id").val(item);
    $("#edit-name").val(fileIdToFileName(item));
    $("#edit-name").removeClass("form-control-warning");
    $("#edit-name-validation").html("");
    $("#btn-submit-rename").prop('disabled', false);
    $('#modal-rename').modal('show');
}

function validateFileName(name) {
    if (name.lastIndexOf('\\') >= 0 ||
        name.lastIndexOf('/') >= 0 ||
        name.lastIndexOf(':') >= 0 ||
        name.lastIndexOf('*') >= 0 ||
        name.lastIndexOf('?') >= 0 ||
        name.lastIndexOf('"') >= 0 ||
        name.lastIndexOf('<') >= 0 ||
        name.lastIndexOf('>') >= 0 ||
        name.lastIndexOf('|') >= 0 ||
        name.lastIndexOf('\t') >= 0 ||
        name.lastIndexOf('\r') >= 0 ||
        name.lastIndexOf('\n') >= 0) {
        return "name cannot contain \\ / : * ? \" < > | \\t \\r \\n";
    } else if (name.length == 0) {
        return "name cannot be empty";
    }

    return null;
}

function commitRenameSelectedFile() {
    var fileId = $("#edit-id").val();
    var name = $("#edit-name").val().trim();

    var err = validateFileName(name);
    if (err != null) {
        showAlert("danger", err);
        return;
    }

    $('#modal-rename').modal('hide');

    $.ajax({
        type: "POST",
        url: _urlRenameFile,
        data: { "fileId": fileId, "newName": name },
        dataType: "json",
    }).then(
        function (data, textStatus, jqXHR) {
            if (data.result == "Success") {
                showAlert("success", data.message);
                refreshNodeInTreeView(_currentFolder);
                loadFolderAsync(_currentFolder, "rename complete");

            } else {
                showAlert("danger", data.message);
            }
        }
    ).catch(function (jqXHR, textStatus, errorThrown) {
        showError(jqXHR, "rename");
    });
}

// select 0 files by unchecking the checkbox in each one
function selectNoFiles() {
    $(".file-chk:checked").each(function () {
        $(this).prop('checked', false);
    });
    _selectedFileId = null;
}

// select all files by checking the checkbox in each one
function selectAllFiles() {
    $(".file-chk").each(function () {
        $(this).prop('checked', true);
    });

    _selectedFileId = getLastSelectedFileId();
}

function deleteSelectedFiles() {
    var items = getSelectedFileIds();

    if (items.length == 0) {
        showAlert("danger", "no items selected");
        return;
    }

    $("#delete-confirmation-prompt").html(items.length == 1 ?
        ("Are you sure you want to delete \"" + fileIdToFileName(items[0]) + "\"?") :
        "Are you sure you want to delete these " + items.length + " items ?");
    $("#modal-delete").modal('show');
}

function deleteConfirmedSelectedFiles() {
    var items = getSelectedFileIds();
    $("#modal-delete").modal('hide');
    if (items.length == 0) {
        showAlert("danger", "no items selected");
        return;
    }

    $.ajax({
        type: "POST",
        url: _urlDeleteFiles,
        data: { "fileIds": items },
        dataType: "json",
    }).then(
        function (data, textStatus, jqXHR) {
            if (data.result == "Success") {
                showAlert("success", data.message);
                refreshNodeInTreeView(_currentFolder);
                loadFolderAsync(_currentFolder, "delete-complete");
            } else {
                showAlert("danger", data.message);
            }
        }
    ).catch(function (jqXHR, textStatus, errorThrown) {
        showError(jqXHR, "delete");
    });
}

function downloadSelectedFiles() {
    var fileIds = getSelectedFileIds();

    if (fileIds.length == 0) {
        showAlert("danger", "no items selected");
        return;
    }

    downloadFileItems(fileIds);
}

// download a list of files
function downloadFileItems(fileIds) {
    var dlurl = _urlDownloadFiles;
    var i;
    for (i = 0; i < fileIds.length; ++i) {
        dlurl += (i == 0) ? "?" : "&";
        dlurl += "fileIds=" + encodeURIComponent(fileIds[i]);
    }

    const a = document.createElement('a');
    a.href = dlurl;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}

async function pasteFiles() {
    $('#modal-paste').modal('show');
    var pasteResult;
    try {
        pasteResult = await $.ajax({
            type: "POST",
            url: _urlPasteFiles,
            data: { "destId": _currentFolder, "overwrite": false },
            traditional: true,
            dataType: "json"
        });
    } catch (jqXHR) {
        showError(jqXHR, "paste");
        return;
    }

    var operationId = pasteResult.operationId;
    $("#modal-paste-title").html(pasteResult.title);
    $("#modal-paste-message").html(pasteResult.message);

    while (true) {
        try {
            var progress = await $.ajax({
                type: "POST",
                url: _urlPasteResults,
                data: { "operationId": operationId, "timeout": 250 },
                dataType: "json"
            });
        }
        catch (jqXHR) {
            showError(jqXHR, "paste progress");
            return;
        }

        if (progress.result != "InProgress") {
            if (progress.result == "Success") {
                $("#modal-paste-message").html("Complete");
                await delay(1500);
            }
            else {
                $("#modal-paste-message").html("<div class=\"text-danger\">" + progress.message + "</div>");
                await delay(2000);
            }

            break;
        }

        $("#modal-paste-message").html(progress.message);
    }

    loadFolderAsync(_currentFolder, "paste complete");
    $('#modal-paste').modal('hide');
}

// when the image for the file is clicked
function handleFileImageClick(src, event) {
    var card = src.closest('.file-card');
    return handleFileInfoClick(card, "image", event);
}

// when the text for the file or folder is clicked
function handleFileLabelClick(src, event) {
    var card = src.closest('.file-card');
    return handleFileInfoClick(card, "label", event);
}

// generic handler for someone clicking on the file or folder
// param card - jquery object for the card element
// param targetName - "label" or "image" or "card" to represent what was clicked
// param event - the event that was fired
function handleFileInfoClick(card, targetName, event) {
    var fileId = decodeURIComponent(card.attr("data-file-id"));
    var fileParent = decodeURIComponent(card.attr("data-file-parent"));

    if (targetName != "label" && (getSelectedFileIds().length > 1 || (_selectedFileId != null && fileId != _selectedFileId))) {
        // already a selection (and it's not this item) - don't download/navigate to the file
        return false;
    } else {
        if (isDirectory(fileId)) {
            selectNodeInTreeView(fileId, fileParent);
            loadFolderAsync(fileId, "file-image complete");
            return true;
        } else {
            // handle in parent
            showFileInfo(fileId);
            return true;
        }
    }
}

function showFileInfo(fileId) {
    $.ajax({
        type: "GET",
        url: _urlFileInfo,
        data: { "fileId": fileId },
        dataType: "json"
    }).then(function (data, textStatus, jqXHR) {
        $("#fileinfo-id").val(data.fileId);
        $("#fileinfo-type").val(data.fileType);
        if (!viewFile(data.fileId, data.fileType, data['read-only'])) {
            _logger.debug("showFileInfo fileId: " + data.fileId + " type: " + data.fileType)
            $("#fileinfo-name").val(data.name);
            $("#fileinfo-created").val(data['created-text']);
            $("#fileinfo-modified").val(data['modified-text']);
            $("#fileinfo-size").val(data['size-text']);
            $('#modal-fileinfo').modal('show');
        }
    }).catch(function (jqXHR, textStatus, errorThrown) {
        showError(jqXHR, "get-fileinfo");
    });
}

function handleFileCardClick(src, event) {
    var chkBox = src.find('.form-check-input');
    var checked = chkBox.is(':checked');
    var fileId = (chkBox.length == 1) ? $(chkBox).val() : null;

    if (event.shiftKey) {
        selectFiles(_selectedFileId, fileId, true);
        _selectedFileId = fileId;
        return;
    }

    var action = !checked;
    src.find('.form-check-input').prop('checked', action);
    if (action) {
        // just selected this node
        _selectedFileId = fileId;
    } else {
        if (_selectedFileId == fileId) {
            // this is the last node selected and we're about to unselect it
            _selectedFileId = getLastSelectedFileId();
        }
        // otherwise this isn't the last node selected and we can ignore it
    }
}

function handleFileCheckboxClick(src, event) {
    var checked = src.is(':checked');
    var fileId = src.val();

    if (checked) {
        // just selected this node
        _selectedFileId = fileId;
    } else {
        if (_selectedFileId == fileId) {
            // this is the last node selected and we're about to unselect it
            _selectedFileId = getLastSelectedFileId();
        }
        // otherwise this isn't the last node selected and we can ignore it
    }
}

function displayLoading(selector) {
    var parent = $('<div />');

    var spinner = $('<div />', { "class": "spinner-border text-primary m-5", "role": "status" });
    parent.append(spinner);

    var span = $('<span />', { "class": "visually-hidden" });
    span.append("Loading...");
    parent.append(span);

    var html = parent.html();
    $(selector).html(html);
}

function loadFolderAsync(folderId, cause, layout) {
    _logger.debug("LoadFolderAsync(folderId: " + folderId +", cause: " + cause + ")");
    // get the folder contents from the server
    displayLoading("#folder-items");
    _selectedFileId = null;
    $.ajax({
        type: "GET",
        url: _urlGetFolderContents,
        data: { "folderId": folderId, "select": false, "layout": layout },
        dataType: "html"
    }).then(
        function (data, textStatus, jqXHR) {
            var renderedFolderId = jqXHR.getResponseHeader("Folder-Id");
            if (typeof (renderedFolderId) === "string") {
                folderId = renderedFolderId;
            }
            setFolderPath(folderId);
            $("#folder-items").html(data);

            $(".file-card").on("click", function (event) {
                handleFileCardClick($(this), event);
            });
            $(".file-card").on("contextmenu", function (event) {
                event.stopPropagation();
                return false;
            });
            $(".file-img").on("click", function (event) {
                if (handleFileImageClick($(this), event))
                    event.stopPropagation();
            });
            $(".file-chk").on("click", function (event) {
                handleFileCheckboxClick($(this), event);
                event.stopPropagation();
            });
            $(".file-label").on("click", function (event) {
                if (handleFileLabelClick($(this), event)) {
                    event.stopPropagation();
                }
            });
        }
    ).catch(function (jqXHR, textStatus, errorThrown) {
        showError(jqXHR, "get-folder");
    });
}