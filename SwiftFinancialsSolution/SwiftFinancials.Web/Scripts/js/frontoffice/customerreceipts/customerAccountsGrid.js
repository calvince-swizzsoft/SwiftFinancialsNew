$(document).ready(function () {


    var defaultProductCode = $('#productCode').val();
    var defaultCustomerFilter = $('#customerFilter').val();


    //Refresh button for reloading the Datable after Applying the filters
    $('#modalFilterForm').on('submit', function (e) {
        e.preventDefault();
        $('#CustomerAccountsGrid').DataTable().ajax.reload();
    });


    var table = $('#CustomerAccountsGrid').DataTable({
        // Ajax settings
        "sPaginationType": "full_numbers",
        "bProcessing": true,
        "bServerSide": true,
        "sAjaxSource": customerAccountsUrl,
        "sServerMethod": "POST",

        // Ensure to include the filters in the Ajax request
        "fnServerParams": function (aoData) {
            aoData.push(
                { "name": "productCode", "value": $('#productCode').val() || defaultProductCode },
                { "name": "customerFilter", "value": $('#customerFilter').val() || defaultCustomerFilter }
            );
        },

        // Callback settings
        "fnRowCallback": function (nRow, aData) {
            var createdDate = new Date(parseInt(aData.CreatedDate.replace("/Date(", "").replace(")/", ""), 10));
            var h = ("0" + createdDate.getHours()).slice(-2);
            var m = ("0" + createdDate.getMinutes()).slice(-2);
            var s = ("0" + createdDate.getSeconds()).slice(-2);
            var dt_Created = $.datepicker.formatDate('dd/mm/yy', createdDate) + " " + h + ":" + m + ":" + s;

            $('td:eq(4)', nRow).html('<button class="btn btn-outline-info select-btn" data-id="' + aData.Id + '">Select</button>');


            return nRow;
        },

        // Display settings with adjusted column widths
        "aoColumns": [
            { "mDataProp": "CustomerFullName", "sTitle": "Name", "sWidth": "30%" },
            { "mDataProp": "FullAccountNumber", "sTitle": "Account No.", "sWidth": "30%" },
            { "mDataProp": "CustomerReference2", "sTitle": "Membership", "sWidth": "15%" },
            { "mDataProp": "CustomerAccountTypeProductCodeDescription", "sTitle": "Product", "sWidth": "15%" },
            { "mDataProp": "CreatedDate", "sTitle": "", "sWidth": "15%", "bSortable": false }
        ],
        "aaSorting": [[0, "desc"]]
    });



    // Use delegated event handling for dynamic elements
    $('#CustomerAccountsGrid').on('click', '.select-btn', function () {
        var customerAccountId = $(this).data('id');
        fetchCustomerAccountDetails(customerAccountId);
    });

    function fetchCustomerAccountDetails(customerAccountId) {
        console.log("Entered fetchCustomerAccountDetails");

        $.ajax({
            url: '/FrontOffice/CustomerReceipts/GetCustomerAccountDetailsJson',
            type: 'GET',
            data: { id: customerAccountId },
            success: function (data) {
                if (data) {
                    console.log(data);
                    console.log(JSON.stringify(data));

                    // Populate the form fields with the customer details
                    $('#CreditCustomerAccount').val(JSON.stringify(data));
                    $('#CreditCustomerAccountId').val(data.Id);
                    $('#CreditChartOfAccountId').val(data.CustomerAccountTypeTargetProductChartOfAccountId);
                    $('#CreditFullAccountNumber').val(data.FullAccountNumber);
                    $('#CreditCustomerFullName').val(data.CustomerFullName);
                    $('#CreditCustomerReference1').val(data.CustomerReference1);
                    $('#CreditCustomerReference2').val(data.CustomerReference2);
                    $('#CreditBranchDescription').val(data.BranchDescription);
                    $('#CreditBranchId').val(data.BranchId);
                    $('#CreditProductDescription').val(data.CustomerAccountTypeTargetProductDescription);
                    $('#CreditAvailableBalance').val(data.AvailableBalance);

                    // Hide the lookup modal after successful data retrieval
                    $('#CustomerAccountLookupModal').modal('hide');
                } else {
                    console.error("No customer details found.");
                }
            },
            error: function (xhr, status, error) {
                console.error("Failed to fetch customer details:", error);
            }
        });
    }
});
