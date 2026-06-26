
//global vars
var apportionments = JSON.parse(sessionStorage.getItem('apportionments')) || [];
var totalValue = parseFloat(sessionStorage.getItem('totalValue')) || 0;


//apportionment util functions
function escapeHtml(text) {
    return text
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function escapeJson(jsonString) {
    return escapeHtml(jsonString).replace(/\n/g, '\\n').replace(/\r/g, '\\r').replace(/\t/g, '\\t');
}


// handle apportionment functions
function updateApportionmentsHiddenField() {
    $('#apportionmentsData').val(JSON.stringify(apportionments));
}

function getTotalValue() {
    return parseFloat($('#TotalValue').val()) || 0;
}

    function saveTotalValue(value) {
        sessionStorage.setItem('totalValue', value);
}

function isApportionmentValid() {
    var totalValue = getTotalValue();
    var apportioned = parseFloat($('#apportioned').val()) || 0;
    var shortage = parseFloat($('#shortage').val()) || 0;

    return Math.abs(shortage) < 0.01; // Allow for small floating point discrepancies
}

function calculateApportionmentTotals() {
    totalValue = getTotalValue(); // Get the value from the form input

    var apportioned = apportionments.reduce(function (total, item) {
        var principal = parseFloat(item.principal) || 0;
        var interest = parseFloat(item.interest) || 0;
        return total + principal + interest;
    }, 0);

    var shortage = totalValue - apportioned;

    $('#apportioned').val(apportioned.toFixed(2));
    $('#shortage').val(shortage.toFixed(2));

    if (shortage < 0) {
        alert("Total apportioned value exceeds the total available value!");
    } else if (shortage > 0) {
        $('#shortage').css("color", "red"); // Highlight shortage in red if the total hasn't been fully apportioned
    } else {
        $('#shortage').css("color", "green"); // Make it green when apportioned amount equals total
    }

    // Update submit button state
    if (isApportionmentValid()) {
        $('#submitButton').prop('disabled', false);
    } else {
        $('#submitButton').prop('disabled', true);
    }
}

function addItemToApportionmentsTable() {
    var principal = parseFloat($('input[name="ApportionmentWrapper.Principal"]').val()) || 0;
    var interest = parseFloat($('input[name="ApportionmentWrapper.Interest"]').val()) || 0;

    var totalCurrent = apportionments.reduce(function (total, item) {
        return total + (parseFloat(item.principal) || 0) + (parseFloat(item.interest) || 0);
    }, 0);

    var newTotal = totalCurrent + principal + interest;

    if (newTotal > totalValue) {
        alert("Adding this apportionment exceeds the total value! Please adjust the amounts.");
        return;
    }

    var apportionToType = {
        1: "Customer Account",
        2: "G/L Account"
    };

    var apportionTo = $('input[name="ApportionmentWrapper.ApportionTo"]').val();

    console.log("apportion to value is", apportionTo);

    var apportionToDescription = apportionToType[apportionTo];

    console.log("aportion to description is", apportionToDescription);

    var item = {
        apportionTo: apportionTo,
        apportionToDescription: apportionToDescription,
        fullAccountNumber: $('input[name="ApportionmentWrapper.FullAccountNumber"]').val(),
        productDescription: $('input[name="ApportionmentWrapper.ProductDescription"]').val(),
        principal: principal.toFixed(2),
        interest: interest.toFixed(2),
        primaryDescription: $('input[name="ApportionmentWrapper.PrimaryDescription"]').val(),
        creditChartOfAccountId: $('input[name="ApportionmentWrapper.CreditChartOfAccountId"]').val(),
        creditChartOfAccountIdGl: $('input[id="CreditChartOfAccountIdGl"]').val(),
        secondaryDescription: '',
        reference: '',
    };

    console.log("is gl chart of account", item.creditChartOfAccountIdGl);

    apportionments.push(item);
    sessionStorage.setItem('apportionments', JSON.stringify(apportionments));
    updateApportionmentsTable();
    calculateApportionmentTotals();
    updateApportionmentsHiddenField();

    clearApportionmentForm();
}

function clearApportionmentForm() {
    $('input[name="ApportionmentWrapper.FullAccountNumber"]').val('');
    $('input[name="ApportionmentWrapper.ProductDescription"]').val('');
    $('input[name="ApportionmentWrapper.Principal"]').val('');
    $('input[name="ApportionmentWrapper.Interest"]').val('');
    $('input[name="ApportionmentWrapper.PrimaryDescription"]').val('');
    $('input[name="ApportionmentWrapper.CreditChartOfAccountId"]').val('');
    $('input[name="ApportionmentWrapper.CustomerReference1"]').val('');
    $('input[name="ApportionmentWrapper.CustomerReference2"]').val('');
    $('input[name="ApportionmentWrapper.CustomerReference3"]').val('');
    $('input[name="ApportionmentWrapper.CustomerFullName"]').val('');
}


function updateApportionmentsTable() {
    var $tableBody = $('#apportionmentTable tbody');
    $tableBody.empty();

    $.each(apportionments, function (index, item) {

        var principal = parseFloat(item.principal) || 0;
        var interest = parseFloat(item.interest) || 0;

        var creditCustomerAccountJson = $('input[name="ApportionmentWrapper.CreditCustomerAccount"]').val();
        var debitCustomerAccountJson = creditCustomerAccountJson;
      
       

        $tableBody.append(`
                        <tr>
                            <td>${item.apportionToDescription || ''}</td>
                            <td>${item.fullAccountNumber || ''}</td>
                            <td>${item.productDescription || ''}</td>
                            <td>${principal.toFixed(2)}</td>
                            <td>${interest.toFixed(2)}</td>
                            <td>${item.primaryDescription || ''}</td>
    <td>
    <button type="button" class="btn btn-danger remove-btn" data-index="${index}">
        <i class="fas fa-times"></i>
    </button>
</td>
<input type="hidden" name="Apportionments[${index}].ApportionTo" value="${item.apportionTo || ''}" />
<input type="hidden" name="Apportionments[${index}].FullAccountNumber" value="${item.fullAccountNumber || ''}" />
<input type="hidden" name="Apportionments[${index}].ProductDescription" value="${item.productDescription || ''}" />
<input type="hidden" name="Apportionments[${index}].Principal" value="${principal.toFixed(2)}" />
<input type="hidden" name="Apportionments[${index}].Interest" value="${interest.toFixed(2)}" />
<input type="hidden" name="Apportionments[${index}].PrimaryDescription" value="${item.primaryDescription || ''}" />
<input type="hidden" name="Apportionments[${index}].SecondaryDescription" value="${item.secondaryDescription || ''}" />
<input type="hidden" name="Apportionments[${index}].Reference" value="${item.reference || ''}" />
<input type="hidden" name="Apportionments[${index}].CreditCustomerAccountJson" value="${escapeJson(creditCustomerAccountJson)}" />
<input type="hidden" name="Apportionments[${index}].DebitCustomerAccountJson" value="${escapeJson(debitCustomerAccountJson)}" />

<!-- Parse and set CreditCustomerAccountId only if creditCustomerAccountJson is not null -->
<input type="hidden" name="Apportionments[${index}].CreditCustomerAccountId" 
       value="${creditCustomerAccountJson ? JSON.parse(creditCustomerAccountJson).Id || '' : ''}" />

<!-- Parse and set DebitCustomerAccountId only if debitCustomerAccountJson is not null -->
<input type="hidden" name="Apportionments[${index}].DebitCustomerAccountId" 
       value="${debitCustomerAccountJson ? JSON.parse(debitCustomerAccountJson).Id || '' : ''}" />

<!-- Conditional setting of CreditChartOfAccountId -->
<input type="hidden" name="Apportionments[${index}].CreditChartOfAccountId" 
       value="${item.creditChartOfAccountIdGl ? item.creditChartOfAccountIdGl : (item.creditChartOfAccountId || '')}" />


                    `);
    });
}

$('#apportionmentTable').on('click', '.remove-btn', function () {
    var index = $(this).data('index');
    apportionments.splice(index, 1);
    sessionStorage.setItem('apportionments', JSON.stringify(apportionments));
    updateApportionmentsTable();
    calculateApportionmentTotals();
    updateApportionmentsHiddenField();
});

function resetAllFields() {
    // Clear customer account fields
    $('#DebitCustomerAccountId, #DebitCustomerAccountJson, #FullName, #RegistrationDate, #StationDescription, #StationId, #StationZoneDivisionEmployerDescription, #StationZoneDivisionEmployerId, #Reference1, #Reference2, #Reference3, #ProductDescription, #IdentificationNumber, #Remarks, #IndividualPayrollNumbers, #TypeDescription').val('');

    $('#CreditCustomerAccount, #CreditCustomerAccountJson, #CreditCustomerAccountId, #CreditChartOfAccountId, #CreditChartOfAccountIdGl, #CreditChartOfAccountNameGl, #CreditFullAccountNumber, #CreditCustomerFullName, #CreditCustomerReference1, #CreditCustomerReference2, #CreditBranchDescription, #CreditBranchId, #CreditProductDescription, #Principal, #Interest, #PrimaryDescription, #CreditAvailableBalance').val('');

    // Clear apportionment fields
    clearApportionmentForm();

    // Clear total values
    $('#TotalValue, #apportioned, #shortage').val('');

    // Reset apportionments array and table
    apportionments = [];
    sessionStorage.removeItem('apportionments');
    updateApportionmentsTable();

    // Reset total value in session storage
    saveTotalValue(0);

    // Disable submit button
    $('#submitButton').prop('disabled', true);
}


//events
$('#addButton').click(function () {
    addItemToApportionmentsTable();
});

$('#TotalValue').on('input', calculateApportionmentTotals);
$('input[name="ApportionmentWrapper.Principal"]').on('input', calculateApportionmentTotals);
$('input[name="ApportionmentWrapper.Interest"]').on('input', calculateApportionmentTotals);

$(document).ready(function () {
    $('#submitButton').click(function (e) {
        if (!isApportionmentValid()) {
            e.preventDefault(); // Prevent
            alert("The total value must be fully apportioned before submitting. Please adjust the apportionments.");
        }
    });
});


//call functions
updateApportionmentsTable();
calculateApportionmentTotals();
updateApportionmentsHiddenField();