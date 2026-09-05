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
    document.getElementById('hidden_' + hiddenId).value = value;
}

function applyFilters() {
    document.getElementById('headerSearchForm').submit();
}

function clearAllFilters() {
    document.getElementById('drop_AreaInteresse').value = '';
    document.getElementById('drop_TipoEvento').value = '';
    document.getElementById('drop_Estado').value = '';
    document.getElementById('drop_Cidade').value = '';
    document.getElementById('drop_Data').value = '';
    document.getElementById('drop_Cidade').disabled = true;
    
    syncFilter('IdAreaInteresse', '');
    syncFilter('IdTipoEvento', '');
    syncFilter('Estado', '');
    syncFilter('Cidade', '');
    syncFilter('Data', '');
}

function clearSearch() {
    var input = document.getElementById('headerSearchInput');
    if (input) {
        input.value = '';
        toggleClearButton();
        input.focus();
    }
}

async function loadCidades(uf, selectedCidade) {
    var dropCidade = document.getElementById('drop_Cidade');
    dropCidade.innerHTML = '<option value="">Carregando...</option>';
    dropCidade.disabled = true;

    if (!uf) {
        dropCidade.innerHTML = '<option value="">Selecione um Estado primeiro</option>';
        syncFilter('Cidade', '');
        return;
    }

    try {
        const response = await fetch(`https://servicodados.ibge.gov.br/api/v1/localidades/estados/${uf}/municipios`);
        const cidades = await response.json();
        
        dropCidade.innerHTML = '<option value="">Todas as Cidades</option>';
        
        var found = false;
        cidades.forEach(c => {
            var option = document.createElement('option');
            option.value = c.nome;
            option.text = c.nome;
            if(c.nome === selectedCidade) {
                option.selected = true;
                found = true;
            }
            dropCidade.appendChild(option);
        });

        if(!found) {
            syncFilter('Cidade', '');
        }
        
        dropCidade.disabled = false;
    } catch (error) {
        dropCidade.innerHTML = '<option value="">Erro ao carregar cidades</option>';
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
                applyFilters();
            }
        });
    }

    // Load cities if a state is already selected
    var dropEstado = document.getElementById('drop_Estado');
    var dropCidade = document.getElementById('drop_Cidade');
    if(dropEstado && dropEstado.value) {
        // Read selected value from hidden input to re-select after loading
        var selectedCidade = document.getElementById('hidden_Cidade') ? document.getElementById('hidden_Cidade').value : '';
        loadCidades(dropEstado.value, selectedCidade);
    }
});
