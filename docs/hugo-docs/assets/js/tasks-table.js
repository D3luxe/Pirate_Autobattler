document.addEventListener('DOMContentLoaded', function() {
    const table = document.getElementById('tasks-table');
    if (!table) return;

    const tbody = table.querySelector('tbody');
    const rows = Array.from(tbody.querySelectorAll('tr')); // Original full set of rows
    const searchInput = document.getElementById('task-search');
    const stageFilter = document.getElementById('task-filter-stage');
    const headers = table.querySelectorAll('th');

    let currentSortKey = null;
    let currentSortDirection = 1; // 1 for ascending, -1 for descending

    function filterAndSortRows() {
        const searchTerm = searchInput.value.toLowerCase();
        const selectedStage = stageFilter.value;

        let processedRows = rows.filter(row => { // Start with the original 'rows'
            const title = row.cells[0].textContent.toLowerCase();
            const stage = row.cells[1].textContent;
            const system = row.cells[2].textContent.toLowerCase();
            const tags = row.cells[3].textContent.toLowerCase();

            const matchesSearch = title.includes(searchTerm) ||
                                  system.includes(searchTerm) ||
                                  tags.includes(searchTerm);

            const matchesStageFilter = selectedStage === '' || stage === selectedStage;

            // New: Check stage toggles
            const activeToggles = Array.from(document.querySelectorAll('.stage-toggle:checked')).map(cb => cb.value);
            const matchesToggle = activeToggles.includes(stage);

            return matchesSearch && matchesStageFilter && matchesToggle;
        });

        // Apply sorting if a sort key is active
        if (currentSortKey) {
            processedRows.sort((a, b) => {
                let valA, valB;
                if (currentSortKey === 'title') {
                    valA = a.cells[0].textContent.toLowerCase();
                    valB = b.cells[0].textContent.toLowerCase();
                } else if (currentSortKey === 'stage') {
                    valA = a.cells[1].textContent;
                    valB = b.cells[1].textContent;
                    const stageOrder = { 'Blocked': 0, 'Planned': 1, 'In Progress': 2, 'Completed': 3 };
                    return (stageOrder[valA] - stageOrder[valB]) * currentSortDirection;
                } else if (currentSortKey === 'system') {
                    valA = a.cells[2].textContent.toLowerCase();
                    valB = b.cells[2].textContent.toLowerCase();
                } else if (currentSortKey === 'tags') {
                    valA = a.cells[3].textContent.toLowerCase();
                    valB = b.cells[3].textContent.toLowerCase();
                }

                if (valA < valB) return -1 * currentSortDirection;
                if (valA > valB) return 1 * currentSortDirection;
                return 0;
            });
        }

        // Update the table body
        tbody.innerHTML = '';
        processedRows.forEach(row => tbody.appendChild(row));
    }

    searchInput.addEventListener('keyup', filterAndSortRows);
    stageFilter.addEventListener('change', filterAndSortRows);

    const stageToggles = document.querySelectorAll('.stage-toggle');
    stageToggles.forEach(toggle => {
        toggle.addEventListener('change', filterAndSortRows);
    });

    headers.forEach(header => {
        header.addEventListener('click', function() {
            const sortKey = this.dataset.sort;
            if (!sortKey) return;

            // Toggle sort direction if clicking the same header
            if (currentSortKey === sortKey) {
                currentSortDirection *= -1;
            } else {
                currentSortKey = sortKey;
                currentSortDirection = 1; // Default to ascending for new sort key
            }

            // Update header classes for visual indication
            headers.forEach(h => h.classList.remove('asc', 'desc'));
            this.classList.add(currentSortDirection === 1 ? 'asc' : 'desc');

            filterAndSortRows(); // Re-filter and re-sort with new parameters
        });
    });

    // Initial filter and sort
    filterAndSortRows();
});