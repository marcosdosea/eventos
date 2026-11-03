(function () {
    "use strict";

    const MENSAGEM_ERRO_EXPORTACAO_PNG = "Não foi possível exportar o PNG. Verifique se a imagem do participante está válida ou tente novamente.";

    document.addEventListener("DOMContentLoaded", function () {
        const selectEvento = document.getElementById("selectEventoEmitir");
        const selectModelo = document.getElementById("selectModeloEmitir");
        const btnImprimirLote = document.getElementById("btnImprimirLotePdf");

        const chkMaster = document.getElementById("chkSelecionarTodosHeader");
        const btnSelecionarTodosPagina = document.getElementById("btnSelecionarTodosPagina");
        const btnLimparSelecao = document.getElementById("btnLimparSelecao");

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

        const ITENS_POR_PAGINA = 5;
        let paginaAtual = 1;
        let escalaAtual = 1;

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
            if (!nome || typeof nome !== "string") return "PA";
            const limpo = nome.trim();
            if (!limpo) return "PA";
            return limpo.substring(0, Math.min(2, limpo.length)).toUpperCase();
        }

        function ehPayloadImagemValido(src) {
            if (!src || typeof src !== "string") return false;
            const limpo = src.trim();
            if (!limpo) return false;

            if (limpo.startsWith("data:")) {
                if (!limpo.startsWith("data:image/")) return false;
                const idxVirgula = limpo.indexOf(",");
                if (idxVirgula < 0) return false;
                const b64 = limpo.substring(idxVirgula + 1).trim();
                if (b64.length < 4) return false;

                const prefixosValidos = [
                    "/9j/",
                    "iVBOR",
                    "UklGR",
                    "R0lGOD",
                    "SUkq",
                    "TU0A",
                    "PHN2Zw",
                    "PD94bW"
                ];
                return prefixosValidos.some(prefixo => b64.startsWith(prefixo));
            }

            return limpo.startsWith("http://") || limpo.startsWith("https://") || limpo.startsWith("/") || limpo.startsWith("blob:");
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
            const termo = (filtroInput?.value || "").toLowerCase().trim();

            if (!termo) {
                linhasFiltradas = [...linhas];
            } else {
                linhasFiltradas = linhas.filter(linha => {
                    const nome = (linha.dataset.nome || "").toLowerCase();
                    const nomeCompleto = (linha.dataset.nomeCompleto || "").toLowerCase();
                    const depto = (linha.dataset.depto || "").toLowerCase();
                    const cpf = (linha.dataset.cpf || "").toLowerCase();
                    const cpfPuro = (linha.dataset.cpfPuro || "").toLowerCase();
                    const credencial = (linha.dataset.credencial || "").toLowerCase();
                    return nome.includes(termo) ||
                           nomeCompleto.includes(termo) ||
                           depto.includes(termo) ||
                           cpf.includes(termo) ||
                           cpfPuro.includes(termo) ||
                           credencial.includes(termo);
                });
            }

            if (!preservarPagina) {
                paginaAtual = 1;
            }
            aplicarPaginacao();
            atualizarContadores();
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

        if (filtroInput) {
            filtroInput.addEventListener("input", function () {
                filtrarParticipantes(false);
            });
        }

        if (btnLimparBusca && filtroInput) {
            btnLimparBusca.addEventListener("click", function () {
                filtroInput.value = "";
                filtrarParticipantes(false);
                filtroInput.focus();
            });
        }

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

        if (selectEvento) {
            selectEvento.addEventListener("change", function () {
                const idEvento = this.value;
                if (idEvento && idEvento !== "0") {
                    window.location.href = `/Modelocracha/EmitirCracha?idEvento=${idEvento}`;
                }
            });
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

        if (selectModelo) {
            selectModelo.addEventListener("change", atualizarModeloSelecionado);
        }

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

        function quebrarTextoEmLinhas(context, text, maxWidth) {
            if (!text) return [];
            const words = text.toString().trim().split(/\s+/);
            const lines = [];
            let currentLine = "";

            for (let i = 0; i < words.length; i++) {
                const testLine = currentLine ? `${currentLine} ${words[i]}` : words[i];
                if (context.measureText(testLine).width > maxWidth && currentLine) {
                    lines.push(currentLine);
                    currentLine = words[i];
                } else {
                    currentLine = testLine;
                }
            }
            if (currentLine) {
                lines.push(currentLine);
            }
            return lines;
        }

        function carregarImagemSegura(src) {
            return new Promise(resolve => {
                if (!ehPayloadImagemValido(src)) {
                    resolve(null);
                    return;
                }
                const img = new Image();
                img.onload = function () {
                    if (img.naturalWidth > 0 && img.naturalHeight > 0) {
                        resolve(img);
                    } else {
                        resolve(null);
                    }
                };
                img.onerror = function () {
                    resolve(null);
                };
                img.src = src;
            });
        }

        function desenharAvatarPadraoCanvas(ctx, centroX, centroY) {
            ctx.fillStyle = "#E2E8F0";
            ctx.fillRect(centroX - 70, centroY - 70, 140, 140);
            ctx.fillStyle = "#475569";
            ctx.beginPath();
            ctx.arc(centroX, centroY - 20, 30, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = "#1E293B";
            ctx.beginPath();
            ctx.arc(centroX, centroY + 68, 52, Math.PI, 0, false);
            ctx.fill();
        }

        function desenharQrCodeFallbackCanvas(ctx, x, y, tamanho) {
            const escala = tamanho / 100;
            ctx.save();
            ctx.translate(x, y);
            ctx.scale(escala, escala);
            ctx.fillStyle = "#FFFFFF";
            ctx.fillRect(0, 0, 100, 100);
            ctx.fillStyle = "#0F172A";
            const blocos = [
                [8, 8, 28, 28], [64, 8, 28, 28], [8, 64, 28, 28],
                [44, 12, 10, 10], [44, 30, 10, 16], [12, 44, 16, 10],
                [48, 48, 16, 16], [72, 44, 14, 10], [48, 72, 12, 16], [70, 70, 18, 18]
            ];
            blocos.forEach(([bx, by, bw, bh]) => ctx.fillRect(bx, by, bw, bh));
            ctx.fillStyle = "#FFFFFF";
            [[14, 14, 16, 16], [70, 14, 16, 16], [14, 70, 16, 16]].forEach(([bx, by, bw, bh]) => ctx.fillRect(bx, by, bw, bh));
            ctx.fillStyle = "#0F172A";
            [[18, 18, 8, 8], [74, 18, 8, 8], [18, 74, 8, 8]].forEach(([bx, by, bw, bh]) => ctx.fillRect(bx, by, bw, bh));
            ctx.restore();
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

        async function renderizarCrachaParaCanvas(dadosParticipante) {
            const canvas = document.createElement("canvas");
            const ctx = canvas.getContext("2d");
            if (!ctx) {
                throw new Error("Contexto 2D do Canvas indisponível.");
            }

            const largura = 640;
            const altura = 920;
            canvas.width = largura;
            canvas.height = altura;

            ctx.fillStyle = "#FFFFFF";
            ctx.beginPath();
            ctx.roundRect(0, 0, largura, altura, 36);
            ctx.fill();

            ctx.fillStyle = "#232ed0";
            ctx.beginPath();
            ctx.roundRect(0, 0, largura, 205, [36, 36, 0, 0]);
            ctx.fill();

            ctx.fillStyle = "#EEF2FA";
            ctx.beginPath();
            ctx.roundRect(largura / 2 - 40, 16, 80, 20, 10);
            ctx.fill();

            ctx.fillStyle = "#FFFFFF";
            ctx.font = "bold 13px Segoe UI, sans-serif";
            ctx.textAlign = "center";
            ctx.fillText("BORA LÁ EVENTOS", largura / 2, 60);

            const logoImg = document.getElementById("crachaLogoElement");
            const logoContainer = document.getElementById("crachaLogoContainer");
            let titleStartY = 96;
            if (logoImg && logoContainer && logoContainer.style.display !== "none" && logoImg.src) {
                const logoCarregado = (logoImg.complete && logoImg.naturalWidth > 0)
                    ? logoImg
                    : await carregarImagemSegura(logoImg.src);

                if (logoCarregado && logoCarregado.naturalWidth > 0) {
                    try {
                        const logoMaxW = 160;
                        const logoMaxH = 34;
                        let logoW = logoCarregado.naturalWidth;
                        let logoH = logoCarregado.naturalHeight;
                        const scale = Math.min(logoMaxW / logoW, logoMaxH / logoH);
                        logoW *= scale;
                        logoH *= scale;
                        ctx.drawImage(logoCarregado, largura / 2 - logoW / 2, 68, logoW, logoH);
                        titleStartY = 118;
                    } catch {
                    }
                }
            }

            const nomeEvento = (document.getElementById("lblPreviewNomeEvento")?.textContent || "EVENTO").trim().toUpperCase();
            const elSub = document.getElementById("lblPreviewSubtituloEvento");
            const subEvento = (elSub && elSub.style.display !== "none" ? elSub.textContent.trim() : "").toUpperCase();

            let fontSizeTitle = 22;
            let lineHeightTitle = 26;
            let titleLines = [];
            const maxTitleWidth = 540;

            while (fontSizeTitle >= 14) {
                ctx.font = `bold ${fontSizeTitle}px Segoe UI, sans-serif`;
                titleLines = quebrarTextoEmLinhas(ctx, nomeEvento, maxTitleWidth);
                const nenhumEstouro = !titleLines.some(l => ctx.measureText(l).width > maxTitleWidth);
                if (nenhumEstouro && titleLines.length <= 2) {
                    break;
                }
                if (fontSizeTitle <= 15) {
                    break;
                }
                fontSizeTitle -= 1;
                lineHeightTitle = Math.round(fontSizeTitle * 1.25);
            }

            ctx.fillStyle = "#FFFFFF";
            ctx.font = `bold ${fontSizeTitle}px Segoe UI, sans-serif`;
            ctx.textAlign = "center";

            const subLines = subEvento ? quebrarTextoEmLinhas(ctx, subEvento, maxTitleWidth) : [];
            const totalSubHeight = subLines.length > 0 ? (subLines.length * 18 + 6) : 0;
            const totalTitleHeight = (titleLines.length - 1) * lineHeightTitle + fontSizeTitle;
            const totalBlockHeight = totalTitleHeight + totalSubHeight;

            const topAvailableY = titleStartY;
            const bottomAvailableY = 196;
            const availableHeight = bottomAvailableY - topAvailableY;
            const espacoSobra = Math.max(0, (availableHeight - totalBlockHeight) / 2);

            let currentY = topAvailableY + espacoSobra + (fontSizeTitle * 0.75);

            titleLines.forEach(line => {
                ctx.fillText(line, largura / 2, currentY);
                currentY += lineHeightTitle;
            });

            if (subLines.length > 0) {
                ctx.font = "bold 13px Segoe UI, sans-serif";
                ctx.fillStyle = "#E0E7FF";
                currentY += 4;
                subLines.forEach(line => {
                    ctx.fillText(line, largura / 2, currentY);
                    currentY += 18;
                });
            }

            const centroY = 320;
            const imgFotoValida = await carregarImagemSegura(dadosParticipante.fotoUrl);

            ctx.save();
            ctx.beginPath();
            ctx.arc(largura / 2, centroY, 70, 0, Math.PI * 2, true);
            ctx.closePath();
            ctx.clip();

            if (imgFotoValida) {
                try {
                    ctx.drawImage(imgFotoValida, largura / 2 - 70, centroY - 70, 140, 140);
                } catch {
                    desenharAvatarPadraoCanvas(ctx, largura / 2, centroY);
                }
            } else {
                desenharAvatarPadraoCanvas(ctx, largura / 2, centroY);
            }
            ctx.restore();

            const aptoExport = Boolean(dadosParticipante.apto);
            ctx.fillStyle = aptoExport ? "#10B981" : "#F59E0B";
            ctx.beginPath();
            ctx.arc(largura / 2 + 50, centroY + 50, 18, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = "#FFFFFF";
            ctx.font = "bold 18px Segoe UI, sans-serif";
            ctx.textAlign = "center";
            ctx.fillText(aptoExport ? "✓" : "!", largura / 2 + 50, centroY + 56);

            const nomePart = (dadosParticipante.nome || "PARTICIPANTE").trim().toUpperCase();
            ctx.fillStyle = "#0F172A";
            let nomeFontSize = 30;
            ctx.font = `bold ${nomeFontSize}px Segoe UI, sans-serif`;
            while (ctx.measureText(nomePart).width > 540 && nomeFontSize > 18) {
                nomeFontSize -= 2;
                ctx.font = `bold ${nomeFontSize}px Segoe UI, sans-serif`;
            }
            ctx.textAlign = "center";
            ctx.fillText(nomePart, largura / 2, 450);

            const orgPart = (dadosParticipante.depto || "").trim();
            if (orgPart) {
                ctx.fillStyle = "#64748B";
                let orgFontSize = 19;
                ctx.font = `500 ${orgFontSize}px Segoe UI, sans-serif`;
                while (ctx.measureText(orgPart).width > 540 && orgFontSize > 13) {
                    orgFontSize -= 1.5;
                    ctx.font = `500 ${orgFontSize}px Segoe UI, sans-serif`;
                }
                ctx.fillText(orgPart, largura / 2, 485);
            }

            const papelTexto = (dadosParticipante.papel || "PARTICIPANTE").trim().toUpperCase();
            ctx.font = "bold 18px Segoe UI, sans-serif";
            const textPapelWidth = ctx.measureText(papelTexto).width;
            const pillWidth = Math.max(220, Math.min(360, textPapelWidth + 50));
            ctx.fillStyle = "#232ed0";
            ctx.beginPath();
            ctx.roundRect(largura / 2 - pillWidth / 2, 516, pillWidth, 42, 21);
            ctx.fill();
            ctx.fillStyle = "#FFFFFF";
            ctx.fillText(papelTexto, largura / 2, 544);

            ctx.fillStyle = "#64748B";
            ctx.font = "15px Segoe UI, sans-serif";
            const cpfTexto = dadosParticipante.cpf || "";
            const credTexto = dadosParticipante.credencial || "";
            ctx.fillText(`CPF: ${cpfTexto}       Credencial: ${credTexto}`, largura / 2, 598);

            const optModelo = selectModelo ? selectModelo.options[selectModelo.selectedIndex] : null;
            const qrCodeHabilitado = optModelo
                ? (optModelo.dataset.qrcode !== "0" && optModelo.dataset.qrcode !== 0)
                : (blocoQrCode ? blocoQrCode.style.display !== "none" : true);

            if (qrCodeHabilitado) {
                ctx.fillStyle = "#FFFFFF";
                ctx.strokeStyle = "#E2E8F0";
                ctx.lineWidth = 2;
                ctx.beginPath();
                ctx.roundRect(50, 625, largura - 100, 165, 16);
                ctx.fill();
                ctx.stroke();

                const qrRaw = (dadosParticipante.qrcodeB64 || "").trim();
                const qrSrc = qrRaw
                    ? (qrRaw.startsWith("data:") ? qrRaw : `data:image/png;base64,${qrRaw}`)
                    : "";
                const qrImg = await carregarImagemSegura(qrSrc);

                if (qrImg) {
                    try {
                        ctx.drawImage(qrImg, 70, 640, 135, 135);
                    } catch {
                        desenharQrCodeFallbackCanvas(ctx, 70, 640, 135);
                    }
                } else {
                    desenharQrCodeFallbackCanvas(ctx, 70, 640, 135);
                }

                ctx.textAlign = "left";
                ctx.fillStyle = "#64748B";
                ctx.font = "bold 13px Segoe UI, sans-serif";
                ctx.fillText("CÓDIGO DE PRESENÇA", 225, 672);

                const presencaTexto = dadosParticipante.presenca || "BL - EVT - 2025 - 00142";
                ctx.fillStyle = "#0F172A";
                ctx.font = "bold 18px Consolas, monospace";
                ctx.fillText(presencaTexto, 225, 706);

                ctx.fillStyle = "#232ed0";
                ctx.font = "bold 14px Segoe UI, sans-serif";
                ctx.fillText("✓ Válido em todas salas", 225, 744);
            }

            ctx.fillStyle = "#F8FAFC";
            ctx.beginPath();
            ctx.roundRect(0, altura - 75, largura, 75, [0, 0, 36, 36]);
            ctx.fill();

            ctx.fillStyle = "#475569";
            ctx.textAlign = "center";
            const textoFooter = (lblTextoCracha?.textContent || "Uso obrigatório e intransferível nas dependências do evento.").trim();

            let footerFontSize = 13;
            let footerLines = [];
            while (footerFontSize >= 10) {
                ctx.font = `italic ${footerFontSize}px Segoe UI, sans-serif`;
                footerLines = quebrarTextoEmLinhas(ctx, textoFooter, 560);
                if (footerLines.length <= 2 || footerFontSize <= 10) {
                    break;
                }
                footerFontSize -= 1;
            }
            ctx.font = `italic ${footerFontSize}px Segoe UI, sans-serif`;
            const footerLineHeight = footerFontSize + 5;
            const footerTotalHeight = (footerLines.length - 1) * footerLineHeight;
            let footerY = (altura - 75) + (75 - footerTotalHeight) / 2 + (footerFontSize * 0.35);

            footerLines.forEach(line => {
                ctx.fillText(line, largura / 2, footerY);
                footerY += footerLineHeight;
            });

            return canvas;
        }

        async function renderizarEBaixarPng() {
            try {
                const linhaAtiva = document.querySelector(".linha-ativa-preview");
                const dados = extrairDadosDaLinha(linhaAtiva);
                const canvas = await renderizarCrachaParaCanvas(dados);

                const dataUrl = canvas.toDataURL("image/png");
                if (!dataUrl || !dataUrl.startsWith("data:image/png")) {
                    throw new Error("Falha ao serializar imagem PNG do canvas.");
                }

                const link = document.createElement("a");
                const nomeLimpo = (dados.nome || "participante").replace(/[^a-zA-Z0-9]/g, "_").toLowerCase();
                link.download = `cracha_${nomeLimpo}.png`;
                link.href = dataUrl;
                link.click();
            } catch (err) {
                console.error("Falha ao exportar imagem PNG do crachá:", err);
                exibirMensagemFeedback(MENSAGEM_ERRO_EXPORTACAO_PNG, "danger");
            }
        }

        if (btnExportarPng && crachaCardElement) {
            btnExportarPng.addEventListener("click", function () {
                renderizarEBaixarPng();
            });
        }

        const TABELA_CRC32 = (function () {
            const tabela = new Uint32Array(256);
            for (let n = 0; n < 256; n++) {
                let c = n;
                for (let k = 0; k < 8; k++) {
                    c = (c & 1) ? (0xEDB88320 ^ (c >>> 1)) : (c >>> 1);
                }
                tabela[n] = c >>> 0;
            }
            return tabela;
        })();

        function calcularCrc32(bytes) {
            let crc = 0xFFFFFFFF;
            for (let i = 0; i < bytes.length; i++) {
                crc = TABELA_CRC32[(crc ^ bytes[i]) & 0xFF] ^ (crc >>> 8);
            }
            return (crc ^ 0xFFFFFFFF) >>> 0;
        }

        function dataUrlParaUint8Array(dataUrl) {
            const idxVirgula = dataUrl.indexOf(",");
            const base64 = idxVirgula >= 0 ? dataUrl.substring(idxVirgula + 1) : dataUrl;
            const binStr = atob(base64);
            const len = binStr.length;
            const bytes = new Uint8Array(len);
            for (let i = 0; i < len; i++) {
                bytes[i] = binStr.charCodeAt(i);
            }
            return bytes;
        }

        function gerarArquivoZip(arquivos) {
            const encoder = new TextEncoder();
            const partesLocais = [];
            const partesCentral = [];
            let offsetAtual = 0;

            const agora = new Date();
            const dosTime = ((agora.getHours() & 0x1F) << 11) | ((agora.getMinutes() & 0x3F) << 5) | ((agora.getSeconds() >> 1) & 0x1F);
            const dosDate = (((Math.max(1980, agora.getFullYear()) - 1980) & 0x7F) << 9) | (((agora.getMonth() + 1) & 0x0F) << 5) | (agora.getDate() & 0x1F);

            arquivos.forEach(arq => {
                const nomeBytes = encoder.encode(arq.nome);
                const dados = arq.bytes;
                const crc = calcularCrc32(dados);

                const localHeader = new Uint8Array(30 + nomeBytes.length);
                const localView = new DataView(localHeader.buffer);
                localView.setUint32(0, 0x04034B50, true);
                localView.setUint16(4, 20, true);
                localView.setUint16(6, 0x0800, true);
                localView.setUint16(8, 0, true);
                localView.setUint16(10, dosTime, true);
                localView.setUint16(12, dosDate, true);
                localView.setUint32(14, crc, true);
                localView.setUint32(18, dados.length, true);
                localView.setUint32(22, dados.length, true);
                localView.setUint16(26, nomeBytes.length, true);
                localView.setUint16(28, 0, true);
                localHeader.set(nomeBytes, 30);

                const centralHeader = new Uint8Array(46 + nomeBytes.length);
                const centralView = new DataView(centralHeader.buffer);
                centralView.setUint32(0, 0x02014B50, true);
                centralView.setUint16(4, 20, true);
                centralView.setUint16(6, 20, true);
                centralView.setUint16(8, 0x0800, true);
                centralView.setUint16(10, 0, true);
                centralView.setUint16(12, dosTime, true);
                centralView.setUint16(14, dosDate, true);
                centralView.setUint32(16, crc, true);
                centralView.setUint32(20, dados.length, true);
                centralView.setUint32(24, dados.length, true);
                centralView.setUint16(28, nomeBytes.length, true);
                centralView.setUint16(30, 0, true);
                centralView.setUint16(32, 0, true);
                centralView.setUint16(34, 0, true);
                centralView.setUint16(36, 0, true);
                centralView.setUint32(38, 0, true);
                centralView.setUint32(42, offsetAtual, true);
                centralHeader.set(nomeBytes, 46);

                partesLocais.push(localHeader, dados);
                partesCentral.push(centralHeader);

                offsetAtual += localHeader.length + dados.length;
            });

            const tamanhoCentral = partesCentral.reduce((acc, p) => acc + p.length, 0);

            const eocd = new Uint8Array(22);
            const eocdView = new DataView(eocd.buffer);
            eocdView.setUint32(0, 0x06054B50, true);
            eocdView.setUint16(4, 0, true);
            eocdView.setUint16(6, 0, true);
            eocdView.setUint16(8, arquivos.length, true);
            eocdView.setUint16(10, arquivos.length, true);
            eocdView.setUint32(12, tamanhoCentral, true);
            eocdView.setUint32(16, offsetAtual, true);
            eocdView.setUint16(20, 0, true);

            return new Blob([...partesLocais, ...partesCentral, eocd], { type: "application/zip" });
        }

        async function exportarLotePngZip() {
            const linhasSelecionadas = linhas.filter(l => l.querySelector(".chk-participante-item")?.checked);
            if (linhasSelecionadas.length === 0) {
                exibirMensagemFeedback("Por favor, selecione pelo menos um participante para exportar os crachás em PNG.", "warning");
                return;
            }

            if (btnExportarLotePng) btnExportarLotePng.disabled = true;

            try {
                const arquivosPng = [];
                const nomesUsados = new Set();
                const total = linhasSelecionadas.length;

                for (let i = 0; i < total; i++) {
                    const linha = linhasSelecionadas[i];
                    const dados = extrairDadosDaLinha(linha);

                    const canvas = await renderizarCrachaParaCanvas(dados);
                    const dataUrl = canvas.toDataURL("image/png");
                    const pngBytes = dataUrlParaUint8Array(dataUrl);

                    const credLimpa = (dados.credencial || `${i + 1}`).replace(/[^a-zA-Z0-9]/g, "");
                    const nomeLimpo = (dados.nome || `participante_${i + 1}`)
                        .normalize("NFD")
                        .replace(/[\u0300-\u036f]/g, "")
                        .replace(/[^a-zA-Z0-9]/g, "_")
                        .replace(/_+/g, "_")
                        .replace(/^_|_$/g, "")
                        .toLowerCase();

                    let nomeArquivo = `cracha_${credLimpa}_${nomeLimpo || (i + 1)}.png`;
                    if (nomesUsados.has(nomeArquivo)) {
                        nomeArquivo = `cracha_${credLimpa}_${nomeLimpo}_${i + 1}.png`;
                    }
                    nomesUsados.add(nomeArquivo);

                    arquivosPng.push({
                        nome: nomeArquivo,
                        bytes: pngBytes
                    });
                }

                const zipBlob = gerarArquivoZip(arquivosPng);
                const urlZip = URL.createObjectURL(zipBlob);
                const nomeEventoBruto = document.getElementById("lblPreviewNomeEvento")?.textContent || "evento";
                const nomeEventoLimpo = nomeEventoBruto
                    .normalize("NFD")
                    .replace(/[\u0300-\u036f]/g, "")
                    .replace(/[^a-zA-Z0-9]/g, "_")
                    .replace(/_+/g, "_")
                    .replace(/^_|_$/g, "")
                    .toLowerCase();

                const link = document.createElement("a");
                link.href = urlZip;
                link.download = `crachas_${nomeEventoLimpo || "lote"}.zip`;
                document.body.appendChild(link);
                link.click();
                link.remove();

                setTimeout(() => URL.revokeObjectURL(urlZip), 5000);
                exibirMensagemFeedback(`Exportação concluída! ${arquivosPng.length} crachá(s) PNG exportado(s) em ZIP.`, "success");
            } catch (err) {
                console.error("Falha ao exportar lote de crachás PNG:", err);
                exibirMensagemFeedback(MENSAGEM_ERRO_EXPORTACAO_PNG, "danger");
            } finally {
                if (btnExportarLotePng) btnExportarLotePng.disabled = false;
            }
        }

        if (btnExportarLotePng) {
            btnExportarLotePng.addEventListener("click", function () {
                exportarLotePngZip();
            });
        }

        if (btnImprimirLote && containerImpressaoLote) {
            btnImprimirLote.addEventListener("click", function () {
                const linhasSelecionadas = linhas.filter(l => l.querySelector(".chk-participante-item")?.checked);

                if (linhasSelecionadas.length === 0) {
                    exibirMensagemFeedback("Por favor, selecione pelo menos um participante para impressão.", "warning");
                    return;
                }

                const optModelo = selectModelo ? selectModelo.options[selectModelo.selectedIndex] : null;
                const modeloQrcodeHabilitado = optModelo ? (optModelo.dataset.qrcode !== "0" && optModelo.dataset.qrcode !== 0) : true;

                containerImpressaoLote.innerHTML = "";
                const grid = document.createElement("div");
                grid.className = "print-grid-lote";

                const nomeEvento = document.getElementById("lblPreviewNomeEvento")?.textContent || "EVENTO";
                const elSubPrint = document.getElementById("lblPreviewSubtituloEvento");
                const subEvento = (elSubPrint && elSubPrint.style.display !== "none" ? elSubPrint.textContent.trim() : "");
                const textoRodape = lblTextoCracha?.textContent || "Uso pessoal e intransferível.";
                const logoSrc = document.getElementById("crachaLogoElement")?.src || "";
                const crachaLogoContainer = document.getElementById("crachaLogoContainer");
                const logoVisivel = crachaLogoContainer && crachaLogoContainer.style.display !== "none" && ehPayloadImagemValido(logoSrc);

                const fallbackQrSvg = `<svg viewBox="0 0 100 100" width="70" height="70" style="display:block;margin:auto;"><rect width="100" height="100" fill="#FFFFFF"/><rect x="8" y="8" width="28" height="28" fill="#0F172A"/><rect x="14" y="14" width="16" height="16" fill="#FFFFFF"/><rect x="18" y="18" width="8" height="8" fill="#0F172A"/><rect x="64" y="8" width="28" height="28" fill="#0F172A"/><rect x="70" y="14" width="16" height="16" fill="#FFFFFF"/><rect x="74" y="18" width="8" height="8" fill="#0F172A"/><rect x="8" y="64" width="28" height="28" fill="#0F172A"/><rect x="14" y="70" width="16" height="16" fill="#FFFFFF"/><rect x="18" y="74" width="8" height="8" fill="#0F172A"/><rect x="44" y="12" width="10" height="10" fill="#0F172A"/><rect x="44" y="30" width="10" height="16" fill="#0F172A"/><rect x="12" y="44" width="16" height="10" fill="#0F172A"/><rect x="48" y="48" width="16" height="16" fill="#0F172A"/><rect x="72" y="44" width="14" height="10" fill="#0F172A"/><rect x="48" y="72" width="12" height="16" fill="#0F172A"/><rect x="70" y="70" width="18" height="18" fill="#0F172A"/></svg>`;
                const placeholderAvatarSvg = `<div class="avatar-placeholder-svg"><svg viewBox="0 0 100 100" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="50" cy="50" r="50" fill="#E2E8F0" /><circle cx="50" cy="38" r="20" fill="#475569" /><path d="M16 88C16 70 30 64 50 64C70 64 84 70 84 88" fill="#1E293B" /></svg></div>`;

                linhasSelecionadas.forEach(linha => {
                    const card = document.createElement("div");
                    card.className = "cracha-print-item";

                    const nome = (linha.dataset.nome || linha.dataset.nomeCompleto || "").toUpperCase();
                    const depto = linha.dataset.depto || "";
                    const papel = (linha.dataset.papel || "PARTICIPANTE").toUpperCase();
                    const cpf = linha.dataset.cpf || "";
                    const cred = linha.dataset.credencial || "";
                    const presenca = linha.dataset.presenca || "";
                    const qrcodeData = (linha.dataset.qrcode || "").trim();
                    const fotoUrl = (linha.dataset.foto || "").trim();
                    const temFotoValida = ehPayloadImagemValido(fotoUrl);
                    const apto = (linha.dataset.apto === "true");
                    const statusDesc = linha.dataset.statusDesc || (apto ? "Apto" : "Pendente Foto");

                    let avatarHtml = `
                        <div class="participant-avatar-wrapper">
                            ${temFotoValida ? `<img src="${fotoUrl}" class="participant-avatar-img" alt="Foto de ${nome}" />` : placeholderAvatarSvg}
                            <div class="participant-verified-badge" style="background-color: ${apto ? '#10B981' : '#F59E0B'};" title="${statusDesc}">
                                <i class="bi ${apto ? 'bi-check-lg' : 'bi-clock-history'}"></i>
                            </div>
                        </div>
                    `;

                    let qrcodeHtml = "";
                    if (modeloQrcodeHabilitado) {
                        if (qrcodeData) {
                            const qrcodeSrc = qrcodeData.startsWith("data:") ? qrcodeData : `data:image/png;base64,${qrcodeData}`;
                            qrcodeHtml = `<img src="${qrcodeSrc}" alt="QR Code" style="width:100%;height:100%;object-fit:contain;display:block;" />`;
                        } else {
                            qrcodeHtml = fallbackQrSvg;
                        }
                    }

                    card.innerHTML = `
                        <div class="cracha-physical-card">
                            <div class="cracha-hole"></div>
                            <div class="cracha-header">
                                <div class="cracha-header-sub"><span>BORA LÁ EVENTOS</span></div>
                                ${logoVisivel ? `<div class="cracha-logo-institucional-container"><img src="${logoSrc}" class="cracha-logo-institucional-img" alt="Logo"/></div>` : ''}
                                <h6 class="cracha-evento-title">${nomeEvento}</h6>
                                ${subEvento ? `<div class="cracha-evento-subtitle">${subEvento}</div>` : ''}
                            </div>
                            <div class="cracha-body">
                                ${avatarHtml}
                                <div class="participant-name">${nome}</div>
                                <div class="participant-org">${depto}</div>
                                <div class="badge-role-pill">${papel}</div>
                                <div class="cracha-meta-info-row">
                                    <div>CPF: <strong>${cpf}</strong></div>
                                    <div>Credencial: <strong>${cred}</strong></div>
                                </div>
                                <div class="cracha-presenca-box" style="${!modeloQrcodeHabilitado ? 'display:none;' : ''}">
                                    <div class="cracha-qrcode-box">
                                        ${qrcodeHtml}
                                    </div>
                                    <div class="cracha-presenca-text">
                                        <div class="presenca-label">CÓDIGO DE PRESENÇA</div>
                                        <div class="presenca-code">${presenca}</div>
                                        <div class="presenca-status"><span>Válido em todas salas</span></div>
                                    </div>
                                </div>
                            </div>
                            <div class="cracha-footer">
                                <p class="cracha-footer-text">${textoRodape}</p>
                            </div>
                        </div>
                    `;

                    const imgPrintAvatar = card.querySelector(".participant-avatar-img");
                    if (imgPrintAvatar) {
                        imgPrintAvatar.addEventListener("error", function () {
                            const wrapper = imgPrintAvatar.closest(".participant-avatar-wrapper");
                            imgPrintAvatar.remove();
                            if (wrapper && !wrapper.querySelector(".avatar-placeholder-svg")) {
                                wrapper.insertAdjacentHTML("afterbegin", placeholderAvatarSvg);
                            }
                        });
                    }

                    grid.appendChild(card);
                });

                document.body.classList.add("imprimindo-lote");
                containerImpressaoLote.classList.remove("d-none");
                containerImpressaoLote.appendChild(grid);

                const imagens = Array.from(grid.querySelectorAll("img"));
                const promessasDecodificacao = imagens.map(img => {
                    if (typeof img.decode === "function") {
                        return img.decode().catch(() => {});
                    }
                    if (img.complete) {
                        return Promise.resolve();
                    }
                    return new Promise(resolve => {
                        img.onload = resolve;
                        img.onerror = resolve;
                    });
                });

                const finalizarImpressao = () => {
                    window.removeEventListener("afterprint", finalizarImpressao);
                    document.body.classList.remove("imprimindo-lote");
                    containerImpressaoLote.classList.add("d-none");
                    containerImpressaoLote.innerHTML = "";
                };

                window.addEventListener("afterprint", finalizarImpressao);

                Promise.all(promessasDecodificacao).then(() => {
                    requestAnimationFrame(() => {
                        setTimeout(() => {
                            window.print();
                        }, 250);
                    });
                });
            });
        }

        configurarResilienciaImagens();
        atualizarModeloSelecionado();
        filtrarParticipantes(false);
        atualizarContadores();
        if (linhasFiltradas.length > 0) {
            atualizarPreviewComLinha(linhasFiltradas[0]);
        }
    });
})();
