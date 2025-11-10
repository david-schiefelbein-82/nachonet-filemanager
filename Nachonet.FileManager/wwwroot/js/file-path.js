
// convert a full fileId to filename only
// eg /path/to/file.txt -> file.txt
// eg /path/to/directory/ -> directory
function fileIdToFileName(id) {

    if (id.endsWith('/')) {
        id = id.substring(0, id.length - 1);
    }

    index = id.lastIndexOf('/');
    if (index >= 0) {
        return id.substring(index + 1);
    }

    return id;
}

// validate if a filename is valid
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

// returns the parent of a file or folder
function getParentFileId(id) {
    if (id.endsWith("/")) {
        id = id.substring(0, id.length - 1);
    }

    var idx = id.lastIndexOf("/");
    if (idx >= 0)
        return id.substring(0, idx + 1);

    return "/";
}

// returns if a fileId is a directory (not a file)
function isDirectory(id) {
    return id.endsWith("/");
}

// returns if a fileId is a file (not a directory)
function isFile(id) {
    return !id.endsWith("/");
}