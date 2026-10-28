window.perfiNetWorth = {
    scrollToRight: function (elementId) {
        var el = document.getElementById(elementId);
        if (el) {
            el.scrollLeft = el.scrollWidth;
        }
    },

    syncLedgerRows: function (accountTableId, valuesContainerId) {
        var accountTable = document.getElementById(accountTableId);
        var valuesContainer = document.getElementById(valuesContainerId);
        var valuesTable = valuesContainer && valuesContainer.querySelector('table');
        if (!accountTable || !valuesTable) {
            return;
        }

        var syncRows = function () {
            var accountRows = Array.from(accountTable.rows);
            var valueRows = Array.from(valuesTable.rows);
            accountRows.forEach(function (row) { row.style.height = ''; });
            valueRows.forEach(function (row) { row.style.height = ''; });

            accountRows.forEach(function (row, index) {
                var valueRow = valueRows[index];
                if (!valueRow) {
                    return;
                }

                var height = Math.max(row.getBoundingClientRect().height, valueRow.getBoundingClientRect().height);
                row.style.height = height + 'px';
                valueRow.style.height = height + 'px';
            });
        };

        syncRows();

        if (!window.ResizeObserver) {
            return;
        }

        if (accountTable.perfiResizeObserver) {
            accountTable.perfiResizeObserver.disconnect();
        }

        var observer = new ResizeObserver(syncRows);
        observer.observe(accountTable);
        observer.observe(valuesTable);
        accountTable.perfiResizeObserver = observer;
    }
};
