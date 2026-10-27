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

const ESTADOS_BRASIL_NOMES = {
    'AC': 'Acre', 'AL': 'Alagoas', 'AP': 'Amapá', 'AM': 'Amazonas',
    'BA': 'Bahia', 'CE': 'Ceará', 'DF': 'Distrito Federal', 'ES': 'Espírito Santo',
    'GO': 'Goiás', 'MA': 'Maranhão', 'MT': 'Mato Grosso', 'MS': 'Mato Grosso do Sul',
    'MG': 'Minas Gerais', 'PA': 'Pará', 'PB': 'Paraíba', 'PR': 'Paraná',
    'PE': 'Pernambuco', 'PI': 'Piauí', 'RJ': 'Rio de Janeiro', 'RN': 'Rio Grande do Norte',
    'RS': 'Rio Grande do Sul', 'RO': 'Rondônia', 'RR': 'Roraima', 'SC': 'Santa Catarina',
    'SP': 'São Paulo', 'SE': 'Sergipe', 'TO': 'Tocantins'
};

function updateHeaderLocationText(locationObj) {
    const headerBtnText = document.getElementById('headerLocationText');
    if (!headerBtnText) return;

    if (locationObj && locationObj.estado) {
        const uf = locationObj.estado.toUpperCase();
        if (locationObj.cidade) {
            headerBtnText.textContent = `${locationObj.cidade} (${uf})`;
        } else {
            const nomeEstado = ESTADOS_BRASIL_NOMES[uf] || uf;
            headerBtnText.textContent = `${nomeEstado} (${uf})`;
        }
    } else {
        headerBtnText.textContent = "Todo o Brasil";
    }
}

const ESTADOS_BRASIL_MAP = {
    'acre': 'AC', 'alagoas': 'AL', 'amapá': 'AP', 'amapa': 'AP',
    'amazonas': 'AM', 'bahia': 'BA', 'ceará': 'CE', 'ceara': 'CE',
    'distrito federal': 'DF', 'espírito santo': 'ES', 'espirito santo': 'ES',
    'goiás': 'GO', 'goias': 'GO', 'maranhão': 'MA', 'maranhao': 'MA',
    'mato grosso': 'MT', 'mato grosso do sul': 'MS', 'minas gerais': 'MG',
    'pará': 'PA', 'para': 'PA', 'paraíba': 'PB', 'paraiba': 'PB',
    'paraná': 'PR', 'parana': 'PR', 'pernambuco': 'PE', 'piauí': 'PI', 'piaui': 'PI',
    'rio de janeiro': 'RJ', 'rio grande do norte': 'RN', 'rio grande do sul': 'RS',
    'rondônia': 'RO', 'rondonia': 'RO', 'roraima': 'RR', 'santa catarina': 'SC',
    'são paulo': 'SP', 'sao paulo': 'SP', 'sergipe': 'SE', 'tocantins': 'TO'
};

function extractLocationFromBigData(data) {
    if (!data) return null;

    let cidade = data.city || data.locality || "";

    // Se a cidade não veio nos campos padrão, tentar achar em localityInfo.administrative
    if (!cidade && data.localityInfo && Array.isArray(data.localityInfo.administrative)) {
        const adminCity = data.localityInfo.administrative.find(a => 
            a.adminLevel === 8 || (a.description && a.description.toLowerCase().includes('munic'))
        );
        if (adminCity && adminCity.name) {
            cidade = adminCity.name;
        }
    }

    const isoCode = data.principalSubdivisionCode || "";
    let estado = "";

    if (isoCode && isoCode.includes('-')) {
        estado = isoCode.split('-')[1].toUpperCase();
    } else if (data.principalSubdivision) {
        const subLower = data.principalSubdivision.trim().toLowerCase();
        estado = ESTADOS_BRASIL_MAP[subLower] || (subLower.length === 2 ? subLower.toUpperCase() : "");
    }

    if (cidade && estado) {
        return { cidade: cidade.trim(), estado: estado.trim() };
    }

    return null;
}

