// wwwroot/js/location.js

const LOCATION_COOKIE_NAME = 'UserLocation';

document.addEventListener('DOMContentLoaded', function () {
    initializeLocation();
    
    const estadoSelect = document.getElementById('modalEstado');
    if (estadoSelect) {
        estadoSelect.addEventListener('change', function() {
            loadCitiesForState(this.value);
        });
    }
});

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return decodeURIComponent(parts.pop().split(';').shift());
    return null;
}

function setCookie(name, value, days = 30) {
    const date = new Date();
    date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
    const expires = `expires=${date.toUTCString()}`;
    document.cookie = `${name}=${encodeURIComponent(value)}; ${expires}; path=/`;
}

function parseLocationCookie() {
    const cookie = getCookie(LOCATION_COOKIE_NAME);
    if (!cookie) return null;
    try {
        return JSON.parse(cookie);
    } catch (e) {
        return null;
    }
}

function updateHeaderLocationText(locationObj) {
    const headerBtnText = document.getElementById('headerLocationText');
    if (!headerBtnText) return;

    if (locationObj && locationObj.estado) {
        if (locationObj.cidade) {
            headerBtnText.textContent = `${locationObj.cidade} - ${locationObj.estado}`;
        } else {
            headerBtnText.textContent = `Todo o estado (${locationObj.estado})`;
        }
    } else {
        headerBtnText.textContent = "Todo o Brasil";
    }
}

function initializeLocation() {
    const location = parseLocationCookie();
    
    if (!location) {
        // Automatically try to get location
        requestGeolocation();
    } else {
        updateHeaderLocationText(location);
    }
}

function requestGeolocation() {
    if ("geolocation" in navigator) {
        navigator.geolocation.getCurrentPosition(
            (position) => {
                console.log("Coordenadas obtidas:", position.coords.latitude, position.coords.longitude);
                reverseGeocode(position.coords.latitude, position.coords.longitude);
            },
            (error) => {
                console.warn("Falha na geolocalização:", error.message);
                setDefaultLocation();
            },
            { timeout: 10000, enableHighAccuracy: false, maximumAge: 60000 }
        );
    } else {
        console.warn("Navegador não suporta geolocalização");
        setDefaultLocation();
    }
}

async function reverseGeocode(lat, lon) {
    try {
        const response = await fetch(`https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${lat}&longitude=${lon}&localityLanguage=pt-BR`);
        const data = await response.json();
        
        console.log("Resposta da API de Localização:", data);
        
        // Em BigDataCloud, a cidade pode vir em city ou locality
        const cidade = data.city || data.locality || data.principalSubdivision || "";
        const isoCode = data.principalSubdivisionCode || "";
        
        if (cidade && isoCode) {
            let estado = "";
            
            if (isoCode.includes('-')) {
                estado = isoCode.split('-')[1]; // SE
            } else {
                estado = data.principalSubdivision || ""; 
            }
            
            // Garantir que a cidade não seja exatamente igual ao estado se houver outra opção, 
            // mas como fallback final, aceitamos
            
            const locObj = { cidade: cidade, estado: estado };
            console.log("Localização detectada:", locObj);
            
            setCookie(LOCATION_COOKIE_NAME, JSON.stringify(locObj));
            updateHeaderLocationText(locObj);
            return;
        }
        
        console.warn("API não retornou dados suficientes de cidade/estado.");
        setDefaultLocation();
    } catch (e) {
        console.error("Erro na requisição de geolocalização (reverse geocoding):", e);
        setDefaultLocation();
    }
}

function setDefaultLocation() {
    const locObj = { cidade: "", estado: "" };
    setCookie(LOCATION_COOKIE_NAME, JSON.stringify(locObj));
    updateHeaderLocationText(locObj);
}

// Modal functions
function openLocationModal() {
    const modal = new bootstrap.Modal(document.getElementById('locationModal'));
    
    // Populate form with current cookie if exists
    const location = parseLocationCookie();
    if (location && location.estado) {
        document.getElementById('modalEstado').value = location.estado;
        loadCitiesForState(location.estado, location.cidade);
    } else {
        document.getElementById('modalEstado').value = "";
        const cidadeSelect = document.getElementById('modalCidade');
        if (cidadeSelect) {
            cidadeSelect.innerHTML = '<option value="">Selecione um Estado primeiro</option>';
            cidadeSelect.disabled = true;
        }
    }
    
    modal.show();
}

