(function () {
    "use strict";

    function gerenciarCampoDecimal(idTela, idReal) {
        const campoTela = document.getElementById(idTela);
        const campoReal = document.getElementById(idReal);
        if (!campoTela || !campoReal) return;

        campoReal.value = campoTela.value;
        campoTela.addEventListener("input", function (e) {
            const digits = e.target.value.replace(/\D/g, "");
            const formatted = digits.length > 0
                ? (parseFloat(digits) / 100).toFixed(2).replace(".", ",")
                : "0,00";
            e.target.value = formatted;
            campoReal.value = formatted;
        });
    }

    function inicializarCep() {
        const cepTela = document.getElementById("Cep_Tela");
        const cepReal = document.getElementById("Cep_Real");
        if (!cepTela || !cepReal) return;

        function sincronizarCep() {
            const digits = cepTela.value.replace(/\D/g, "").slice(0, 8);
            cepReal.value = digits;
            cepTela.value = digits.length > 5
                ? `${digits.slice(0, 5)}-${digits.slice(5)}`
                : digits;
        }

        sincronizarCep();
        cepTela.addEventListener("input", sincronizarCep);

        const form = document.getElementById("formGestorEditarEvento");
        if (form) {
            form.addEventListener("submit", sincronizarCep);
        }
    }

    function inicializarBadgePortal() {
        const switchPublico = document.getElementById("EventoPublico");
        const badge = document.getElementById("badgePublicacaoPortal");
        const lbl = document.getElementById("lblPublicacaoPortal");
        if (!switchPublico || !badge || !lbl) return;

        function atualizarBadge() {
            const icone = badge.querySelector("i");
            if (switchPublico.checked) {
                badge.classList.remove("gestor-pill-rascunho");
                badge.classList.add("gestor-pill-publicado");
                lbl.textContent = "Publicado no Portal";
                if (icone) icone.className = "bi bi-broadcast";
            } else {
                badge.classList.remove("gestor-pill-publicado");
                badge.classList.add("gestor-pill-rascunho");
                lbl.textContent = "Oculto do Portal";
                if (icone) icone.className = "bi bi-eye-slash";
            }
        }

        switchPublico.addEventListener("change", atualizarBadge);
    }

    function inicializarUploadCapa() {
        const btn = document.getElementById("btnSelecionarCapa");
        const input = document.getElementById("fileInput");
        const thumbImg = document.getElementById("thumbCapaImg");
        const thumbPlaceholder = document.getElementById("thumbCapaPlaceholder");
        const lblNome = document.getElementById("lblNomeCapa");
        const lblInfo = document.getElementById("lblInfoCapa");
        const alerta = document.getElementById("alertaTamanhoCapa");
        if (!btn || !input) return;

        btn.addEventListener("click", function () {
            input.click();
        });

        input.addEventListener("change", function () {
            const file = this.files && this.files[0];
            if (!file) return;

            if (file.size > 65535) {
                if (alerta) alerta.classList.remove("d-none");
                this.value = "";
                return;
            }

            if (alerta) alerta.classList.add("d-none");
            const kb = Math.max(1, Math.round(file.size / 1024));
            if (lblNome) {
                lblNome.textContent = file.name;
                lblNome.title = file.name;
            }
            if (lblInfo) lblInfo.textContent = `${kb} KB`;
            const lblBotao = btn.querySelector("span");
            if (lblBotao) lblBotao.textContent = "Trocar";

            const reader = new FileReader();
            reader.onload = function (e) {
                const dataUrl = e.target.result;
                if (thumbImg) {
                    thumbImg.src = dataUrl;
                    thumbImg.classList.remove("d-none");
                }
                if (thumbPlaceholder) {
                    thumbPlaceholder.classList.add("d-none");
                }

                const imgProbe = new Image();
                imgProbe.onload = function () {
                    if (lblInfo) {
                        lblInfo.textContent = `${imgProbe.naturalWidth}×${imgProbe.naturalHeight} px • ${kb} KB`;
                    }
                };
                imgProbe.src = dataUrl;
            };
            reader.readAsDataURL(file);
        });
    }

    function inicializarAreasInteresse() {
        const select = document.getElementById("areaInteresseSelect");
        const btnAdd = document.getElementById("addAreaButton");
        const container = document.getElementById("selectedAreas");
        const hintVazio = document.getElementById("areasVaziasHint");
        if (!select || !btnAdd || !container) return;

        function atualizarHintVazio() {
            if (!hintVazio) return;
            const possuiChips = container.querySelectorAll(".gestor-tag-chip").length > 0;
            hintVazio.classList.toggle("d-none", possuiChips);
        }

        function criarChip(id, nome) {
            const chip = document.createElement("span");
            chip.className = "gestor-tag-chip";
            chip.setAttribute("data-id", id);

            const texto = document.createElement("span");
            texto.textContent = nome;

            const hidden = document.createElement("input");
            hidden.type = "hidden";
            hidden.name = "IdAreaInteresses";
            hidden.value = id;

            const btnRemover = document.createElement("button");
            btnRemover.type = "button";
            btnRemover.className = "gestor-tag-remove";
            btnRemover.title = `Remover ${nome}`;
            btnRemover.setAttribute("aria-label", `Remover ${nome}`);
            btnRemover.innerHTML = '<i class="bi bi-x"></i>';

            chip.appendChild(texto);
            chip.appendChild(hidden);
            chip.appendChild(btnRemover);
            return chip;
        }

        btnAdd.addEventListener("click", function () {
            const option = select.options[select.selectedIndex];
            const value = option ? option.value : "";
            const text = option ? option.text : "";

            if (!value) return;
            if (container.querySelector(`.gestor-tag-chip[data-id="${value}"]`)) {
                select.value = "";
                return;
            }

            container.appendChild(criarChip(value, text));
            select.value = "";
            atualizarHintVazio();
        });

        container.addEventListener("click", function (e) {
            const btnRemover = e.target.closest(".gestor-tag-remove");
            if (!btnRemover) return;
            const chip = btnRemover.closest(".gestor-tag-chip");
            if (chip) {
                chip.remove();
                atualizarHintVazio();
            }
        });

        atualizarHintVazio();
    }

    function inicializarFiltroSubeventos() {
        const filtroInput = document.getElementById("filtroSubeventos");
        const linhas = document.querySelectorAll("#tabelaSubeventos tbody tr.linha-subevento");
        const linhaVazia = document.getElementById("linhaSemSubeventoFiltro");
        const lblVisiveis = document.getElementById("lblSubeventosVisiveis");
        if (!filtroInput || linhas.length === 0) return;

        filtroInput.addEventListener("input", function () {
            const termo = this.value.trim().toLowerCase();
            let visiveis = 0;

            linhas.forEach(function (tr) {
                const texto = tr.getAttribute("data-busca") || "";
                const corresponde = termo === "" || texto.includes(termo);
                tr.classList.toggle("d-none", !corresponde);
                if (corresponde) visiveis++;
            });

            if (linhaVazia) {
                linhaVazia.classList.toggle("d-none", visiveis > 0);
            }
            if (lblVisiveis) {
                lblVisiveis.textContent = visiveis.toString();
            }
        });
    }

    function inicializarAlertasGestor() {
        if (!document.getElementById("alerterror") && !document.getElementById("alerterror2")) return;
        setTimeout(function () {
            if (window.jQuery) {
                $("#alerterror, #alerterror2").fadeOut("slow");
            }
        }, 3500);
    }

    document.addEventListener("DOMContentLoaded", function () {
        gerenciarCampoDecimal("ValorInscricao_Tela", "ValorInscricao_Real");
        gerenciarCampoDecimal("FrequenciaMinimaCertificado_Tela", "FrequenciaMinimaCertificado_Real");
        inicializarCep();
        inicializarBadgePortal();
        inicializarUploadCapa();
        inicializarAreasInteresse();
        inicializarFiltroSubeventos();
        inicializarAlertasGestor();
    });
})();
