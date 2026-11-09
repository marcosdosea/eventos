/**
 * emitircracha.js
 * Orquestrador principal da interface de Emissão e Pré-visualização de Crachás.
 * Gerencia filtragem por texto e por cargo, paginação, seleção em lote,
 * simulação 1:1 e diálogo modal para edição rápida de participantes.
 */
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        const selectEvento = document.getElementById("selectEventoEmitir");
        const selectModelo = document.getElementById("selectModeloEmitir");
        const btnImprimirLote = document.getElementById("btnImprimirLotePdf");

        const chkMaster = document.getElementById("chkSelecionarTodosHeader");
        const btnSelecionarTodosPagina = document.getElementById("btnSelecionarTodosPagina");
        const btnLimparSelecao = document.getElementById("btnLimparSelecao");

        const filtroCargo = document.getElementById("filtroCargoParticipante");
        const filtroInput = document.getElementById("filtroParticipantes");
        const btnLimparBusca = document.getElementById("btnLimparBusca");
        const tabelaBody = document.getElementById("tabelaParticipantesBody");

        const lblQtdSelecionados = document.getElementById("lblQtdSelecionados");
        const lblQtdTotal = document.getElementById("lblQtdTotal");

        const lblTextoPaginacao = document.getElementById("lblTextoPaginacao");
        const btnPaginaAnt = document.getElementById("btnPaginaAnt");
        const btnPaginaProx = document.getElementById("btnPaginaProx");

        const crachaCardElement = document.getElementById("crachaCardElement");
        const previewFoto = document.getElementById("previewFotoElement");
        const previewAvatarPlaceholder = document.getElementById("previewAvatarPlaceholder");
        const previewNome = document.getElementById("previewNomeElement");
        const previewOrg = document.getElementById("previewOrgElement");
        const previewPapelTexto = document.getElementById("previewPapelTexto");
        const previewPapelIcon = document.getElementById("previewPapelIcon");
        const previewCpf = document.getElementById("previewCpfElement");
        const previewCredencial = document.getElementById("previewCredencialElement");
        const previewCodigoPresenca = document.getElementById("previewCodigoPresenca");
        const previewQrCodeImg = document.getElementById("previewQrCodeImg");
        const previewQrPlaceholder = document.getElementById("previewQrPlaceholderSvg");
        const lblTextoCracha = document.getElementById("lblPreviewTextoCracha");
        const blocoQrCode = document.getElementById("blocoQrCodePreview");

        const btnExportarPng = document.getElementById("btnExportarPng");
        const btnExportarLotePng = document.getElementById("btnExportarLotePng");
        const btnZoomIn = document.getElementById("btnZoomIn");
        const btnZoomOut = document.getElementById("btnZoomOut");
        const btnFullscreen = document.getElementById("btnFullscreenPreview");

        const containerImpressaoLote = document.getElementById("containerImpressaoLoteA4");

        const modalEditarEl = document.getElementById("modalEditarParticipanteCracha");
        const formEditarParticipante = document.getElementById("formEditarParticipanteCracha");
        const editModalIdEvento = document.getElementById("editModalIdEvento");
        const editModalIdPessoa = document.getElementById("editModalIdPessoa");
        const editModalNomeCompleto = document.getElementById("editModalNomeCompleto");
        const editModalNomeCracha = document.getElementById("editModalNomeCracha");
        const contadorCaracteresCracha = document.getElementById("contadorCaracteresCracha");
        const btnSalvarEdicao = document.getElementById("btnSalvarEdicaoParticipante");
        const spinnerSalvarEdicao = document.getElementById("spinnerSalvarEdicao");
        const iconeSalvarEdicao = document.getElementById("iconeSalvarEdicao");

        let linhas = tabelaBody ? Array.from(tabelaBody.querySelectorAll(".linha-participante")) : [];
        let linhasFiltradas = [...linhas];

        const ITENS_POR_PAGINA = 12;
        let paginaAtual = 1;
        let escalaAtual = 1;

        const CanvasModule = window.CrachaCanvas || {};
        const ExportModule = window.CrachaExport || {};

        function exibirMensagemFeedback(mensagem, tipo = "success", containerId = "alertaCrachaContainer") {
            const icone = tipo === "success"
                ? "bi-check-circle-fill text-success"
                : (tipo === "warning" ? "bi-exclamation-triangle-fill text-warning" : "bi-exclamation-octagon-fill text-danger");

            const container = document.getElementById(containerId) || document.getElementById("alertaCrachaContainer");
            if (!container) return;

            const alertDiv = document.createElement("div");
            alertDiv.className = `alert alert-${tipo} alert-dismissible fade show d-flex align-items-center shadow-sm rounded-3 py-2 px-3 mb-3`;
            alertDiv.setAttribute("role", "alert");
            alertDiv.innerHTML = `
                <i class="bi ${icone} fs-5 me-2"></i>
                <span class="fw-semibold small">${mensagem}</span>
                <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert" aria-label="Fechar"></button>
            `;

            container.innerHTML = "";
            container.appendChild(alertDiv);

            setTimeout(function () {
                if (alertDiv && alertDiv.parentNode) {
                    alertDiv.classList.remove("show");
                    setTimeout(function () {
                        if (alertDiv.parentNode) {
                            alertDiv.remove();
                        }
                    }, 300);
                }
            }, 4000);
        }

        function obterIniciais(nome) {
            if (CanvasModule.obterIniciais) {
                return CanvasModule.obterIniciais(nome);
            }
            if (!nome || typeof nome !== "string") return "PA";
            const limpo = nome.trim();
            if (!limpo) return "PA";
            return limpo.substring(0, Math.min(2, limpo.length)).toUpperCase();
        }

        function ehPayloadImagemValido(src) {
            if (CanvasModule.ehPayloadImagemValido) {
                return CanvasModule.ehPayloadImagemValido(src);
            }
            if (!src || typeof src !== "string") return false;
            const limpo = src.trim();
            return limpo.length > 0;
        }

        function popularFiltroCargosSeNecessario() {
            if (!filtroCargo) return;
            const opcoesExistentes = new Set(
                Array.from(filtroCargo.options).map(opt => opt.value.toLowerCase().trim())
            );

            const cargosEncontrados = new Set();
            linhas.forEach(linha => {
                const papel = (linha.dataset.papel || "").trim();
                if (papel && !cargosEncontrados.has(papel.toLowerCase())) {
                    cargosEncontrados.add(papel.toLowerCase());
                    if (!opcoesExistentes.has(papel.toLowerCase())) {
                        const opt = document.createElement("option");
                        opt.value = papel;
                        opt.textContent = papel;
                        filtroCargo.appendChild(opt);
                    }
                }
            });
        }

        function degradarFotoDaLinhaParaPlaceholder(linha) {
            if (!linha) return;
            linha.dataset.foto = "";
            const thumbBox = linha.querySelector(".participant-thumb-box");
            if (thumbBox) {
                const img = thumbBox.querySelector("img");
                if (img) {
                    img.style.display = "none";
                    img.removeAttribute("src");
                }
                let divIniciais = thumbBox.querySelector(".avatar-iniciais");
                if (!divIniciais) {
                    divIniciais = document.createElement("div");
                    divIniciais.className = "avatar-iniciais";
                    thumbBox.appendChild(divIniciais);
                }
                divIniciais.textContent = obterIniciais(linha.dataset.nome || linha.dataset.nomeCompleto);
                divIniciais.style.display = "";
            }
        }

        function configurarResilienciaImagens() {
            linhas.forEach(linha => {
                const fotoData = (linha.dataset.foto || "").trim();
                const thumbImg = linha.querySelector(".participant-thumb-box img");

                if (fotoData && !ehPayloadImagemValido(fotoData)) {
                    degradarFotoDaLinhaParaPlaceholder(linha);
                } else if (thumbImg) {
                    thumbImg.addEventListener("error", function () {
                        degradarFotoDaLinhaParaPlaceholder(linha);
                        if (linha.classList.contains("linha-ativa-preview")) {
                            exibirAvatarPlaceholderNoPreview();
                        }
                    });
                    if (thumbImg.complete && thumbImg.naturalWidth === 0) {
                        degradarFotoDaLinhaParaPlaceholder(linha);
                    }
                }
            });

            if (previewFoto) {
                previewFoto.addEventListener("error", function () {
                    exibirAvatarPlaceholderNoPreview();
                    const linhaAtiva = document.querySelector(".linha-ativa-preview");
                    if (linhaAtiva) {
                        degradarFotoDaLinhaParaPlaceholder(linhaAtiva);
                    }
                });
            }

            const crachaLogoElement = document.getElementById("crachaLogoElement");
            const crachaLogoContainer = document.getElementById("crachaLogoContainer");
            if (crachaLogoElement && crachaLogoContainer) {
                crachaLogoElement.addEventListener("error", function () {
                    crachaLogoElement.removeAttribute("src");
                    crachaLogoContainer.style.display = "none";
                });
            }
        }

        function exibirAvatarPlaceholderNoPreview() {
            if (previewFoto) {
                previewFoto.removeAttribute("src");
                previewFoto.style.display = "none";
            }
            if (previewAvatarPlaceholder) {
                previewAvatarPlaceholder.style.display = "block";
            }
        }

        function atualizarContadores() {
            if (!lblQtdSelecionados || !lblQtdTotal) return;
            const selecionados = linhas.filter(l => {
                const chk = l.querySelector(".chk-participante-item");
                return chk && chk.checked;
            }).length;
            lblQtdSelecionados.textContent = selecionados.toString();
            lblQtdTotal.textContent = linhas.length.toString();

            if (chkMaster) {
                const alvoLinhas = (linhasFiltradas && linhasFiltradas.length > 0) ? linhasFiltradas : linhas;
                if (alvoLinhas.length === 0) {
                    chkMaster.checked = false;
                    chkMaster.indeterminate = false;
                } else {
                    const marcados = alvoLinhas.filter(l => l.querySelector(".chk-participante-item")?.checked).length;
                    if (marcados === alvoLinhas.length) {
                        chkMaster.checked = true;
                        chkMaster.indeterminate = false;
                    } else if (marcados > 0) {
                        chkMaster.checked = false;
                        chkMaster.indeterminate = true;
                    } else {
                        chkMaster.checked = false;
                        chkMaster.indeterminate = false;
                    }
                }
            }
        }

        function aplicarPaginacao() {
            const totalPaginas = Math.max(1, Math.ceil(linhasFiltradas.length / ITENS_POR_PAGINA));
            if (paginaAtual > totalPaginas) paginaAtual = totalPaginas;
            if (paginaAtual < 1) paginaAtual = 1;

            const inicio = (paginaAtual - 1) * ITENS_POR_PAGINA;
            const fim = inicio + ITENS_POR_PAGINA;

            linhas.forEach(l => l.style.display = "none");

            linhasFiltradas.forEach((linha, index) => {
                if (index >= inicio && index < fim) {
                    linha.style.display = "";
                } else {
                    linha.style.display = "none";
                }
            });

            if (lblTextoPaginacao) {
                lblTextoPaginacao.textContent = `Página ${paginaAtual} de ${totalPaginas}`;
            }

            if (btnPaginaAnt) btnPaginaAnt.disabled = (paginaAtual <= 1);
            if (btnPaginaProx) btnPaginaProx.disabled = (paginaAtual >= totalPaginas);

            const linhaSemResultados = document.getElementById("linhaSemResultadosBusca");
            if (linhaSemResultados) {
                if (linhas.length > 0 && linhasFiltradas.length === 0) {
                    linhaSemResultados.style.display = "";
                } else {
                    linhaSemResultados.style.display = "none";
                }
            }
        }

        function filtrarParticipantes(preservarPagina = false) {
            const cargoFiltro = (filtroCargo?.value || "").toLowerCase().trim();
            const termo = (filtroInput?.value || "").toLowerCase().trim();

            linhasFiltradas = linhas.filter(linha => {
                if (cargoFiltro) {
                    const papel = (linha.dataset.papel || "").toLowerCase().trim();
                    if (papel !== cargoFiltro) {
                        return false;
                    }
                }

                if (!termo) {
                    return true;
                }

                const nome = (linha.dataset.nome || "").toLowerCase();
                const nomeCompleto = (linha.dataset.nomeCompleto || "").toLowerCase();
                const depto = (linha.dataset.depto || "").toLowerCase();
                const cpf = (linha.dataset.cpf || "").toLowerCase();
                const cpfPuro = (linha.dataset.cpfPuro || "").toLowerCase();
                const credencial = (linha.dataset.credencial || "").toLowerCase();
                const papelLinha = (linha.dataset.papel || "").toLowerCase();

                return nome.includes(termo) ||
                    nomeCompleto.includes(termo) ||
                    depto.includes(termo) ||
                    cpf.includes(termo) ||
                    cpfPuro.includes(termo) ||
                    credencial.includes(termo) ||
                    papelLinha.includes(termo);
            });

            if (!preservarPagina) {
                paginaAtual = 1;
            }
            aplicarPaginacao();
            atualizarContadores();

            const linhaAtiva = document.querySelector(".linha-ativa-preview");
            if (linhaAtiva && !linhasFiltradas.includes(linhaAtiva)) {
                if (linhasFiltradas.length > 0) {
                    atualizarPreviewComLinha(linhasFiltradas[0]);
                }
            }
        }

        function atualizarPreviewComLinha(linha) {
            if (!linha) return;

            linhas.forEach(l => l.classList.remove("linha-ativa-preview"));
            linha.classList.add("linha-ativa-preview");

            const nome = linha.dataset.nome || linha.dataset.nomeCompleto || "PARTICIPANTE";
            const depto = linha.dataset.depto || "";
            const papel = linha.dataset.papel || "PARTICIPANTE";
            const cpf = linha.dataset.cpf || "***.***.***-**";
            const credencial = linha.dataset.credencial || "#00000";
            const presenca = linha.dataset.presenca || "BL - EVT - 2025 - 00000";
            const qrcodeB64 = linha.dataset.qrcode || "";
            const fotoUrl = (linha.dataset.foto || "").trim();

            if (previewNome) previewNome.textContent = nome.toUpperCase();
            if (previewOrg) previewOrg.textContent = depto;
            if (previewPapelTexto) previewPapelTexto.textContent = papel.toUpperCase();

            if (previewPapelIcon) {
                const papelUpper = papel.toUpperCase();
                if (papelUpper.includes("PALESTRANTE")) {
                    previewPapelIcon.className = "bi bi-megaphone-fill me-1";
                } else if (papelUpper.includes("GESTOR") || papelUpper.includes("COORDENAÇÃO")) {
                    previewPapelIcon.className = "bi bi-shield-check me-1";
                } else if (papelUpper.includes("COLABORADOR") || papelUpper.includes("TI")) {
                    previewPapelIcon.className = "bi bi-tools me-1";
                } else {
                    previewPapelIcon.className = "bi bi-person-fill me-1";
                }
            }

            if (previewCpf) previewCpf.textContent = cpf;
            if (previewCredencial) previewCredencial.textContent = credencial;
            if (previewCodigoPresenca) previewCodigoPresenca.textContent = presenca;

            if (ehPayloadImagemValido(fotoUrl)) {
                if (previewAvatarPlaceholder) {
                    previewAvatarPlaceholder.style.display = "none";
                }
                if (previewFoto) {
                    previewFoto.style.display = "block";
                    previewFoto.src = fotoUrl;
                }
            } else {
                if (fotoUrl !== "") {
                    degradarFotoDaLinhaParaPlaceholder(linha);
                }
                exibirAvatarPlaceholderNoPreview();
            }

            if (qrcodeB64 && qrcodeB64.trim() !== "") {
                if (previewQrCodeImg) {
                    previewQrCodeImg.src = qrcodeB64.startsWith("data:") ? qrcodeB64 : `data:image/png;base64,${qrcodeB64}`;
                    previewQrCodeImg.style.display = "block";
                }
                if (previewQrPlaceholder) {
                    previewQrPlaceholder.style.display = "none";
                }
            } else {
                if (previewQrCodeImg) {
                    previewQrCodeImg.removeAttribute("src");
                    previewQrCodeImg.style.display = "none";
                }
                if (previewQrPlaceholder) {
                    previewQrPlaceholder.style.display = "block";
                }
            }

            const apto = (linha.dataset.apto === "true");
            const statusDesc = linha.dataset.statusDesc || (apto ? "Apto" : "Pendente Foto");
            const badgeVerified = document.querySelector(".participant-verified-badge");
            if (badgeVerified) {
                if (apto) {
                    badgeVerified.style.backgroundColor = "#10B981";
                    badgeVerified.title = "Participante Apto para Emissão";
                    badgeVerified.innerHTML = '<i class="bi bi-check-lg"></i>';
                } else {
                    badgeVerified.style.backgroundColor = "#F59E0B";
                    badgeVerified.title = statusDesc;
                    badgeVerified.innerHTML = '<i class="bi bi-clock-history"></i>';
                }
            }
        }

        function atualizarContadorCaracteresModal() {
            if (!contadorCaracteresCracha || !editModalNomeCracha) return;
            const len = (editModalNomeCracha.value || "").length;
            contadorCaracteresCracha.textContent = `${len}/20`;
        }

        function abrirModalEdicaoParticipante(linha) {
            if (!linha || !modalEditarEl) return;

            atualizarPreviewComLinha(linha);

            const containerAlertaModal = document.getElementById("alertaModalCrachaContainer");
            if (containerAlertaModal) {
                containerAlertaModal.innerHTML = "";
            }

            const idPessoa = linha.dataset.idPessoa || "0";
            const idEvento = selectEvento?.value || editModalIdEvento?.value || "0";
            const nomeCrachaAtual = linha.dataset.nome || "";
            const nomeCompletoAtual = linha.dataset.nomeCompleto || nomeCrachaAtual;

            if (editModalIdPessoa) editModalIdPessoa.value = idPessoa;
            if (editModalIdEvento) editModalIdEvento.value = idEvento;
            if (editModalNomeCompleto) editModalNomeCompleto.value = nomeCompletoAtual;
            if (editModalNomeCracha) editModalNomeCracha.value = nomeCrachaAtual.substring(0, 20);

            atualizarContadorCaracteresModal();

            if (window.bootstrap && window.bootstrap.Modal) {
                const modalInstance = window.bootstrap.Modal.getOrCreateInstance(modalEditarEl);
                modalInstance.show();
            }
        }

        function aplicarDadosAtualizadosNaLinha(linha, dados) {
            if (!linha || !dados) return;

            const novoNomeCracha = (dados.nomeCracha || dados.NomeCracha || linha.dataset.nome || "").trim();
            const novoNomeCompleto = (dados.nomeCompleto || dados.NomeCompleto || linha.dataset.nomeCompleto || novoNomeCracha).trim();
            const novoQrCode = dados.qrCodeBase64 || dados.QrCodeBase64 || linha.dataset.qrcode || "";
            const novasIniciais = obterIniciais(novoNomeCracha);

            linha.dataset.nome = novoNomeCracha;
            linha.dataset.nomeCompleto = novoNomeCompleto;
            if (novoQrCode) {
                linha.dataset.qrcode = novoQrCode;
            }

            const elNomeTabela = linha.querySelector(".participante-nome-tabela");
            if (elNomeTabela) {
                elNomeTabela.textContent = novoNomeCracha;
            }

            const elIniciais = linha.querySelector(".avatar-iniciais");
            if (elIniciais) {
                elIniciais.textContent = novasIniciais;
            }

            const imgThumb = linha.querySelector(".participant-thumb-box img");
            if (imgThumb) {
                imgThumb.alt = novoNomeCracha;
            }

            const btnEditar = linha.querySelector(".btn-abrir-modal-editar");
            if (btnEditar) {
                btnEditar.dataset.nome = novoNomeCracha;
                btnEditar.dataset.nomeCompleto = novoNomeCompleto;
            }
        }

        function extrairDadosDaLinha(linha) {
            if (!linha) {
                return {
                    nome: (previewNome?.textContent || "PARTICIPANTE").trim(),
                    depto: (previewOrg?.textContent || "").trim(),
                    papel: (previewPapelTexto?.textContent || "PARTICIPANTE").trim(),
                    cpf: (previewCpf?.textContent || "***.***.***-**").trim(),
                    credencial: (previewCredencial?.textContent || "#00000").trim(),
                    presenca: (previewCodigoPresenca?.textContent || "BL - EVT - 2025 - 00142").trim(),
                    fotoUrl: (previewFoto && previewFoto.style.display !== "none" ? previewFoto.getAttribute("src") || "" : "").trim(),
                    qrcodeB64: (previewQrCodeImg && previewQrCodeImg.style.display !== "none" ? previewQrCodeImg.getAttribute("src") || "" : "").trim(),
                    apto: true
                };
            }

            return {
                nome: (linha.dataset.nome || linha.dataset.nomeCompleto || "PARTICIPANTE").trim(),
                depto: (linha.dataset.depto || "").trim(),
                papel: (linha.dataset.papel || "PARTICIPANTE").trim(),
                cpf: (linha.dataset.cpf || "***.***.***-**").trim(),
                credencial: (linha.dataset.credencial || "#00000").trim(),
                presenca: (linha.dataset.presenca || "BL - EVT - 2025 - 00000").trim(),
                fotoUrl: (linha.dataset.foto || "").trim(),
                qrcodeB64: (linha.dataset.qrcode || "").trim(),
                apto: (linha.dataset.apto === "true")
            };
        }

        function atualizarModeloSelecionado() {
            if (!selectModelo) return;
            const opt = selectModelo.options[selectModelo.selectedIndex];
            if (opt) {
                const texto = opt.dataset.texto;
                const qrcode = opt.dataset.qrcode;
                const logo = opt.dataset.logo;

                if (lblTextoCracha && texto) {
                    lblTextoCracha.textContent = texto;
                }

                if (blocoQrCode) {
                    blocoQrCode.style.display = (qrcode === "0" || qrcode === 0) ? "none" : "flex";
                }

                const crachaLogoElement = document.getElementById("crachaLogoElement");
                const crachaLogoContainer = document.getElementById("crachaLogoContainer");
                if (crachaLogoElement && crachaLogoContainer) {
                    if (logo && ehPayloadImagemValido(logo)) {
                        crachaLogoElement.src = logo;
                        crachaLogoContainer.style.display = "";
                    } else {
                        crachaLogoElement.removeAttribute("src");
                        crachaLogoContainer.style.display = "none";
                    }
                }
            }
        }

        // Configuração do formulário de edição rápida
        if (editModalNomeCracha) {
            editModalNomeCracha.addEventListener("input", atualizarContadorCaracteresModal);
        }

        if (formEditarParticipante) {
            formEditarParticipante.addEventListener("submit", async function (e) {
                e.preventDefault();

                const idPessoa = editModalIdPessoa?.value || "0";
                const nomeCompleto = (editModalNomeCompleto?.value || "").trim();
                let nomeCracha = (editModalNomeCracha?.value || "").trim();

                if (!nomeCompleto || nomeCompleto.length < 2) {
                    exibirMensagemFeedback("Informe o nome completo do participante (mínimo 2 caracteres).", "warning", "alertaModalCrachaContainer");
                    editModalNomeCompleto?.focus();
                    return;
                }

                if (!nomeCracha) {
                    nomeCracha = nomeCompleto.substring(0, 20).trim();
                } else if (nomeCracha.length > 20) {
                    exibirMensagemFeedback("O nome no crachá deve ter no máximo 20 caracteres.", "warning", "alertaModalCrachaContainer");
                    editModalNomeCracha?.focus();
                    return;
                }

                if (btnSalvarEdicao) btnSalvarEdicao.disabled = true;
                if (spinnerSalvarEdicao) spinnerSalvarEdicao.classList.remove("d-none");
                if (iconeSalvarEdicao) iconeSalvarEdicao.classList.add("d-none");

                try {
                    const formData = new FormData(formEditarParticipante);
                    formData.set("Nome", nomeCompleto);
                    formData.set("NomeCracha", nomeCracha);

                    const response = await fetch("/Modelocracha/AtualizarNomeCracha", {
                        method: "POST",
                        body: formData,
                        headers: {
                            "X-Requested-With": "XMLHttpRequest"
                        }
                    });

                    let resultado = null;
                    try {
                        resultado = await response.json();
                    } catch {
                        resultado = null;
                    }

                    if (!response.ok || !resultado || !resultado.sucesso) {
                        const msgErro = resultado?.mensagem || "Não foi possível atualizar os dados do participante. Tente novamente.";
                        exibirMensagemFeedback(msgErro, "danger", "alertaModalCrachaContainer");
                        return;
                    }

                    const linhasParticipante = linhas.filter(l => l.dataset.idPessoa === String(idPessoa));
                    linhasParticipante.forEach(l => aplicarDadosAtualizadosNaLinha(l, resultado));

                    filtrarParticipantes(true);

                    const linhaAtiva = document.querySelector(".linha-ativa-preview") || linhasParticipante[0];
                    if (linhaAtiva) {
                        atualizarPreviewComLinha(linhaAtiva);
                    }

                    if (modalEditarEl && window.bootstrap && window.bootstrap.Modal) {
                        const modalInstance = window.bootstrap.Modal.getInstance(modalEditarEl);
                        modalInstance?.hide();
                    }

                    exibirMensagemFeedback(resultado.mensagem || "Dados do participante atualizados com sucesso!", "success");
                } catch (err) {
                    console.error("Erro ao atualizar participante:", err);
                    exibirMensagemFeedback("Erro de comunicação ao salvar alterações do participante.", "danger", "alertaModalCrachaContainer");
                } finally {
                    if (btnSalvarEdicao) btnSalvarEdicao.disabled = false;
                    if (spinnerSalvarEdicao) spinnerSalvarEdicao.classList.add("d-none");
                    if (iconeSalvarEdicao) iconeSalvarEdicao.classList.remove("d-none");
                }
            });
        }

        // Delegação de eventos na tabela
        if (tabelaBody) {
            tabelaBody.addEventListener("click", function (e) {
                const btnPrevia = e.target.closest(".btn-ver-previa");
                if (btnPrevia) {
                    const linha = btnPrevia.closest(".linha-participante");
                    atualizarPreviewComLinha(linha);
                    return;
                }

                const btnEditar = e.target.closest(".btn-abrir-modal-editar");
                if (btnEditar) {
                    e.preventDefault();
                    const linha = btnEditar.closest(".linha-participante");
                    abrirModalEdicaoParticipante(linha);
                    return;
                }

                const btnExcluir = e.target.closest(".btn-excluir-item");
                if (btnExcluir) {
                    const linha = btnExcluir.closest(".linha-participante");
                    if (linha && confirm(`Deseja remover "${linha.dataset.nome || "este participante"}" da lista de emissão?`)) {
                        linha.remove();
                        linhas = Array.from(tabelaBody.querySelectorAll(".linha-participante"));
                        filtrarParticipantes(true);
                        atualizarContadores();
                        if (linha.classList.contains("linha-ativa-preview")) {
                            atualizarPreviewComLinha(linhasFiltradas[0] || linhas[0]);
                        }
                    }
                    return;
                }

                const chk = e.target.closest(".chk-participante-item");
                if (chk) {
                    atualizarContadores();
                }
            });
        }

        // Seleção de linhas
        if (chkMaster) {
            chkMaster.addEventListener("change", function () {
                const marcar = chkMaster.checked;
                const alvoLinhas = (linhasFiltradas && linhasFiltradas.length > 0) ? linhasFiltradas : linhas;
                alvoLinhas.forEach(l => {
                    const c = l.querySelector(".chk-participante-item");
                    if (c) c.checked = marcar;
                });
                chkMaster.indeterminate = false;
                atualizarContadores();
            });
        }

        if (btnSelecionarTodosPagina) {
            btnSelecionarTodosPagina.addEventListener("click", function () {
                const alvoLinhas = (linhasFiltradas && linhasFiltradas.length > 0) ? linhasFiltradas : linhas;
                alvoLinhas.forEach(l => {
                    const c = l.querySelector(".chk-participante-item");
                    if (c) c.checked = true;
                });
                if (chkMaster) {
                    chkMaster.checked = true;
                    chkMaster.indeterminate = false;
                }
                atualizarContadores();
            });
        }

        if (btnLimparSelecao) {
            btnLimparSelecao.addEventListener("click", function () {
                linhas.forEach(l => {
                    const c = l.querySelector(".chk-participante-item");
                    if (c) c.checked = false;
                });
                if (chkMaster) {
                    chkMaster.checked = false;
                    chkMaster.indeterminate = false;
                }
                atualizarContadores();
            });
        }

        // Filtro por cargo de emissão
        if (filtroCargo) {
            filtroCargo.addEventListener("change", function () {
                filtrarParticipantes(false);
            });
        }

        // Filtro por texto (nome, CPF, etc)
        if (filtroInput) {
            filtroInput.addEventListener("input", function () {
                filtrarParticipantes(false);
            });
        }

        if (btnLimparBusca) {
            btnLimparBusca.addEventListener("click", function () {
                if (filtroInput) filtroInput.value = "";
                if (filtroCargo) filtroCargo.value = "";
                filtrarParticipantes(false);
                filtroInput?.focus();
            });
        }

        // Paginação
        if (btnPaginaAnt) {
            btnPaginaAnt.addEventListener("click", function () {
                if (paginaAtual > 1) {
                    paginaAtual--;
                    aplicarPaginacao();
                }
            });
        }

        if (btnPaginaProx) {
            btnPaginaProx.addEventListener("click", function () {
                const totalPaginas = Math.ceil(linhasFiltradas.length / ITENS_POR_PAGINA);
                if (paginaAtual < totalPaginas) {
                    paginaAtual++;
                    aplicarPaginacao();
                }
            });
        }

        // Troca de Evento
        if (selectEvento) {
            selectEvento.addEventListener("change", function () {
                const idEvento = this.value;
                if (idEvento && idEvento !== "0") {
                    window.location.href = `/Modelocracha/EmitirCracha?idEvento=${idEvento}`;
                }
            });
        }

        // Troca de Modelo de crachá
        if (selectModelo) {
            selectModelo.addEventListener("change", atualizarModeloSelecionado);
        }

        // Zoom e tela cheia
        if (btnZoomIn && crachaCardElement) {
            btnZoomIn.addEventListener("click", function () {
                if (escalaAtual < 1.3) {
                    escalaAtual = Math.round((escalaAtual + 0.1) * 10) / 10;
                    crachaCardElement.style.transformOrigin = "top center";
                    crachaCardElement.style.transform = `scale(${escalaAtual})`;
                }
            });
        }

        if (btnZoomOut && crachaCardElement) {
            btnZoomOut.addEventListener("click", function () {
                if (escalaAtual > 0.7) {
                    escalaAtual = Math.round((escalaAtual - 0.1) * 10) / 10;
                    crachaCardElement.style.transformOrigin = "top center";
                    crachaCardElement.style.transform = `scale(${escalaAtual})`;
                }
            });
        }

        if (btnFullscreen) {
            btnFullscreen.addEventListener("click", function () {
                const container = document.getElementById("cardSimulacaoImpressao");
                if (!document.fullscreenElement) {
                    container?.requestFullscreen().catch(err => console.log(err));
                } else {
                    document.exitFullscreen();
                }
            });
        }

        // Exportação PNG individual
        if (btnExportarPng && crachaCardElement) {
            btnExportarPng.addEventListener("click", async function () {
                try {
                    const linhaAtiva = document.querySelector(".linha-ativa-preview");
                    const dados = extrairDadosDaLinha(linhaAtiva);
                    if (ExportModule.exportarCrachaIndividualPng) {
                        await ExportModule.exportarCrachaIndividualPng(dados);
                    } else {
                        throw new Error("Módulo de exportação indisponível.");
                    }
                } catch (err) {
                    console.error("Falha ao exportar imagem PNG do crachá:", err);
                    exibirMensagemFeedback(
                        ExportModule.MENSAGEM_ERRO_EXPORTACAO_PNG || "Não foi possível exportar o PNG do crachá.",
                        "danger"
                    );
                }
            });
        }

        // Exportação Lote PNG compactado em ZIP
        if (btnExportarLotePng) {
            btnExportarLotePng.addEventListener("click", async function () {
                const linhasSelecionadas = linhas.filter(l => l.querySelector(".chk-participante-item")?.checked);
                if (linhasSelecionadas.length === 0) {
                    exibirMensagemFeedback("Por favor, selecione pelo menos um participante para exportar os crachás em PNG.", "warning");
                    return;
                }

                btnExportarLotePng.disabled = true;
                try {
                    if (ExportModule.exportarLotePngZip) {
                        const qtd = await ExportModule.exportarLotePngZip(linhasSelecionadas, {
                            extrairDados: extrairDadosDaLinha
                        });
                        exibirMensagemFeedback(`Exportação concluída! ${qtd} crachá(s) PNG exportado(s) em ZIP.`, "success");
                    } else {
                        throw new Error("Módulo de exportação ZIP indisponível.");
                    }
                } catch (err) {
                    console.error("Falha ao exportar lote de crachás PNG:", err);
                    exibirMensagemFeedback(
                        ExportModule.MENSAGEM_ERRO_EXPORTACAO_PNG || "Não foi possível exportar os crachás em PNG.",
                        "danger"
                    );
                } finally {
                    btnExportarLotePng.disabled = false;
                }
            });
        }

        // Impressão em lote A4
        if (btnImprimirLote && containerImpressaoLote) {
            btnImprimirLote.addEventListener("click", function () {
                const linhasSelecionadas = linhas.filter(l => l.querySelector(".chk-participante-item")?.checked);
                if (linhasSelecionadas.length === 0) {
                    exibirMensagemFeedback("Por favor, selecione pelo menos um participante para impressão.", "warning");
                    return;
                }

                if (ExportModule.executarImpressaoLoteA4) {
                    ExportModule.executarImpressaoLoteA4(linhasSelecionadas, {
                        container: containerImpressaoLote,
                        selectModelo
                    });
                }
            });
        }

        // Inicialização
        popularFiltroCargosSeNecessario();
        configurarResilienciaImagens();
        atualizarModeloSelecionado();
        filtrarParticipantes(false);
        atualizarContadores();
        if (linhasFiltradas.length > 0) {
            atualizarPreviewComLinha(linhasFiltradas[0]);
        }
    });
})();
