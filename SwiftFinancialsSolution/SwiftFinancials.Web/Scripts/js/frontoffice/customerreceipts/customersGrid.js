
// Set Default Value for Filters
var defaultRecordStatus = $('#CustomerRecordStatus').val();
var defaultCustomerFilter = $('#CustomerFilter').val();

console.log(defaultCustomerFilter);
console.log(defaultRecordStatus);

//Refresh button for reloading the Datable after Applying the filters
$('#CustomerModalFilterForm').on('submit', function (e) {
    e.preventDefault();
    $('#CustomersGrid').DataTable().ajax.reload();
});

$('#CustomersGrid').DataTable({

    "sPaginationType": "full_numbers",
    "bProcessing": true,
    "bServerSide": true,
    "sAjaxSource": customersUrl, // Use the variable defined in the view
    "sServerMethod": "POST",
    "fnServerParams": function (aoData) {
        aoData.push(
            { "name": "recordStatus", "value": $('#CustomerRecordStatus').val() || defaultRecordStatus },
            { "name": "customerFilter", "value": $('#CustomerFilter').val() || defaultCustomerFilter }
        );
    },
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
    "aaSorting": [[0, "desc"]],
    "dom": 'lftip',
    "responsive": true,
    "initComplete": function (settings, json) {
        $('#CustomersGrid_length').appendTo('.blue-section .row:first-child #items-dropdown');
        $('#CustomersGrid_filter').appendTo('.blue-section #searchField').find('input').attr('placeholder', 'Search...');
        $('#CustomersGrid_filter label').contents().filter(function () {
            return this.nodeType === 3;
        }).remove();
        $('#Customers_paginate').appendTo('.blue-section .row:first-child #page-control');
    },
    "error": function (xhr, error, thrown) {
        console.log("Error fetching data: ", xhr, error, thrown);
        alert("Failed to load data. Please try again.");
    }
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





//$('#CustomersGrid').DataTable({
//    // Ajax settings
//    "sPaginationType": "full_numbers",
//    "bProcessing": true,
//    "bServerSide": true,
//    "sAjaxSource": "@(Url.Action("Index", "Customer", new { Area = "Registry"}))",
//    "sServerMethod": "POST",
//    "fnServerParams": function (aoData) {
//        aoData.push(
//            { "name": "recordStatus", "value": $('#EmployeeCustomerRecordStatus').val() || defaultRecordStatus },
//            { "name": "customerFilter", "value": $('#EmployeeCustomerFilter').val() || defaultCustomerFilter }
//        );
//    },
//    // Callback settings
//    "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {

//        var createdDate = new Date(parseInt(aData.CreatedDate.replace("/Date(", "").replace(")/", ""), 10));
//        var h = ("0" + createdDate.getHours()).slice(-2);
//        var m = ("0" + createdDate.getMinutes()).slice(-2);
//        var s = ("0" + createdDate.getSeconds()).slice(-2);

//        var dt_Created = $.datepicker.formatDate('dd/mm/yy', createdDate) + " " + h + ":" + m + ":" + s;
//        aData.CreatedDate = dt_Created;

//        var selectUrl = '<a  href="/humanResource/employee/create/' + aData.Id + '" class = "btn btn-outline-info"">' + 'Select</a>';

//        $('td:eq(4)', nRow).html(dt_Created);
//        $('td:eq(5)', nRow).html(selectUrl);

//        return nRow;
//    },
//    // Display settings
//    "aoColumns": [
//        { "mDataProp": "FullName", "sTitle": "Name", "sWidth": "15%" },
//        { "mDataProp": "TypeDescription", "sTitle": "Type", "sWidth": "10%" },
//        { "mDataProp": "Reference2", "sTitle": "Membership  Number", "sWidth": "10%" },
//        { "mDataProp": "IndividualTypeDescription", "sTitle": "Individual Type", "sWidth": "15%" },
//        { "mDataProp": "CreatedDate", "sTitle": "Created Date", "sWidth": "10%" },
//        { "mDataProp": "CreatedDate", "sTitle": "", "sWidth": "10%", "bSortable": false }],
//    "aaSorting": [[0, "desc"]],
//    "dom": 'lftip',
//    "responsive": true,
//    "initComplete": function (settings, json) {
//        $('#CustomersGrid_length').appendTo('.blue-section .row:first-child #items-dropdown');
//        $('#CustomersGrid_filter').appendTo('.blue-section #searchField').find('input').attr('placeholder', 'Search...');
//        $('#CustomersGrid_filter label').contents().filter(function () {
//            return this.nodeType === 3;
//        }).remove();
//        $('#Customers_paginate').appendTo('.blue-section .row:first-child #page-control');
//    },
//    "error": function (xhr, error, thrown) {
//        console.log("Error fetching data: ", xhr, error, thrown);
//        alert("Failed to load data. Please try again.");
//    }
//});

//$("#s tbody").on("click", "tr", function () {
//    var data = customersGrid.row(this).data();

//    $('#CustomerId').val(data.Id);
//    $('input[name=CustomerId]').val(data.Id);
//    $('input[name=CustomerFullName]').val(data.FullName);
//    //$('input[name=ChartOfAccountAccountCode]').val(data.AccountCode);

//    $('#customerLookupModal').modal('hide');
//});

