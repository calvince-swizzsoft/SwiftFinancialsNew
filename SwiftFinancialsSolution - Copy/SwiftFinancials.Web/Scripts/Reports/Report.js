$(document).ready(function () {
    $("btnLoadReport").click(function () {
        ReportManagers.LoadReport();
    });
});

var ReportManagers = {
    LoadReport: function () {
        var jsonParam = "";
        var serviceUrl = "../";
        ReportManagers.GetReport(serviceUrl, jsonParam, onFailed);
        function onFailed(error) {
            alert("Found Error");
        }
    },

    GetReport: function (serviceUrl, jsonParams, errorCallBack) {
        jQuery.ajax({
            url: serviceUrl,
            async: false,
            type: "POST",
            data: "{" + jsonParams + "}",
            contentType: "application/json; charset=utf-8",
            success: function () {
                window.open('..Reports/ReportViewer.aspx', '_newtab');
            },
            error: errorCallBack
        });
    }
}