(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        const selectEvento = document.getElementById("selectEventoEmitir");
        const selectModelo = document.getElementById("selectModeloEmitir");
        const btnImprimirLote = document.getElementById("btnImprimirLotePdf");

        const chkMaster = document.getElementById("chkSelecionarTodosHeader");
        const btnSelecionarTodosPagina = document.getElementById("btnSelecionarTodosPagina") || document.getElementById("btnSelecionarTodos");
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

            // Atualiza estado do checkbox master refletindo todos os participantes (todas as páginas)
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
                ctx.roundRect(0, 0, largura, 205, [36, 36, 0, 0]);
                ctx.fill();

                // Furo do crachá
                ctx.fillStyle = "#EEF2FA";
                ctx.beginPath();
                ctx.roundRect(largura / 2 - 40, 16, 80, 20, 10);
                ctx.fill();

                // Título Institucional Superior
                ctx.fillStyle = "#FFFFFF";
                ctx.font = "bold 13px Segoe UI, sans-serif";
                ctx.textAlign = "center";
                ctx.fillText("BORA LÁ EVENTOS", largura / 2, 60);

                // Logotipo Institucional se presente
                const logoImg = document.getElementById("crachaLogoElement");
                let titleStartY = 96;
                if (logoImg && logoImg.style.display !== "none" && logoImg.src && logoImg.complete && logoImg.naturalWidth > 0) {
                    try {
                        const logoMaxW = 160;
                        const logoMaxH = 34;
                        let logoW = logoImg.naturalWidth;
                        let logoH = logoImg.naturalHeight;
                        const scale = Math.min(logoMaxW / logoW, logoMaxH / logoH);
                        logoW *= scale;
                        logoH *= scale;
                        ctx.drawImage(logoImg, largura / 2 - logoW / 2, 68, logoW, logoH);
                        titleStartY = 118;
                    } catch (e) {
                    }
                }

                // Nome do Evento (quebra automática de linhas e ajuste de fonte para nunca cortar)
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

                // Centralização vertical matemática no cabeçalho azul
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
                    // Cabeça do avatar
                    ctx.fillStyle = "#475569";
                    ctx.beginPath();
                    ctx.arc(largura / 2, centroY - 20, 30, 0, Math.PI * 2);
                    ctx.fill();
                    // Ombros do avatar
                    ctx.fillStyle = "#1E293B";
                    ctx.beginPath();
                    ctx.arc(largura / 2, centroY + 68, 52, Math.PI, 0, false);
                    ctx.fill();
                }
                ctx.restore();

                // Selo de verificado
                const linhaAtiva = document.querySelector(".linha-ativa-preview");
                const aptoExport = linhaAtiva ? (linhaAtiva.dataset.apto === "true") : true;
                ctx.fillStyle = aptoExport ? "#10B981" : "#F59E0B";
                ctx.beginPath();
                ctx.arc(largura / 2 + 50, centroY + 50, 18, 0, Math.PI * 2);
                ctx.fill();
                ctx.fillStyle = "#FFFFFF";
                ctx.font = "bold 18px Segoe UI, sans-serif";
                ctx.fillText(aptoExport ? "✓" : "!", largura / 2 + 50, centroY + 56);

                // Nome do participante (auto-ajuste de tamanho de fonte para nomes extensos)
                const nomePart = (previewNome?.textContent || "PARTICIPANTE").trim().toUpperCase();
                ctx.fillStyle = "#0F172A";
                let nomeFontSize = 30;
                ctx.font = `bold ${nomeFontSize}px Segoe UI, sans-serif`;
                while (ctx.measureText(nomePart).width > 540 && nomeFontSize > 18) {
                    nomeFontSize -= 2;
                    ctx.font = `bold ${nomeFontSize}px Segoe UI, sans-serif`;
                }
                ctx.textAlign = "center";
                ctx.fillText(nomePart, largura / 2, 450);

                // Organização ou Departamento (auto-ajuste para nunca estourar)
                const orgPart = (previewOrg?.textContent || "").trim();
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

                // Pílula de Papel
                const papelTexto = (previewPapelTexto?.textContent || "PARTICIPANTE").trim().toUpperCase();
                ctx.font = "bold 18px Segoe UI, sans-serif";
                const textPapelWidth = ctx.measureText(papelTexto).width;
                const pillWidth = Math.max(220, Math.min(360, textPapelWidth + 50));
                ctx.fillStyle = "#232ed0";
                ctx.beginPath();
                ctx.roundRect(largura / 2 - pillWidth / 2, 516, pillWidth, 42, 21);
                ctx.fill();
                ctx.fillStyle = "#FFFFFF";
                ctx.fillText(papelTexto, largura / 2, 544);

                // Metadados CPF e Credencial
                ctx.fillStyle = "#64748B";
                ctx.font = "15px Segoe UI, sans-serif";
                const cpfTexto = previewCpf?.textContent || "";
                const credTexto = previewCredencial?.textContent || "";
                ctx.fillText(`CPF: ${cpfTexto}       Credencial: ${credTexto}`, largura / 2, 598);

                // Caixa de QR Code e Código de Presença
                ctx.fillStyle = "#FFFFFF";
                ctx.strokeStyle = "#E2E8F0";
                ctx.lineWidth = 2;
                ctx.beginPath();
                ctx.roundRect(50, 625, largura - 100, 165, 16);
                ctx.fill();
                ctx.stroke();

                if (previewQrCodeImg && previewQrCodeImg.style.display !== "none" && previewQrCodeImg.src) {
                    ctx.drawImage(previewQrCodeImg, 70, 640, 135, 135);
                }

                ctx.textAlign = "left";
                ctx.fillStyle = "#64748B";
                ctx.font = "bold 13px Segoe UI, sans-serif";
                ctx.fillText("CÓDIGO DE PRESENÇA", 225, 672);

                const presencaTexto = previewCodigoPresenca?.textContent || "BL - EVT - 2025 - 00142";
                ctx.fillStyle = "#0F172A";
                ctx.font = "bold 18px Consolas, monospace";
                ctx.fillText(presencaTexto, 225, 706);

                ctx.fillStyle = "#232ed0";
                ctx.font = "bold 14px Segoe UI, sans-serif";
                ctx.fillText("✓ Válido em todas salas", 225, 744);

                // Rodapé com quebra de linhas para nunca cortar texto com reticências
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

                // Download da imagem PNG
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

                // SVG vetorial de QR Code para contingência e renderização determinística
                const fallbackQrSvg = `<svg viewBox="0 0 100 100" width="70" height="70" style="display:block;margin:auto;"><rect width="100" height="100" fill="#FFFFFF"/><rect x="8" y="8" width="28" height="28" fill="#0F172A"/><rect x="14" y="14" width="16" height="16" fill="#FFFFFF"/><rect x="18" y="18" width="8" height="8" fill="#0F172A"/><rect x="64" y="8" width="28" height="28" fill="#0F172A"/><rect x="70" y="14" width="16" height="16" fill="#FFFFFF"/><rect x="74" y="18" width="8" height="8" fill="#0F172A"/><rect x="8" y="64" width="28" height="28" fill="#0F172A"/><rect x="14" y="70" width="16" height="16" fill="#FFFFFF"/><rect x="18" y="74" width="8" height="8" fill="#0F172A"/><rect x="44" y="12" width="10" height="10" fill="#0F172A"/><rect x="44" y="30" width="10" height="16" fill="#0F172A"/><rect x="12" y="44" width="16" height="10" fill="#0F172A"/><rect x="48" y="48" width="16" height="16" fill="#0F172A"/><rect x="72" y="44" width="14" height="10" fill="#0F172A"/><rect x="48" y="72" width="12" height="16" fill="#0F172A"/><rect x="70" y="70" width="18" height="18" fill="#0F172A"/></svg>`;

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
                                ${logoSrc ? `<div class="cracha-logo-institucional-container"><img src="${logoSrc}" class="cracha-logo-institucional-img" alt="Logo"/></div>` : ''}
                                <h6 class="cracha-evento-title">${nomeEvento}</h6>
                                ${subEvento ? `<div class="cracha-evento-subtitle">${subEvento}</div>` : ''}
                            </div>
                            <div class="cracha-body">
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
                    grid.appendChild(card);
                });

                document.body.classList.add("imprimindo-lote");
                containerImpressaoLote.classList.remove("d-none");
                containerImpressaoLote.appendChild(grid);

                // Aguarda a decodificação de todas as imagens antes de abrir a janela de impressão
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

        // Inicialização
        filtrarParticipantes();
        atualizarContadores();
        if (linhasFiltradas.length > 0) {
            atualizarPreviewComLinha(linhasFiltradas[0]);
        }
    });
})();
