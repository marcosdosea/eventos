(function () {
    "use strict";

    function inicializarTabelaEventos() {
        const tableEl = document.getElementById("tableEvento");
        if (!tableEl) return;

        const filtroInput = document.getElementById("filtroEventos");
        const filtroStatus = document.getElementById("filtroStatusEvento");
        const btnLimpar = document.getElementById("btnLimparFiltroEventos");
        const lblInicioFim = document.getElementById("lblEventosInicioFim");
        const lblTotal = document.getElementById("lblEventosTotal");
        const paginacaoNav = document.getElementById("paginacaoEventosNav");

        function atualizarBotaoLimpar() {
            if (!btnLimpar) return;
            const temTexto = filtroInput && filtroInput.value.trim().length > 0;
            btnLimpar.classList.toggle("d-none", !temTexto);
        }

        function criarBotaoPagina(label, disabled, active, onClick) {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "page-btn" + (active ? " active" : "");
            btn.textContent = label;
            btn.disabled = disabled;
            if (!disabled && typeof onClick === "function") {
                btn.addEventListener("click", onClick);
            }
            return btn;
        }

        function renderizarPaginacao(paginaAtual, totalPaginas, aoMudarPagina) {
            if (!paginacaoNav) return;
            paginacaoNav.innerHTML = "";

            const paginasTotais = Math.max(1, totalPaginas);
            paginacaoNav.appendChild(
                criarBotaoPagina("Anterior", paginaAtual <= 0, false, function () {
                    aoMudarPagina(paginaAtual - 1);
                })
            );

            const inicioJanela = Math.max(0, Math.min(paginaAtual - 2, paginasTotais - 5));
            const fimJanela = Math.min(paginasTotais - 1, inicioJanela + 4);

            for (let i = inicioJanela; i <= fimJanela; i++) {
                paginacaoNav.appendChild(
                    criarBotaoPagina(String(i + 1), false, i === paginaAtual, function () {
                        aoMudarPagina(i);
                    })
                );
            }

            paginacaoNav.appendChild(
                criarBotaoPagina("Próximo", paginaAtual >= paginasTotais - 1, false, function () {
                    aoMudarPagina(paginaAtual + 1);
                })
            );
        }

        function atualizarResumo(inicio, fim, total) {
            if (lblInicioFim) {
                lblInicioFim.textContent = total === 0 ? "0" : `${inicio}-${fim}`;
            }
            if (lblTotal) {
                lblTotal.textContent = String(total);
            }
        }

        if (window.jQuery && window.jQuery.fn && window.jQuery.fn.DataTable) {
            const $ = window.jQuery;

            $.fn.dataTable.ext.search.push(function (settings, _data, dataIndex) {
                if (!settings || settings.nTable !== tableEl) return true;
                if (!filtroStatus || !filtroStatus.value) return true;

                const rowNode = settings.aoData[dataIndex]?.nTr;
                const statusLinha = rowNode ? (rowNode.getAttribute("data-status") || "") : "";
                return statusLinha === filtroStatus.value;
            });

            const dt = $(tableEl).DataTable({
                dom: "t",
                pageLength: 10,
                ordering: true,
                order: [[0, "desc"]],
                columnDefs: [
                    { orderable: true, targets: [0, 1, 2, 3] },
                    { orderable: false, targets: [4] }
                ],
                language: {
                    emptyTable: "Nenhum evento cadastrado no sistema.",
                    zeroRecords: "Nenhum evento encontrado para os filtros informados."
                }
            });

            function sincronizarRodapeDataTable() {
                const info = dt.page.info();
                const exibidos = info.recordsDisplay;
                const inicio = exibidos === 0 ? 0 : info.start + 1;
                const fim = exibidos === 0 ? 0 : info.end;

                atualizarResumo(inicio, fim, exibidos);
                renderizarPaginacao(info.page, info.pages, function (novaPagina) {
                    dt.page(novaPagina).draw("page");
                });
            }

            dt.on("draw", sincronizarRodapeDataTable);

            if (filtroInput) {
                filtroInput.addEventListener("input", function () {
                    atualizarBotaoLimpar();
                    dt.search(this.value).draw();
                });
            }

            if (filtroStatus) {
                filtroStatus.addEventListener("change", function () {
                    dt.draw();
                });
            }

            if (btnLimpar && filtroInput) {
                btnLimpar.addEventListener("click", function () {
                    filtroInput.value = "";
                    atualizarBotaoLimpar();
                    dt.search("").draw();
                    filtroInput.focus();
                });
            }

            atualizarBotaoLimpar();
            sincronizarRodapeDataTable();
            return;
        }

        // Fallback em JavaScript puro caso DataTables não esteja carregado
        const tbody = tableEl.querySelector("tbody");
        if (!tbody) return;
        const linhas = Array.from(tbody.querySelectorAll("tr.linha-evento"));
        const linhaVaziaFiltro = document.createElement("tr");
        linhaVaziaFiltro.id = "linhaSemEventoFiltro";
        linhaVaziaFiltro.className = "d-none";
        linhaVaziaFiltro.innerHTML = '<td colspan="5" class="dataTables_empty">Nenhum evento encontrado para os filtros informados.</td>';
        tbody.appendChild(linhaVaziaFiltro);
        const pageLength = 10;
        let paginaAtual = 0;

        function aplicarFiltroFallback() {
            const termo = filtroInput ? filtroInput.value.trim().toLowerCase() : "";
            const statusSelecionado = filtroStatus ? filtroStatus.value : "";

            const filtradas = linhas.filter(function (tr) {
                const busca = tr.getAttribute("data-busca") || "";
                const status = tr.getAttribute("data-status") || "";
                const bateTermo = !termo || busca.includes(termo);
                const bateStatus = !statusSelecionado || status === statusSelecionado;
                return bateTermo && bateStatus;
            });

            const total = filtradas.length;
            const totalPaginas = Math.max(1, Math.ceil(total / pageLength));
            if (paginaAtual >= totalPaginas) {
                paginaAtual = totalPaginas - 1;
            }

            const inicioIdx = paginaAtual * pageLength;
            const fimIdx = Math.min(inicioIdx + pageLength, total);

            linhas.forEach(function (tr) {
                tr.classList.add("d-none");
            });

            for (let i = inicioIdx; i < fimIdx; i++) {
                filtradas[i].classList.remove("d-none");
            }

            if (linhaVaziaFiltro) {
                linhaVaziaFiltro.classList.toggle("d-none", total > 0);
            }

            atualizarResumo(total === 0 ? 0 : inicioIdx + 1, fimIdx, total);
            renderizarPaginacao(paginaAtual, totalPaginas, function (novaPagina) {
                paginaAtual = novaPagina;
                aplicarFiltroFallback();
            });
        }

        if (filtroInput) {
            filtroInput.addEventListener("input", function () {
                paginaAtual = 0;
                atualizarBotaoLimpar();
                aplicarFiltroFallback();
            });
        }

        if (filtroStatus) {
            filtroStatus.addEventListener("change", function () {
                paginaAtual = 0;
                aplicarFiltroFallback();
            });
        }

        if (btnLimpar && filtroInput) {
            btnLimpar.addEventListener("click", function () {
                filtroInput.value = "";
                paginaAtual = 0;
                atualizarBotaoLimpar();
                aplicarFiltroFallback();
                filtroInput.focus();
            });
        }

        atualizarBotaoLimpar();
        aplicarFiltroFallback();
    }

    function inicializarEditStatusFeedback() {
        const selectStatus = document.getElementById("Status");
        const badgeHeader = document.getElementById("badgeStatusHeader");
        const lblStatusHeader = document.getElementById("lblStatusHeader");
        const badgeBox = document.getElementById("badgeStatusBox");
        const descBox = document.getElementById("descStatusBox");
        if (!selectStatus || (!badgeHeader && !badgeBox)) return;

        const mapaStatus = {
            C: {
                nome: "Cadastro",
                classe: "evento-pill-cadastro",
                descricao: "Em preparação administrativa. O evento não é exibido na vitrine pública até que o Gestor finalize as configurações e ative a publicação."
            },
            A: {
                nome: "Ativo",
                classe: "evento-pill-ativo",
                descricao: "Evento ativo no sistema. Quando marcado como público pelo Gestor e dentro do período vigente, fica disponível na vitrine do portal."
            },
            F: {
                nome: "Finalizado",
                classe: "evento-pill-finalizado",
                descricao: "Evento encerrado. As inscrições são bloqueadas e o cálculo final de frequência e certificados passa a ser consolidado."
            }
        };

        function atualizarStatusVisual() {
            const info = mapaStatus[selectStatus.value] || mapaStatus.C;

            if (badgeHeader) {
                badgeHeader.classList.remove("evento-pill-cadastro", "evento-pill-ativo", "evento-pill-finalizado", "evento-pill-inativo");
                badgeHeader.classList.add(info.classe);
            }
            if (lblStatusHeader) {
                lblStatusHeader.textContent = `Status: ${info.nome}`;
            }
            if (badgeBox) {
                badgeBox.textContent = info.nome;
            }
            if (descBox) {
                descBox.textContent = info.descricao;
            }
        }

        selectStatus.addEventListener("change", atualizarStatusVisual);
        atualizarStatusVisual();
    }

    function inicializarModalExclusaoEvento() {
        const modal = document.getElementById("modalExcluirEvento");
        if (!modal) return;

        const inputId = document.getElementById("excluirEventoId");
        const lblNome = document.getElementById("excluirEventoNome");

        modal.addEventListener("show.bs.modal", function (event) {
            const botao = event.relatedTarget;
            const eventoId = botao ? botao.getAttribute("data-evento-id") : null;
            const eventoNome = botao ? botao.getAttribute("data-evento-nome") : "";

            if (!eventoId) {
                event.preventDefault();
                return;
            }

            if (inputId) {
                inputId.value = eventoId;
            }
            if (lblNome) {
                lblNome.textContent = eventoNome || "";
            }
        });
    }

    function inicializarValidacaoImediataFormulario() {
        if (!window.jQuery || !window.jQuery.fn || !window.jQuery.fn.validate) return;
        const $ = window.jQuery;
        const $forms = $("#formEventoCreate, #formEventoEdit");
        if (!$forms.length) return;

        $forms.on("input change", "input, select", function () {
            const $form = $(this).closest("form");
            const validator = $form.data("validator");
            if (validator && (validator.submitted[this.name] || $(this).hasClass("input-validation-error"))) {
                validator.element(this);
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        inicializarTabelaEventos();
        inicializarModalExclusaoEvento();
        inicializarEditStatusFeedback();
        inicializarValidacaoImediataFormulario();
    });
})();
