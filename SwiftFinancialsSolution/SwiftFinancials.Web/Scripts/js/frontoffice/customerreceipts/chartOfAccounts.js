$('.ChartOfAccountsLookup').typeahead({
    source: function (query, process) {
        return $.getJSON(
            //chartOfAccountsUrl,
            { query: query },
            function (ChartOfAccounts) {
                var newChartOfAccountsData = [];
                ChartOfAccountsMap = {};
                $.each(ChartOfAccounts, function (i, ChartOfAccount) {
                    newChartOfAccountsData.push(ChartOfAccount.AccountName);
                    ChartOfAccountsMap[ChartOfAccount.AccountName] = ChartOfAccount;
                });
                process(newChartOfAccountsData);
                return;
            });
    },
    matcher: function (item) {
        if (item.toLowerCase().indexOf(this.query.trim().toLowerCase()) != -1) {
            return true;
        }
    },
    sorter: function (items) {
        return items.sort();
    },
    highlighter: function (item) {
        var regex = new RegExp('(' + this.query + ')', 'gi');
        return item.replace(regex, "<strong>$1</strong>");
    },
    updater: function (item) {
        var chartOfAccountId = "";
        ChartOfAccountId = ChartOfAccountsMap[item].Id;
        console.log("retrueved chart of accont id is", ChartOfAccountId);
        $('#CreditChartOfAccountIdGl').val(ChartOfAccountId);
        return item;
    }
});