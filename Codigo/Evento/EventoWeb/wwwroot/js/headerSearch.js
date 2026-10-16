// headerSearch.js

function toggleClearButton() {
    var input = document.getElementById('headerSearchInput');
    var btnClear = document.getElementById('btnClearSearch');
    if (input && btnClear) {
        if (input.value.length > 0) {
            btnClear.style.display = 'block';
        } else {
            btnClear.style.display = 'none';
        }
    }
}

function syncFilter(hiddenId, value) {
    var elem = document.getElementById('hidden_' + hiddenId);
    if (elem) {
        elem.value = value;
    }
}

function applyFilters() {
    const form = document.getElementById('headerSearchForm');
    if (form) {
        // requestSubmit fires the submit event listeners, unlike form.submit()
        if (typeof form.requestSubmit === 'function') {
            form.requestSubmit();
        } else {
            form.dispatchEvent(new Event('submit', { cancelable: true, bubbles: true }));
            form.submit();
        }
    }
}

function clearAllFilters() {
    var dropArea = document.getElementById('drop_AreaInteresse');
    if (dropArea) dropArea.value = '';

    var dropTipo = document.getElementById('drop_TipoEvento');
    if (dropTipo) dropTipo.value = '';

    var dropData = document.getElementById('drop_Data');
    if (dropData) dropData.value = '';

    var dropSub = document.getElementById('drop_PossuiSubeventos');
    if (dropSub) dropSub.checked = false;

    syncFilter('IdAreaInteresse', '');
    syncFilter('IdTipoEvento', '');
    syncFilter('Data', '');
    syncFilter('PossuiSubeventos', '');

    applyFilters();
}

function clearSearch() {
    var input = document.getElementById('headerSearchInput');
    if (input) {
        input.value = '';
        toggleClearButton();
        input.focus();
    }
}

document.addEventListener("DOMContentLoaded", function() {
    toggleClearButton();
    
    // Bind enter key on search input
    var searchInput = document.getElementById("headerSearchInput");
    if (searchInput) {
        searchInput.addEventListener("keypress", function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                
                if (searchInput.value.trim() === '') {
                    alert('Insira algum texto na barra de pesquisa');
                    return;
                }
                
                applyFilters();
            }
        });
    }
});
