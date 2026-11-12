(function (global) {
    "use strict";

    const MENSAGEM_ERRO_EXPORTACAO_PNG = "Não foi possível exportar o PNG. Verifique se a imagem do participante está válida ou tente novamente.";

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

    async function exportarCrachaIndividualPng(dadosParticipante, opcoes = {}) {
        const canvasRenderer = (global.CrachaCanvas && global.CrachaCanvas.renderizarCrachaParaCanvas) || opcoes.renderizarCrachaParaCanvas;
        if (typeof canvasRenderer !== "function") {
            throw new Error("Renderizador de crachá em Canvas não encontrado.");
        }

        const canvas = await canvasRenderer(dadosParticipante, opcoes);
        const dataUrl = canvas.toDataURL("image/png");
        if (!dataUrl || !dataUrl.startsWith("data:image/png")) {
            throw new Error("Falha ao serializar imagem PNG do canvas.");
        }

        const link = document.createElement("a");
        const nomeLimpo = (dadosParticipante.nome || "participante").replace(/[^a-zA-Z0-9]/g, "_").toLowerCase();
        link.download = `cracha_${nomeLimpo}.png`;
        link.href = dataUrl;
        document.body.appendChild(link);
        link.click();
        link.remove();
    }

    async function exportarLotePngZip(linhasOuParticipantes, opcoes = {}) {
        const extrairDados = opcoes.extrairDados || (l => l);
        const canvasRenderer = (global.CrachaCanvas && global.CrachaCanvas.renderizarCrachaParaCanvas) || opcoes.renderizarCrachaParaCanvas;
        if (typeof canvasRenderer !== "function") {
            throw new Error("Renderizador de crachá em Canvas não encontrado.");
        }

        const arquivosPng = [];
        const nomesUsados = new Set();
        const total = linhasOuParticipantes.length;

        for (let i = 0; i < total; i++) {
            const item = linhasOuParticipantes[i];
            const dados = extrairDados(item);

            const canvas = await canvasRenderer(dados, opcoes);
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
        const nomeEventoBruto = opcoes.nomeEvento || document.getElementById("lblPreviewNomeEvento")?.textContent || "evento";
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
        return arquivosPng.length;
    }

    function executarImpressaoLoteA4(linhasSelecionadas, opcoes = {}) {
        const containerImpressaoLote = opcoes.container || document.getElementById("containerImpressaoLoteA4");
        if (!containerImpressaoLote) return;

        const ehImagemValida = (global.CrachaCanvas && global.CrachaCanvas.ehPayloadImagemValido) || (() => true);

        const optModelo = opcoes.selectModelo ? opcoes.selectModelo.options[opcoes.selectModelo.selectedIndex] : document.getElementById("selectModeloEmitir")?.selectedOptions?.[0];
        const modeloQrcodeHabilitado = optModelo ? (optModelo.dataset.qrcode !== "0" && optModelo.dataset.qrcode !== 0) : true;

        containerImpressaoLote.innerHTML = "";
        const grid = document.createElement("div");
        grid.className = "print-grid-lote";

        const nomeEvento = document.getElementById("lblPreviewNomeEvento")?.textContent || "EVENTO";
        const elSubPrint = document.getElementById("lblPreviewSubtituloEvento");
        const subEvento = (elSubPrint && elSubPrint.style.display !== "none" ? elSubPrint.textContent.trim() : "");
        const textoRodape = document.getElementById("lblPreviewTextoCracha")?.textContent || "Uso pessoal e intransferível.";
        const logoSrc = document.getElementById("crachaLogoElement")?.src || "";
        const crachaLogoContainer = document.getElementById("crachaLogoContainer");
        const logoVisivel = crachaLogoContainer && crachaLogoContainer.style.display !== "none" && ehImagemValida(logoSrc);

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
            const temFotoValida = ehImagemValida(fotoUrl);
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
                return img.decode().catch(() => { });
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
    }

    const CrachaExport = {
        MENSAGEM_ERRO_EXPORTACAO_PNG,
        calcularCrc32,
        dataUrlParaUint8Array,
        gerarArquivoZip,
        exportarCrachaIndividualPng,
        exportarLotePngZip,
        executarImpressaoLoteA4
    };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = CrachaExport;
    }
    global.CrachaExport = CrachaExport;
})(typeof window !== "undefined" ? window : this);

