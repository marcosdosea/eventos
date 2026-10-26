(function () {
    "use strict";

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

        // Elementos do Live Preview
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
        const btnZoomIn = document.getElementById("btnZoomIn");
        const btnZoomOut = document.getElementById("btnZoomOut");
        const btnFullscreen = document.getElementById("btnFullscreenPreview");

        const containerImpressaoLote = document.getElementById("containerImpressaoLoteA4");

        let linhas = tabelaBody ? Array.from(tabelaBody.querySelectorAll(".linha-participante")) : [];
        let linhasFiltradas = [...linhas];

        const ITENS_POR_PAGINA = 5;
        let paginaAtual = 1;
        let escalaAtual = 1;

        function atualizarContadores() {
            if (!lblQtdSelecionados || !lblQtdTotal) return;
            const selecionados = linhas.filter(l => {
                const chk = l.querySelector(".chk-participante-item");
                return chk && chk.checked;
            }).length;
            lblQtdSelecionados.textContent = selecionados.toString();
            lblQtdTotal.textContent = linhas.length.toString();
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

            // Atualiza estado do checkbox master na página
            if (chkMaster) {
                const linhasPagina = linhasFiltradas.slice(inicio, fim);
                if (linhasPagina.length === 0) {
                    chkMaster.checked = false;
                    chkMaster.indeterminate = false;
                } else {
                    const todosMarcados = linhasPagina.every(l => l.querySelector(".chk-participante-item")?.checked);
                    const algumMarcado = linhasPagina.some(l => l.querySelector(".chk-participante-item")?.checked);
                    chkMaster.checked = todosMarcados;
                    chkMaster.indeterminate = (!todosMarcados && algumMarcado);
                }
            }
        }

        function filtrarParticipantes() {
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

            paginaAtual = 1;
            aplicarPaginacao();
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
            const fotoUrl = linha.dataset.foto || "";

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

            if (fotoUrl && fotoUrl.trim() !== "") {
                if (previewFoto) {
                    previewFoto.src = fotoUrl;
                    previewFoto.style.display = "block";
                }
                if (previewAvatarPlaceholder) {
                    previewAvatarPlaceholder.style.display = "none";
                }
            } else {
                if (previewFoto) {
                    previewFoto.src = "";
                    previewFoto.style.display = "none";
                }
                if (previewAvatarPlaceholder) {
                    previewAvatarPlaceholder.style.display = "block";
                }
            }

            if (qrcodeB64 && qrcodeB64.trim() !== "") {
                if (previewQrCodeImg) {
                    previewQrCodeImg.src = `data:image/png;base64,${qrcodeB64}`;
                    previewQrCodeImg.style.display = "block";
                }
                if (previewQrPlaceholder) {
                    previewQrPlaceholder.style.display = "none";
                }
            } else {
                if (previewQrCodeImg) {
                    previewQrCodeImg.src = "";
                    previewQrCodeImg.style.display = "none";
                }
                if (previewQrPlaceholder) {
                    previewQrPlaceholder.style.display = "block";
                }
            }
        }

        // Eventos da Tabela
        if (tabelaBody) {
            tabelaBody.addEventListener("click", function (e) {
                const btnPrevia = e.target.closest(".btn-ver-previa");
                if (btnPrevia) {
                    const linha = btnPrevia.closest(".linha-participante");
                    atualizarPreviewComLinha(linha);
                    return;
                }

                const btnExcluir = e.target.closest(".btn-excluir-item");
                if (btnExcluir) {
                    const linha = btnExcluir.closest(".linha-participante");
                    if (linha && confirm(`Deseja remover "${linha.dataset.nome || "este participante"}" da lista de emissão?`)) {
                        linha.remove();
                        linhas = Array.from(tabelaBody.querySelectorAll(".linha-participante"));
                        filtrarParticipantes();
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
                    aplicarPaginacao();
                }
            });
        }

        if (chkMaster) {
            chkMaster.addEventListener("change", function () {
                const inicio = (paginaAtual - 1) * ITENS_POR_PAGINA;
                const fim = inicio + ITENS_POR_PAGINA;
                const linhasPagina = linhasFiltradas.slice(inicio, fim);
                linhasPagina.forEach(l => {
                    const c = l.querySelector(".chk-participante-item");
                    if (c) c.checked = chkMaster.checked;
                });
                atualizarContadores();
            });
        }

        if (btnSelecionarTodosPagina) {
            btnSelecionarTodosPagina.addEventListener("click", function () {
                const inicio = (paginaAtual - 1) * ITENS_POR_PAGINA;
                const fim = inicio + ITENS_POR_PAGINA;
                linhasFiltradas.slice(inicio, fim).forEach(l => {
                    const c = l.querySelector(".chk-participante-item");
                    if (c) c.checked = true;
                });
                if (chkMaster) chkMaster.checked = true;
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
            filtroInput.addEventListener("input", filtrarParticipantes);
        }

        if (btnLimparBusca && filtroInput) {
            btnLimparBusca.addEventListener("click", function () {
                filtroInput.value = "";
                filtrarParticipantes();
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

        // Alteração de Evento Vinculado
        if (selectEvento) {
            selectEvento.addEventListener("change", function () {
                const idEvento = this.value;
                if (idEvento && idEvento !== "0") {
                    window.location.href = `/Modelocracha/EmitirCracha?idEvento=${idEvento}`;
                }
            });
        }

        // Alteração de Gabarito / Modelo
        if (selectModelo) {
            selectModelo.addEventListener("change", function () {
                const opt = this.options[this.selectedIndex];
                if (opt) {
                    const texto = opt.dataset.texto;
                    const qrcode = opt.dataset.qrcode;

                    if (lblTextoCracha && texto) {
                        lblTextoCracha.textContent = texto;
                    }

                    if (blocoQrCode) {
                        blocoQrCode.style.display = (qrcode === "0" || qrcode === 0) ? "none" : "flex";
                    }
                }
            });
        }

        // Zoom no Card
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

        // Tela cheia
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

        // Exportar PNG em Alta Resolução
        if (btnExportarPng && crachaCardElement) {
            btnExportarPng.addEventListener("click", function () {
                renderizarEBaixarPng();
            });
        }

        function renderizarEBaixarPng() {
            try {
                const canvas = document.createElement("canvas");
                const ctx = canvas.getContext("2d");
                const largura = 640;
                const altura = 920;
                canvas.width = largura;
                canvas.height = altura;

                // Fundo do Crachá
                ctx.fillStyle = "#FFFFFF";
                ctx.beginPath();
                ctx.roundRect(0, 0, largura, altura, 36);
                ctx.fill();

                // Cabeçalho Azul
                ctx.fillStyle = "#232ed0";
                ctx.beginPath();
                ctx.roundRect(0, 0, largura, 200, [36, 36, 0, 0]);
                ctx.fill();

                // Furo do crachá
                ctx.fillStyle = "#EEF2FA";
                ctx.beginPath();
                ctx.roundRect(largura / 2 - 40, 16, 80, 20, 10);
                ctx.fill();

                // Títulos do Cabeçalho
                ctx.fillStyle = "#FFFFFF";
                ctx.font = "bold 18px Segoe UI, sans-serif";
                ctx.textAlign = "center";
                ctx.fillText("BORA LÁ EVENTOS", largura / 2, 80);

                const nomeEvento = (document.getElementById("lblPreviewNomeEvento")?.textContent || "EVENTO").toUpperCase();
                ctx.font = "bold 24px Segoe UI, sans-serif";
                ctx.fillText(nomeEvento, largura / 2, 130);

                const subEvento = (document.getElementById("lblPreviewSubtituloEvento")?.textContent || "TECNOLOGIA & GOVERNANÇA 2025").toUpperCase();
                ctx.font = "bold 16px Segoe UI, sans-serif";
                ctx.fillStyle = "#E0E7FF";
                ctx.fillText(subEvento, largura / 2, 165);

                // Foto ou Avatar
                const centroY = 320;
                ctx.save();
                ctx.beginPath();
                ctx.arc(largura / 2, centroY, 70, 0, Math.PI * 2, true);
                ctx.closePath();
                ctx.clip();
                if (previewFoto && previewFoto.style.display !== "none" && previewFoto.src) {
                    ctx.drawImage(previewFoto, largura / 2 - 70, centroY - 70, 140, 140);
                } else {
                    ctx.fillStyle = "#E2E8F0";
                    ctx.fillRect(largura / 2 - 70, centroY - 70, 140, 140);
                    ctx.fillStyle = "#475569";
                    ctx.beginPath();
                    ctx.arc(largura / 2, centroY - 20, 30, 0, Math.PI * 2);
                    ctx.fill();
                }
                ctx.restore();

                // Selo de verificado
                ctx.fillStyle = "#10B981";
                ctx.beginPath();
                ctx.arc(largura / 2 + 50, centroY + 50, 18, 0, Math.PI * 2);
                ctx.fill();
                ctx.fillStyle = "#FFFFFF";
                ctx.font = "bold 18px Segoe UI, sans-serif";
                ctx.fillText("✓", largura / 2 + 50, centroY + 56);

                // Nome do participante
                const nomePart = (previewNome?.textContent || "PARTICIPANTE").toUpperCase();
                ctx.fillStyle = "#0F172A";
                ctx.font = "bold 32px Segoe UI, sans-serif";
                ctx.textAlign = "center";
                ctx.fillText(nomePart, largura / 2, 450);

                // Organização
                const orgPart = previewOrg?.textContent || "";
                ctx.fillStyle = "#64748B";
                ctx.font = "500 20px Segoe UI, sans-serif";
                ctx.fillText(orgPart, largura / 2, 485);

                // Pílula de Papel
                const papelTexto = (previewPapelTexto?.textContent || "PARTICIPANTE").toUpperCase();
                ctx.fillStyle = "#232ed0";
                ctx.beginPath();
                ctx.roundRect(largura / 2 - 130, 520, 260, 44, 22);
                ctx.fill();
                ctx.fillStyle = "#FFFFFF";
                ctx.font = "bold 20px Segoe UI, sans-serif";
                ctx.fillText(papelTexto, largura / 2, 550);

                // Metadados CPF e Credencial
                ctx.fillStyle = "#64748B";
                ctx.font = "16px Segoe UI, sans-serif";
                const cpfTexto = previewCpf?.textContent || "";
                const credTexto = previewCredencial?.textContent || "";
                ctx.fillText(`CPF: ${cpfTexto}       Credencial: ${credTexto}`, largura / 2, 605);

                // Caixa de QR Code e Código de Presença
                ctx.fillStyle = "#FFFFFF";
                ctx.strokeStyle = "#E2E8F0";
                ctx.lineWidth = 2;
                ctx.beginPath();
                ctx.roundRect(60, 635, largura - 120, 175, 16);
                ctx.fill();
                ctx.stroke();

                if (previewQrCodeImg && previewQrCodeImg.style.display !== "none" && previewQrCodeImg.src) {
                    ctx.drawImage(previewQrCodeImg, 80, 650, 140, 140);
                }

                ctx.textAlign = "left";
                ctx.fillStyle = "#64748B";
                ctx.font = "bold 14px Segoe UI, sans-serif";
                ctx.fillText("CÓDIGO DE PRESENÇA", 240, 690);

                const presencaTexto = previewCodigoPresenca?.textContent || "BL - EVT - 2025 - 00142";
                ctx.fillStyle = "#0F172A";
                ctx.font = "bold 20px Consolas, monospace";
                ctx.fillText(presencaTexto, 240, 725);

                ctx.fillStyle = "#232ed0";
                ctx.font = "bold 15px Segoe UI, sans-serif";
                ctx.fillText("✓ Válido em todas salas", 240, 765);

                // Rodapé
                ctx.fillStyle = "#F8FAFC";
                ctx.beginPath();
                ctx.roundRect(0, altura - 75, largura, 75, [0, 0, 36, 36]);
                ctx.fill();

                ctx.fillStyle = "#475569";
                ctx.font = "italic 15px Segoe UI, sans-serif";
                ctx.textAlign = "center";
                const textoFooter = lblTextoCracha?.textContent || "Uso obrigatório e intransferível nas dependências do evento.";
                ctx.fillText(textoFooter.length > 60 ? textoFooter.substring(0, 57) + "..." : textoFooter, largura / 2, altura - 30);

                // Download da imagem
                const link = document.createElement("a");
                const nomeLimpo = nomePart.replace(/[^a-zA-Z0-9]/g, "_").toLowerCase();
                link.download = `cracha_${nomeLimpo}.png`;
                link.href = canvas.toDataURL("image/png");
                link.click();
            } catch (err) {
                console.error("Falha ao exportar imagem PNG do crachá:", err);
                alert("Não foi possível gerar a exportação PNG neste navegador. Tente a impressão via PDF.");
            }
        }

        // Impressão em Lote (PDF A4)
        if (btnImprimirLote && containerImpressaoLote) {
            btnImprimirLote.addEventListener("click", function () {
                const linhasSelecionadas = linhas.filter(l => l.querySelector(".chk-participante-item")?.checked);

                if (linhasSelecionadas.length === 0) {
                    alert("Por favor, selecione pelo menos um participante para impressão.");
                    return;
                }

                containerImpressaoLote.innerHTML = "";
                const grid = document.createElement("div");
                grid.className = "print-grid-lote";

                const nomeEvento = document.getElementById("lblPreviewNomeEvento")?.textContent || "EVENTO";
                const subEvento = document.getElementById("lblPreviewSubtituloEvento")?.textContent || "TECNOLOGIA & GOVERNANÇA 2025";
                const textoRodape = lblTextoCracha?.textContent || "Uso pessoal e intransferível.";
                const logoSrc = document.getElementById("crachaLogoElement")?.src || "";

                linhasSelecionadas.forEach(linha => {
                    const card = document.createElement("div");
                    card.className = "cracha-print-item";

                    const nome = (linha.dataset.nome || linha.dataset.nomeCompleto || "").toUpperCase();
                    const depto = linha.dataset.depto || "";
                    const papel = (linha.dataset.papel || "PARTICIPANTE").toUpperCase();
                    const cpf = linha.dataset.cpf || "";
                    const cred = linha.dataset.credencial || "";
                    const presenca = linha.dataset.presenca || "";
                    const qrcode = linha.dataset.qrcode || "";

                    card.innerHTML = `
                        <div class="cracha-physical-card">
                            <div class="cracha-hole"></div>
                            <div class="cracha-header">
                                <div class="cracha-header-sub"><span>BORA LÁ EVENTOS</span></div>
                                ${logoSrc ? `<div class="cracha-logo-institucional-container"><img src="${logoSrc}" class="cracha-logo-institucional-img" alt="Logo"/></div>` : ''}
                                <h6 class="cracha-evento-title">${nomeEvento}</h6>
                                <div class="cracha-evento-subtitle">${subEvento}</div>
                            </div>
                            <div class="cracha-body">
                                <div class="participant-name">${nome}</div>
                                <div class="participant-org">${depto}</div>
                                <div class="badge-role-pill">${papel}</div>
                                <div class="cracha-meta-info-row">
                                    <div>CPF: <strong>${cpf}</strong></div>
                                    <div>Credencial: <strong>${cred}</strong></div>
                                </div>
                                <div class="cracha-presenca-box">
                                    <div class="cracha-qrcode-box">
                                        ${qrcode ? `<img src="data:image/png;base64,${qrcode}" style="width:100%;height:100%;object-fit:contain;"/>` : ''}
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
                    grid.appendChild(card);
                });

                containerImpressaoLote.appendChild(grid);
                window.print();
            });
        }

        // Inicialização
        filtrarParticipantes();
        atualizarContadores();
        if (linhasFiltradas.length > 0) {
            atualizarPreviewComLinha(linhasFiltradas[0]);
        }
    });
})();
