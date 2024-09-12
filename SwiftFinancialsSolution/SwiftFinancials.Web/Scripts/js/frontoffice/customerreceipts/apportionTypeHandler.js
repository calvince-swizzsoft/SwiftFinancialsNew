$(document).ready(function () {
    // Set default 'ApportionTo' when the page loads (Customer Account by default)
    $('#ApportionToType').val(1);

    // Listen for tab changes
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var activeTab = $(e.target).attr('href'); // Get the active tab ID

        if (activeTab === '#apportionToCustomerAccount') {
            // Set ApportionTo to 'CustomerAccount' for the Customer Account tab
            $('#ApportionToType').val(1);
        } else if (activeTab === '#apportionToGlAccount') {
            // Set ApportionTo to 'GLAccount' for the GL Account tab
            $('#ApportionToType').val(2);
        }
    });
});