document.addEventListener("DOMContentLoaded", function () {
    const btn = document.getElementById("btnReenviar");
    const spanTempo = document.getElementById("tempoRestante");
    const tempoEsperaSegundos = 60;

    function iniciarCountdown(segundosRestantes) {
        btn.classList.add("disabled");
        btn.setAttribute("aria-disabled", "true");
        btn.style.pointerEvents = "none";
        spanTempo.style.display = "inline";

        const interval = setInterval(function () {
            segundosRestantes--;
            spanTempo.innerText = `Aguarde ${segundosRestantes}s para reenviar.`;

            if (segundosRestantes <= 0) {
                clearInterval(interval);
                btn.classList.remove("disabled");
                btn.removeAttribute("aria-disabled");
                btn.style.pointerEvents = "auto";
                spanTempo.style.display = "none";
                localStorage.removeItem("tempoReenvioEmail");
            }
        }, 1000);
    }

    
    const tempoSalvo = localStorage.getItem("tempoReenvioEmail");
    if (tempoSalvo) {
        const segundosDecorridos = Math.floor((Date.now() - parseInt(tempoSalvo, 10)) / 1000);
        const restantes = tempoEsperaSegundos - segundosDecorridos;

        if (restantes > 0) {
            iniciarCountdown(restantes);
        } else {
            localStorage.removeItem("tempoReenvioEmail");
        }
    }

   
    btn.addEventListener("click", function (e) {
        if (btn.classList.contains("disabled")) {
            e.preventDefault();
            return;
        }
       
        localStorage.setItem("tempoReenvioEmail", Date.now().toString());
    });
});