async function reverseGeocodeCoords(lat, lon) {
    try {
        const response = await fetch(`https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${lat}&longitude=${lon}&localityLanguage=pt-BR`);
        if (!response.ok) return null;
        const data = await response.json();
        return extractLocationFromBigData(data);
    } catch (e) {
        console.error("Erro na requisição de geolocalização por coordenadas:", e);
        return null;
    }
}

async function reverseGeocodeByIp() {
    try {
        const response = await fetch(`https://api.bigdatacloud.net/data/reverse-geocode-client?localityLanguage=pt-BR`);
        if (!response.ok) return null;
        const data = await response.json();
        return extractLocationFromBigData(data);
    } catch (e) {
        console.error("Erro no fallback de geolocalização por IP:", e);
        return null;
    }
}

function isSearchPage() {
    const path = window.location.pathname.toLowerCase();
    return path.includes('/home/buscar') || path === '/buscar';
}

function initializeLocation() {
    if (isSearchPage()) {
        // Na tela de busca, o cabeçalho reflete o filtro efetivo da busca vindo do servidor
        return;
    }

    const headerBtnText = document.getElementById('headerLocationText');
    const serverResolvedUf = headerBtnText ? (headerBtnText.getAttribute('data-server-resolved') || '').trim() : '';
    const location = parseLocationCookie();
    
    // Se o servidor já resolveu o estado ativo (ex: usuário autenticado ou busca por estado específico)
    if (serverResolvedUf) {
        if (location && location.manual && location.estado === serverResolvedUf && location.cidade) {
            updateHeaderLocationText(location);
        }
        // Não substitui o header nem dispara geolocalização automática se o servidor já resolveu a localização
        return;
    }

    // Se temos cookie manual explícito
    if (location && location.manual) {
        updateHeaderLocationText(location);
        return;
    }

    // Se temos cookie automático persistido
    if (location && location.estado) {
        updateHeaderLocationText(location);
        return;
    }

    // Visitante novo sem cookie e sem estado resolvido pelo servidor
    requestGeolocation();
}

function requestGeolocation() {
    if ("geolocation" in navigator) {
        navigator.geolocation.getCurrentPosition(
            (position) => {
                console.log("Coordenadas obtidas:", position.coords.latitude, position.coords.longitude);
                reverseGeocode(position.coords.latitude, position.coords.longitude);
            },
            async (error) => {
                console.warn("Falha na geolocalização do navegador:", error.message);
                const ipLoc = await reverseGeocodeByIp();
                if (ipLoc) {
                    ipLoc.manual = false;
                    setCookie(LOCATION_COOKIE_NAME, JSON.stringify(ipLoc));
                    updateHeaderLocationText(ipLoc);
                    if (isHomePage() || isSearchPage()) {
                        reloadLocationContext();
                    }
                } else {
                    setDefaultLocation(false);
                }
            },
            { timeout: 10000, enableHighAccuracy: false, maximumAge: 60000 }
        );
    } else {
        console.warn("Navegador não suporta geolocalização");
        reverseGeocodeByIp().then(ipLoc => {
            if (ipLoc) {
                ipLoc.manual = false;
                setCookie(LOCATION_COOKIE_NAME, JSON.stringify(ipLoc));
                updateHeaderLocationText(ipLoc);
                if (isHomePage() || isSearchPage()) {
                    reloadLocationContext();
                }
            } else {
                setDefaultLocation(false);
            }
        });
    }
}

