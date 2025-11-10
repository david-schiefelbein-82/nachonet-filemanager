
async function queueFileUpload(folderId, files) {
   var i;
   var rows = [];
   for (i = 0; i < files.length; ++i) {
      var element = addUploadingFile("#panel-upload-files", files[i]);

      element.on("uploading", function (ev, data) {
         var filename = data.filename;
         var perc = Math.floor(data.uploaded / data.size * 100);
         setFileUploadProgress($(this), filename, perc);
      });
      element.on("uploaded", function (ev, data) {
         var filename = data.filename;
         var id = $(this).id;
         setFileUploadProgressComplete($(this), filename);
      });
      element.on("uploadError", function (ev, data) {
         var filename = data.filename;
         setFileUploadProgressError($(this), filename);
      });
      rows.push(element);
   }

   for (i = 0; i < files.length; ++i) {
      await uploadFile(rows[i], folderId, files[i]);
    }

    await delay(1000);
}

function delay(t, val) {
    return new Promise(resolve => setTimeout(resolve, t, val));
}

function setFileUploadProgress(element, filename, percent) {
   var progressBar = element.find(".progress-bar");
   progressBar.attr("style", "width: " + percent + "%");
}

function setFileUploadProgressComplete(element, filename) {
   var progressBar = element.find(".progress-bar");
   var closeBtn = element.find(".btn-close");
   closeBtn.attr("disabled", false);
   progressBar.attr("style", "width: 100%");
   progressBar.addClass("bg-success");
}

function setFileUploadProgressError(element, filename) {
   var progressBar = element.find(".progress-bar");
   var closeBtn = element.find(".btn-close");
   closeBtn.attr("disabled", false);
   progressBar.attr("style", "width: 100%");
   progressBar.addClass("bg-danger");
}

function randomFileId() {
   return "file-upload-" + Math.floor(Math.random() * 10000)
}

function addUploadingFile(selector, file) {
   var id = randomFileId();
   var row = $('<div />', { "id": id, "class": "row py-1 upload-file", "data-upload-file": file.name });
   var col = $('<div />', { "class": "col" });
    var title = $('<span />', { "class": "upload-fname" });
   title.append(file.name);
   col.append(title);
   row.append(col);

    col = $('<div />', { "class": "col file-upload-progress-col" });
    var progressDiv = $('<div />', { "class": "progress file-upload-progress", "role": "progressbar" });
   progressDiv.append($('<div />', { "class": "progress-bar", "style": "width: 0%" }));
   col.append(progressDiv);
   row.append(col);

   col = $('<div />', { "class": "col-auto" });
   var closeBtn = $('<button />', { "class": "btn-sm btn-close", "disabled": "true" });
   col.append(closeBtn);
   row.append(col);

   closeBtn.on("click", function () {
      var element = $(this).closest(".upload-file");
      element.remove();
   });

   $(selector).append(row);
   return row;
}