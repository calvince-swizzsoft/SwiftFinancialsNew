


       
        const datePickerOptions = {
        startView: 2,
    todayBtn: "linked",
    keyboardNavigation: false,
    forceParse: false,
    autoclose: true,
    format: "dd/mm/yyyy"
        };

    $('#data_3 .input-group.date').datepicker(datePickerOptions);
    $('#startDate').datepicker(datePickerOptions);
    $('#endDate').datepicker(datePickerOptions);



$('#refreshButton1').on('click', function () {
    // Reload the DataTable when the Refresh button is clicked
    tellerGlTable.ajax.reload();
    $('#startDate').val('');
    $('#endDate').val('');
});

const defaultStartDate = new Date();
defaultStartDate.setDate(defaultStartDate.getDate() - 30); // 30 days ago
const defaultEndDate = new Date();

const tellerGlTable = $('#tellerGlTableCustomerReceipts').DataTable({
    "sPaginationType": "full_numbers",
    "bProcessing": true,
    "bServerSide": true,
    "sAjaxSource": customerReceiptsUrl,
    "sServerMethod": "POST",
    "fnServerParams": function (aoData) {
        // Add startDate and endDate to the server parameters
        aoData.push(
            {
                "name": "startDate",
                "value": $('#startDate').val() || defaultStartDate.toISOString().split('T')[0]
            },
            {
                "name": "endDate",
                "value": $('#endDate').val() || defaultEndDate.toISOString().split('T')[0]
            }
        );
    },

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
        {
            "mDataProp": "CustomerFullName",
            "sTitle": "Customer Name",
            "sWidth": "25%",
            "mRender": function (data, type, row) {
                return data ? data : "NULL";
            }
        },
        { "mDataProp": "Debit", "sTitle": "Debit", "sWidth": "10%" },
        { "mDataProp": "Credit", "sTitle": "Credit", "sWidth": "10%" },
        { "mDataProp": "RunningBalance", "sTitle": "Running Balance", "sWidth": "10%" },
        { "mDataProp": "ContraGLAccountName", "sTitle": "Contra G/L Account", "sWidth": "15%" }
    ],
    "aaSorting": [[1, "desc"]]
});



