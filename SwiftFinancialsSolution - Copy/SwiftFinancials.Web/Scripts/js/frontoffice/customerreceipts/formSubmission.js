$(document).ready(function () {
    $('#submitButton').click(function (e) {

        e.preventDefault();

        if (!isApportionmentValid()) {
            Swal.fire({
                icon: 'warning',
                title: 'Validation Error',
                text: 'The total value must be fully apportioned before submitting. Please adjust the apportionments.'
            });
            return;
        }

        Swal.fire({
            title: 'Are you sure?',
            text: "Do you want to proceed with the transaction?",
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, submit it!',
            cancelButtonText: 'No, go back'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: $('#customerReceiptsForm').attr('action'),
                    type: 'POST',
                    data: $('#customerReceiptsForm').serialize(),
                    dataType: 'json',
                    success: function (response) {
                        if (response.redirect) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Redirecting',
                                text: 'Operation successful, redirecting now.'
                            });
                            resetAllFields();
                            $('#tellerGlTableCustomerReceipts').DataTable().ajax.reload();
                        } else if (response.success) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Success',
                                text: 'Customer Receipts: Operation successful.'
                            });
                            resetAllFields();
                            $('#tellerGlTableCustomerReceipts').DataTable().ajax.reload();
                        } else {
                            Swal.fire({
                                icon: 'error',
                                title: 'Error',
                                text: 'Customer Receipts: Operation failed.\n\n' + (response.errorMessages ? response.errorMessages.join('\n') : 'Unknown error')
                            });
                            resetAllFields();
                            $('#tellerGlTableCustomerReceipts').DataTable().ajax.reload();
                        }
                    },
                    error: function (xhr, status, error) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: 'An error occurred while submitting the form. Please try again.'
                        });
                        resetAllFields();
                        console.error(error);
                    }
                });
            }
        });
    });
});