async function reverseGeocode(lat, lon) {
    try {
        const locObj = await reverseGeocodeCoords(lat, lon);
        if (locObj) {
            locObj.manual = false;
            console.log("Localização detectada:", locObj);
            setCookie(LOCATION_COOKIE_NAME, JSON.stringify(locObj));
            updateHeaderLocationText(locObj);
            if (isHomePage() || isSearchPage()) {
                reloadLocationContext();
            }
            return;
        }
        
        console.warn("Coordenadas não retornaram cidade/estado, tentando IP...");
        const ipLoc = await reverseGeocodeByIp();
        if (ipLoc) {
            ipLoc.manual = false;
            setCookie(LOCATION_COOKIE_NAME, JSON.stringify(ipLoc));
            updateHeaderLocationText(ipLoc);
            if (isHomePage() || isSearchPage()) {
                reloadLocationContext();
            }
            return;
        }
        
        setDefaultLocation(false);
    } catch (e) {
        console.error("Erro na requisição de geolocalização:", e);
        setDefaultLocation(false);
    }
}

function setDefaultLocation(persist = true) {
    const locObj = { cidade: "", estado: "", manual: persist };
    if (persist) {
        setCookie(LOCATION_COOKIE_NAME, JSON.stringify(locObj));
    }
    updateHeaderLocationText(locObj);
}

// Detecção sob demanda acionada pelo botão do modal
async function detectCurrentLocation() {
    const btn = document.getElementById('btnDetectCurrentLocation');
    const btnText = document.getElementById('btnDetectCurrentLocationText');
    const feedback = document.getElementById('locationDetectFeedback');

    if (feedback) {
        feedback.textContent = '';
        feedback.classList.add('d-none');
    }

    if (!("geolocation" in navigator)) {
        if (feedback) {
            feedback.textContent = 'Seu navegador não suporta geolocalização. Selecione manualmente.';
            feedback.classList.remove('d-none');
        }
        return;
    }

    if (btn) btn.disabled = true;
    if (btnText) {
        btnText.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Detectando localização...';
    }

    const restoreBtn = () => {
        if (btn) btn.disabled = false;
        if (btnText) {
            btnText.innerHTML = 'Usar minha localização atual';
        }
    };

    navigator.geolocation.getCurrentPosition(
        async (position) => {
            try {
                const locObj = await reverseGeocodeCoords(position.coords.latitude, position.coords.longitude);
                if (locObj) {
                    applyDetectedLocation(locObj);
                } else {
                    const ipLoc = await reverseGeocodeByIp();
                    if (ipLoc) {
                        applyDetectedLocation(ipLoc);
                    } else {
                        restoreBtn();
                        if (feedback) {
                            feedback.textContent = 'Não foi possível identificar a cidade para suas coordenadas. Selecione manualmente.';
                            feedback.classList.remove('d-none');
                        }
                    }
                }
            } catch (err) {
                restoreBtn();
                if (feedback) {
                    feedback.textContent = 'Erro ao processar localização. Selecione manualmente.';
                    feedback.classList.remove('d-none');
                }
            }
        },
        async (error) => {
            console.warn("Falha no getCurrentPosition ao clicar no botão:", error.message);
            // Tenta fallback por IP antes de desistir
            const ipLoc = await reverseGeocodeByIp();
            if (ipLoc) {
                applyDetectedLocation(ipLoc);
                return;
            }

            restoreBtn();
            if (feedback) {
                if (error.code === 1) {
                    feedback.textContent = 'Permissão de localização negada no navegador. Permita o acesso ou selecione abaixo.';
                } else if (error.code === 2) {
                    feedback.textContent = 'Sinal de localização indisponível na sua rede/dispositivo. Selecione abaixo.';
                } else if (error.code === 3) {
                    feedback.textContent = 'Tempo limite esgotado ao buscar localização. Tente novamente ou selecione abaixo.';
                } else {
                    feedback.textContent = 'Não foi possível obter sua localização atual. Selecione abaixo.';
                }
                feedback.classList.remove('d-none');
            }
        },
        { timeout: 12000, enableHighAccuracy: true, maximumAge: 0 }
    );
}

