$('#tellerGlTableCustomerReceipts').DataTable({
    "sPaginationType": "full_numbers",
    "bProcessing": true,
    "bServerSide": true,
    "sAjaxSource": customerReceiptsUrl,
    "sServerMethod": "POST",
    "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
        var createdDate = new Date(parseInt(aData.JournalCreatedDate.replace("/Date(", "").replace(")/", ""), 10));
        var h = ("0" + createdDate.getHours()).slice(-2);
        var m = ("0" + createdDate.getMinutes()).slice(-2);
        var s = ("0" + createdDate.getSeconds()).slice(-2);

        var dt_Created = $.datepicker.formatDate('dd/mm/yy', createdDate) + " " + h + ":" + m + ":" + s;
        aData.JournalCreatedDate = dt_Created;

        $('td:eq(1)', nRow).html(dt_Created);
        return nRow;
    },
    "aoColumns": [
        { "mDataProp": "BranchDescription", "sTitle": "Branch", "sWidth": "15%" },
        { "mDataProp": "JournalCreatedDate", "sTitle": "Txn Date", "sWidth": "15%" },
        { "mDataProp": "CustomerFullName", "sTitle": "Customer Name", "sWidth": "25%" },
        { "mDataProp": "Debit", "sTitle": "Debit", "sWidth": "10%" },
        { "mDataProp": "Credit", "sTitle": "Credit", "sWidth": "10%" },
        { "mDataProp": "RunningBalance", "sTitle": "Running Balance", "sWidth": "10%" },
        { "mDataProp": "ContraGLAccountName", "sTitle": "Contra G/L Account", "sWidth": "15%" }
    ],
    "aaSorting": [[1, "desc"]]
});
