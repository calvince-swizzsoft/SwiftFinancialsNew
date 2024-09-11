$('#CustomersGrid').DataTable({
   

    "sPaginationType": "full_numbers",
    "bProcessing": true,
    "bServerSide": true,
    "sAjaxSource": customersUrl, // Use the variable defined in the view
    "sServerMethod": "POST",
    "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
        var createdDate = new Date(parseInt(aData.CreatedDate.replace("/Date(", "").replace(")/", ""), 10));
        var h = ("0" + createdDate.getHours()).slice(-2);
        var m = ("0" + createdDate.getMinutes()).slice(-2);
        var s = ("0" + createdDate.getSeconds()).slice(-2);

        var dt_Created = $.datepicker.formatDate('dd/mm/yy', createdDate) + " " + h + ":" + m + ":" + s;
        aData.CreatedDate = dt_Created;

        var selectUrl = '<button class="btn btn-outline-info" onclick="fetchCustomerDetails(\'' + String(aData.Id) + '\')">Select</button>';

        $('td:eq(4)', nRow).html(dt_Created);
        $('td:eq(5)', nRow).html(selectUrl);

        return nRow;
    },
    "aoColumns": [
        { "mDataProp": "FullName", "sTitle": "Name", "sWidth": "15%" },
        { "mDataProp": "TypeDescription", "sTitle": "Type", "sWidth": "10%" },
        { "mDataProp": "Reference2", "sTitle": "Membership  Number", "sWidth": "10%" },
        { "mDataProp": "IndividualTypeDescription", "sTitle": "Individual Type", "sWidth": "15%" },
        { "mDataProp": "CreatedDate", "sTitle": "Created Date", "sWidth": "10%" },
        { "mDataProp": "CreatedDate", "sTitle": "", "sWidth": "10%", "bSortable": false }
    ],
    "aaSorting": [[0, "desc"]]
});

function fetchCustomerDetails(customerId) {
    console.log("entered fetchCustomerDetails");

    $.ajax({
        url: '/FrontOffice/CustomerReceipts/GetCustomerDetailsJson',
        type: 'GET',
        data: { id: customerId },
        success: function (data) {
            if (data) {
                console.log(data);
                console.log(JSON.stringify(data));

                // Populate the fields with the returned data
                $('#FullName').val(data.FullName);
                $('#Type').val(data.Type);
                $('#TypeDescription').val(data.TypeDescription);
                $('#IndividualPayrollNumbers').val(data.IndividualPayrollNumbers);
                $('#StationZoneDivisionEmployerId').val(data.StationZoneDivisionEmployerId);
                $('#StationZoneDivisionEmployerDescription').val(data.StationZoneDivisionEmployerDescription);
                $('#StationId').val(data.StationId);
                $('#StationDescription').val(data.StationDescription);
                $('#RegistrationDate').val(data.RegistrationDate);
                $('#IdentificationNumber').val(data.IdentificationNumber);
                $('#Reference1').val(data.Reference1);
                $('#Reference2').val(data.Reference2);
                $('#Reference3').val(data.Reference3);

                // Close the modal after successful data retrieval
                $('#CustomerLookupModal').modal('hide');
            } else {
                console.error("No customer details found.");
            }
        },
        error: function (xhr, status, error) {
            console.error("Failed to fetch customer details:", error);
        }
    });
}