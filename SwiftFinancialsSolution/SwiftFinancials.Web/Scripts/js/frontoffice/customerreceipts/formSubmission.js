$(document).ready(function () {
    $('#submitButton').click(function (e) {

        e.preventDefault();

        if (!isApportionmentValid()) {
            alert("The total value must be fully apportioned before submitting. Please adjust the apportionments.");
            return;
        }

        $.ajax({
            url: $('#customerReceiptsForm').attr('action'),
            type: 'POST',
            data: $('#customerReceiptsForm').serialize(),
            dataType: 'json',
            success: function (response) {
                if (response.redirect) {

                    resetAllFields();
                } else if (response.success) {

                    //alert('Customer Receipts: Operation Success');
                    resetAllFields();
                } else {

                    //alert('Customer Receipts: Operation Failed\n\n' + (response.errorMessages ? response.errorMessages.join('\n') : 'Unknown error'));
                    resetAllFields();
                }
            },
            error: function (xhr, status, error) {

                //alert('An error occurred while submitting the form. Please try again.');
                resetAllFields();
                console.error(error);
            }
        });
    });
});