async function loadCitiesForState(uf, selectedCity = "") {
    const cidadeSelect = document.getElementById('modalCidade');
    if (!cidadeSelect) return;
    
    cidadeSelect.innerHTML = '<option value="">Carregando...</option>';
    cidadeSelect.disabled = true;
    
    if (!uf) {
        cidadeSelect.innerHTML = '<option value="">Selecione um Estado primeiro</option>';
        return;
    }
    
    try {
        const response = await fetch(`https://servicodados.ibge.gov.br/api/v1/localidades/estados/${uf}/municipios`);
        const cidades = await response.json();
        
        cidadeSelect.innerHTML = '<option value="">Selecione uma Cidade</option>';
        
        cidades.sort((a, b) => a.nome.localeCompare(b.nome));
        
        cidades.forEach(c => {
            const option = document.createElement('option');
            option.value = c.nome;
            option.textContent = c.nome;
            if (c.nome === selectedCity) {
                option.selected = true;
            }
            cidadeSelect.appendChild(option);
        });
        
        cidadeSelect.disabled = false;
    } catch (e) {
        console.error("Erro ao carregar cidades do IBGE", e);
        cidadeSelect.innerHTML = '<option value="">Erro ao carregar cidades</option>';
    }
}

function saveManualLocation() {
    const estado = document.getElementById('modalEstado').value.trim();
    const cidade = document.getElementById('modalCidade').value.trim();
    
    if (estado) {
        const locObj = { cidade: cidade, estado: estado };
        setCookie(LOCATION_COOKIE_NAME, JSON.stringify(locObj));
        updateHeaderLocationText(locObj);
    } else {
        // Se o estado estiver vazio, assumir Todo o Brasil
        setDefaultLocation();
    }
    
    const modalEl = document.getElementById('locationModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) modal.hide();
    
    // Se estivermos na tela de busca, podemos querer recarregar a busca com o novo filtro
    if (window.location.pathname.toLowerCase().includes('/home/buscar') || window.location.pathname.toLowerCase() === '/buscar') {
        const searchForm = document.getElementById('headerSearchForm');
        if (searchForm) {
            searchForm.requestSubmit();
        } else {
            window.location.reload();
        }
    }
}

function clearManualLocation() {
    document.getElementById('modalEstado').value = "";
    document.getElementById('modalCidade').value = "";
    setDefaultLocation();
    
    const modalEl = document.getElementById('locationModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) modal.hide();
    
    if (window.location.pathname.toLowerCase().includes('/home/buscar')) {
         const searchForm = document.getElementById('headerSearchForm');
         if (searchForm) {
             searchForm.requestSubmit();
         }
    }
}

// Ensure the header search form appends the location correctly when submitted
document.addEventListener('DOMContentLoaded', function() {
    const searchForm = document.getElementById('headerSearchForm');
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            // Check if user clicked 'clear filter' in the search results
            // We use a flag (e.g. data attribute or a global variable) if we should ignore the cookie for this submission
            if (window.ignoreLocationCookie) {
                return; // Let the form submit normally without injecting cookie location
            }

            const location = parseLocationCookie();
            if (location && location.estado) {
                // Remove existing ones if present to avoid duplication
                const existingEstado = document.querySelector('input[name="Estado"]');
                const existingCidade = document.querySelector('input[name="Cidade"]');
                
                if (existingEstado) existingEstado.remove();
                if (existingCidade) existingCidade.remove();
                
                const estadoInput = document.createElement('input');
                estadoInput.type = 'hidden';
                estadoInput.name = 'Estado';
                estadoInput.value = location.estado;
                searchForm.appendChild(estadoInput);
                
                if (location.cidade) {
                    const cidadeInput = document.createElement('input');
                    cidadeInput.type = 'hidden';
                    cidadeInput.name = 'Cidade';
                    cidadeInput.value = location.cidade;
                    searchForm.appendChild(cidadeInput);
                }
            }
        });
    }
});

function clearSearchLocationFilter() {
    // This is called from the badge in the search results
    window.ignoreLocationCookie = true; // Flag for form submit
    const searchForm = document.getElementById('headerSearchForm');
    
    if (searchForm) {
        // Ensure there are no Estado/Cidade inputs
        const existingEstado = document.querySelector('input[name="Estado"]');
        const existingCidade = document.querySelector('input[name="Cidade"]');
        if (existingEstado) existingEstado.value = "";
        if (existingCidade) existingCidade.value = "";
        
        // Also remove them from the URL by submitting a clean form
        searchForm.submit();
    } else {
        // Fallback
        const url = new URL(window.location.href);
        url.searchParams.delete('Estado');
        url.searchParams.delete('Cidade');
        window.location.href = url.toString();
    }
}
