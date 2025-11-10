function encodeHtml(text) {
   var safestring = $('<div>').text(text).html();
   return safestring;
}

var _alertId = 10;
var _container = "#liveAlerts";

function initAlerts(containerSelector) {
   _container = containerSelector;
}

function showAlert(level, message, timeout = 0) {
   var alertId = "alert" + (++_alertId);
   if (timeout == 0)
      timeout = 3000;

   var html = '<div id="' + alertId + '" class="alert alert-' + level + ' alert-dismissible" role="alert">' +
      '   <div>' + encodeHtml(message) + '</div>' +
      '   <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
      '</div>';
   $(_container).append(html);
   if (timeout > 0) {
      window.setTimeout(function () {
         hideAlert(alertId);
      }, timeout);
   }
   return alertId;
}

function hideAlert(alertId) {
   $("#" + alertId).slideUp(200);
}