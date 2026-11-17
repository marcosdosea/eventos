(function (global) {
    "use strict";

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

    async function renderizarCrachaParaCanvas(dadosParticipante, opcoes = {}) {
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

        const logoImg = opcoes.logoImg || document.getElementById("crachaLogoElement");
        const logoContainer = opcoes.logoContainer || document.getElementById("crachaLogoContainer");
        let titleStartY = 96;
        if (logoImg && (!logoContainer || logoContainer.style.display !== "none") && logoImg.src) {
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

        const nomeEvento = (opcoes.nomeEvento || document.getElementById("lblPreviewNomeEvento")?.textContent || "EVENTO").trim().toUpperCase();
        const elSub = opcoes.subtituloElement || document.getElementById("lblPreviewSubtituloEvento");
        const subEvento = (opcoes.subtitulo != null ? opcoes.subtitulo : (elSub && elSub.style.display !== "none" ? elSub.textContent.trim() : "")).toUpperCase();

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

        const optModelo = opcoes.selectModelo
            ? opcoes.selectModelo.options[opcoes.selectModelo.selectedIndex]
            : document.getElementById("selectModeloEmitir")?.selectedOptions?.[0];
        const blocoQrCode = opcoes.blocoQrCode || document.getElementById("blocoQrCodePreview");
        const qrCodeHabilitado = opcoes.qrcodeHabilitado != null
            ? opcoes.qrcodeHabilitado
            : (optModelo
                ? (optModelo.dataset.qrcode !== "0" && optModelo.dataset.qrcode !== 0)
                : (blocoQrCode ? blocoQrCode.style.display !== "none" : true));

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
        const textoFooter = (opcoes.textoFooter || document.getElementById("lblPreviewTextoCracha")?.textContent || "Uso obrigatório e intransferível nas dependências do evento.").trim();

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

    const CrachaCanvas = {
        obterIniciais,
        ehPayloadImagemValido,
        carregarImagemSegura,
        quebrarTextoEmLinhas,
        desenharAvatarPadraoCanvas,
        desenharQrCodeFallbackCanvas,
        renderizarCrachaParaCanvas
    };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = CrachaCanvas;
    }
    global.CrachaCanvas = CrachaCanvas;
})(typeof window !== "undefined" ? window : this);