function applyDetectedLocation(locObj) {
    locObj.manual = true;
    setCookie(LOCATION_COOKIE_NAME, JSON.stringify(locObj));
    updateHeaderLocationText(locObj);

    // Atualizar selects do modal se estiver aberto
    const estadoSelect = document.getElementById('modalEstado');
    if (estadoSelect && locObj.estado) {
        estadoSelect.value = locObj.estado;
        loadCitiesForState(locObj.estado, locObj.cidade);
    }

    // Restaurar botão
    const btn = document.getElementById('btnDetectCurrentLocation');
    const btnText = document.getElementById('btnDetectCurrentLocationText');
    if (btn) btn.disabled = false;
    if (btnText) {
        btnText.innerHTML = 'Usar minha localização atual';
    }

    // Fechar modal
    const modalEl = document.getElementById('locationModal');
    if (modalEl) {
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }

    reloadLocationContext();
}

function isHomePage() {
    const path = window.location.pathname.toLowerCase();
    return path === '/' || path === '/home' || path === '/home/index';
}

function reloadLocationContext() {
    const isSearchPage = window.location.pathname.toLowerCase().includes('/home/buscar') || window.location.pathname.toLowerCase() === '/buscar';
    if (isSearchPage) {
        const searchForm = document.getElementById('headerSearchForm');
        if (searchForm) {
            searchForm.requestSubmit();
        } else {
            window.location.reload();
        }
    } else if (isHomePage()) {
        if (window.location.search) {
            window.location.href = window.location.pathname;
        } else {
            window.location.reload();
        }
    }
}

// Modal functions
function openLocationModal() {
    const modalEl = document.getElementById('locationModal');
    if (!modalEl) return;
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

    // Limpar mensagens de feedback de erro anteriores
    const feedback = document.getElementById('locationDetectFeedback');
    if (feedback) {
        feedback.textContent = '';
        feedback.classList.add('d-none');
    }

    // Resetar estado do botão
    const btn = document.getElementById('btnDetectCurrentLocation');
    const btnText = document.getElementById('btnDetectCurrentLocationText');
    if (btn) btn.disabled = false;
    if (btnText) {
        btnText.innerHTML = 'Usar minha localização atual';
    }

    // Populate form with current cookie if exists, or fallback to serverResolvedUf (ViewBag.EstadoAtivo)
    const estadoSelect = document.getElementById('modalEstado');
    const serverResolvedUf = (
        (modalEl ? modalEl.getAttribute('data-server-resolved') : '') ||
        (estadoSelect ? estadoSelect.getAttribute('data-server-resolved') : '') ||
        (document.getElementById('headerLocationText') ? document.getElementById('headerLocationText').getAttribute('data-server-resolved') : '') ||
        ''
    ).trim();

    const location = parseLocationCookie();
    if (location && location.estado) {
        if (estadoSelect) estadoSelect.value = location.estado;
        loadCitiesForState(location.estado, location.cidade);
    } else if (serverResolvedUf) {
        if (estadoSelect) estadoSelect.value = serverResolvedUf;
        loadCitiesForState(serverResolvedUf, "");
    } else {
        if (estadoSelect) estadoSelect.value = "";
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
        const locObj = { cidade: cidade, estado: estado, manual: true };
        setCookie(LOCATION_COOKIE_NAME, JSON.stringify(locObj));
        updateHeaderLocationText(locObj);
    } else {
        // Se o estado estiver vazio, assumir Todo o Brasil
        setDefaultLocation(true);
    }

    const modalEl = document.getElementById('locationModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) modal.hide();

    // Se estivermos na tela de busca, podemos querer recarregar a busca com o novo filtro
    reloadLocationContext();
}

function clearManualLocation() {
    document.getElementById('modalEstado').value = "";
    document.getElementById('modalCidade').value = "";
    setDefaultLocation(true);

    const modalEl = document.getElementById('locationModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) modal.hide();

    reloadLocationContext();
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
