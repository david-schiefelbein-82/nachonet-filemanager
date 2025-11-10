
var _chunkSize = 1024 * 500; // 500 kb
var reader = new FileReader();
var _urlUploadChunk;
var _urlStartFileUpload;
var _urlFinishFileUpload;

function initFileUpload(urlInfo, chunkSize) {
   _urlUploadChunk = urlInfo["upload-chunk"];
   _urlStartFileUpload = urlInfo["start-file-upload"];
   _urlFinishFileUpload = urlInfo['finish-file-upload'];
   if (chunkSize > 0)
      _chunkSize = chunkSize;
}

async function uploadFile(element, folderId, file, cancel) {
   return uploadFileChunks(element, folderId, file, _chunkSize, cancel);
}

async function sendStartFileUpload(uploadId, folderId, fileName, fileLen, chunks, chunkSize) {
   return await $.ajax({
      type: "POST",
      url: _urlStartFileUpload,
      data: { "uploadId": uploadId, "folderId": folderId, "fileName": fileName, "fileLen": fileLen, "chunks": chunks, "chunkSize": chunkSize },
      dataType: "json",
   });
}

async function sendFinishFileUpload(uploadId, folderId, fileName, fileLen, chunks, chunkSize) {
   return await $.ajax({
      type: "POST",
      url: _urlFinishFileUpload,
      data: { "uploadId": uploadId, "folderId": folderId, "fileName": fileName, "fileLen": fileLen, "chunks": chunks, "chunkSize": chunkSize },
      dataType: "json",
   });
}

function uuidv4() {
   return ([1e7] + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, c =>
      (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
   );
}

async function uploadFileChunks(element, folderId, file, chunkSize, cancel) {
   var offset = 0;
   var chunkId = 0;
   var chunks = Math.ceil(file.size / chunkSize);

   var uploadId = uuidv4();
   try {
      await sendStartFileUpload(uploadId, folderId, file.name, file.size, chunks, chunkSize);
   } catch (jqXHR) {
      element.trigger("uploadError", [{ "filename": file.name, "chunkId": chunkId, "chunks": chunks }]);
      return { "status": "uploadError", "filename": file.name, "chunks": chunks, "uploaded": offset, "size": file.size };
   }

   while (offset < file.size) {
      var chunk = file.slice(offset, offset + chunkSize);

      var stateObj = { "status": "uploading", "filename": file.name, "chunk": chunkId, "chunks": chunks, "uploaded": offset, "size": file.size };
      element.trigger("uploading", [stateObj]);
            
      var formData = new FormData()
      formData.append('uploadId', uploadId)
      formData.append('chunkId', chunkId)
      formData.append('fileOffset', offset)
      formData.append('data', chunk)
      var result;

      try {
         result = await $.ajax({
            type: "POST",
            url: _urlUploadChunk,
            data: formData,
            processData: false,
            contentType: false,
            dataType: "json",
         });
      } catch (jqXHR) {
         element.trigger("uploadError", [{ "filename": file.name, "chunkId": chunkId, "chunks": chunks }]);
         return { "status": "uploadError", "filename": file.name, "chunks": chunks, "uploaded": offset, "size": file.size };
      }

      offset += chunk.size;
      chunkId++;
   }

   try {
      await sendFinishFileUpload(uploadId, folderId, file.name, file.size, chunks, chunkSize);
   } catch (jqXHR) {
      element.trigger("uploadError", [{ "filename": file.name, "chunkId": chunkId, "chunks": chunks }]);
      return { "status": "uploadError", "filename": file.name, "chunks": chunks, "uploaded": offset, "size": file.size };
   }

   element.trigger("uploaded", [{ "filename": file.name, "chunks": chunks, "uploaded": offset, "size": file.size }]);
   return { "status": "uploaded", "filename": file.name, "chunks": chunks, "uploaded": offset, "size": file.size };
}