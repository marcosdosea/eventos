using AutoMapper;
using Core.Service;
using Core;
using Core.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class InscricaoController : Controller
    {
        private readonly IEventoService _eventoService;
        private readonly IPessoaService _pessoaService;
        private readonly IInscricaoService _inscricaoService;
        private readonly ISubeventoService _subeventoService;
        private readonly UserManager<UsuarioIdentity> _userManager;
        private readonly ITipoInscricaoService _tipoinscricaoService;
        private readonly IMapper _mapper;

        public InscricaoController(UserManager<UsuarioIdentity> userManager, ITipoInscricaoService tipoinscricaoService, IEventoService eventoService, IMapper mapper, IInscricaoService inscricaoService, IPessoaService pessoaService, ISubeventoService subeventoService)
        {
            _tipoinscricaoService = tipoinscricaoService;
            _eventoService = eventoService;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
            _pessoaService = pessoaService;
            _subeventoService = subeventoService;
            _userManager = userManager;
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(int id)
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("Create")]
        public ActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("Edit/{id}")]
        public ActionResult Edit(int id)
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("Delete/{id}")]
        public ActionResult Delete(int id)
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [Authorize]
        [HttpGet]
        [Route("PessoaAllInscricao")]
        public IActionResult pessoaAllInscricao()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        [Route("RealizarInscricao/{idEvento}/{idSubevento?}")]
        public IActionResult realizarInscricao(uint idEvento, uint? idSubevento)
        {
            Evento evento = _eventoService.Get(idEvento);
            if (evento == null)
            {
                return RedirectToAction("Index", "Home");
            }

            EventoModel eventoModel = _mapper.Map<EventoModel>(evento);
            var tipoInscricaoModel = _tipoinscricaoService.GetByEvento(idEvento).ToList();
            var subeventos = _subeventoService.GetByIdEvento(idEvento).ToList();
            var subeventosOpcoes = new List<SubeventoOpcao>();
            foreach(var sub in subeventos)
            {
                var tipos = _tipoinscricaoService.GetTiposInscricaosSubevento(sub.Id);
                subeventosOpcoes.Add(new SubeventoOpcao { Subevento = sub, TiposInscricao = tipos });
            }

            var model = new InscricaoEventoViewModel(){
                tipoInscricao = tipoInscricaoModel,
                eventoNavigation = eventoModel,
                SubeventosOpcoes = subeventosOpcoes
            };

            if (User.Identity != null && !string.IsNullOrEmpty(User.Identity.Name))
            {
                var pessoa = _pessoaService.GetByCpf(User.Identity.Name);
                if (pessoa != null && _inscricaoService.IsInscrito(pessoa.Id, idEvento))
                {
                    ViewBag.JaInscrito = true;
                }
            }
            
            if (evento.PossuiCertificado == 0)
            {
                return View("RealizarInscricaoLote", model);
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [Route("RealizarInscricao/{idEvento}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> realizarInscricao(uint idEvento, InscricaoEventoModel inscricaoEvento)
        {
            var pessoa = _pessoaService.GetByCpf(User.Identity.Name);
            if (pessoa == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (_inscricaoService.IsInscrito(pessoa.Id, idEvento))
            {
                TempData["ParticipanteMessage"] = "Você já está inscrito neste evento!";
                return RedirectToAction("minhasInscricoes", new { idEvento = idEvento });
            }

            var evento = _eventoService.Get(idEvento);

            var mainEventQuantities = new Dictionary<uint, int>();
            int totalEventTickets = 0;

            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("QuantidadeTipoInscricao_"))
                {
                    if (uint.TryParse(key.Replace("QuantidadeTipoInscricao_", ""), out uint idTipo))
                    {
                        if (int.TryParse(Request.Form[key], out int qtd) && qtd > 0)
                        {
                            mainEventQuantities[idTipo] = qtd;
                            totalEventTickets += qtd;
                        }
                    }
                }
            }

            if (evento.PossuiCertificado != 0 || totalEventTickets < 1)
            {
                totalEventTickets = 1;
                if (mainEventQuantities.Count > 0)
                {
                    var firstKey = mainEventQuantities.Keys.First();
                    mainEventQuantities.Clear();
                    mainEventQuantities[firstKey] = 1;
                }
                else
                {
                    mainEventQuantities[inscricaoEvento.IdTipoInscricao ?? 0] = 1;
                }
            }
            else if (totalEventTickets > 8)
            {
                TempData["ParticipanteMessage"] = "Limite máximo de ingressos excedido.";
                return RedirectToAction("Index", "Home"); 
            }

            foreach (var kvp in mainEventQuantities)
            {
                uint idTipo = kvp.Key;
                int quantidade = kvp.Value;

                decimal valorMain = 0m;
                uint? idTipoToSave = idTipo;

                if (idTipo != 0 && idTipo != 999999)
                {
                    var tipoObjMain = _tipoinscricaoService.Get(idTipo);
                    valorMain = tipoObjMain != null ? tipoObjMain.Valor : 0m;
                }
                else if (idTipo == 999999)
                {
                    valorMain = evento != null ? (evento.ValorInscricao / 2m) : 0m;
                    idTipoToSave = 0;
                }
                else
                {
                    valorMain = evento != null ? evento.ValorInscricao : 0m;
                }

                for (int i = 0; i < quantidade; i++)
                {
                    var novaInscricao = new InscricaoEventoModel()
                    {
                        IdPessoa = pessoa.Id,
                        IdEvento = idEvento,
                        IdPapel = 4,
                        DataInscricao = DateTime.Now,
                        NomeCracha = User.Identity.Name,
                        Status = "S",
                        IdTipoInscricao = idTipoToSave,
                        FrequenciaFinal = 0m,
                        ValorTotal = valorMain 
                    };

                    var inscricao = _mapper.Map<Inscricaopessoaevento>(novaInscricao);
                    _inscricaoService.CreateInscricaoEvento(inscricao);
                    _eventoService.AtualizarVagasDisponiveis(idEvento);
                }
            }

            if (inscricaoEvento.SelectedSubeventos != null && inscricaoEvento.SelectedSubeventos.Any())
            {
                foreach (var idSubevento in inscricaoEvento.SelectedSubeventos)
                {
                    var subevento = _subeventoService.Get(idSubevento);
                    var subEventQuantities = new Dictionary<uint, int>();
                    int totalSubTickets = 0;

                    foreach (var key in Request.Form.Keys)
                    {
                        string prefix = $"QuantidadeTipoInscricaoSubevento_{idSubevento}_";
                        if (key.StartsWith(prefix))
                        {
                            if (uint.TryParse(key.Replace(prefix, ""), out uint idTipoSub))
                            {
                                if (int.TryParse(Request.Form[key], out int qtd) && qtd > 0)
                                {
                                    subEventQuantities[idTipoSub] = qtd;
                                    totalSubTickets += qtd;
                                }
                            }
                        }
                    }
                    
                    if (totalSubTickets > 8)
                    {
                        continue; 
                    }

                    foreach (var kvpSub in subEventQuantities)
                    {
                        uint idTipoSub = kvpSub.Key;
                        int quantidadeSub = kvpSub.Value;

                        decimal valorSub = 0m;
                        if (idTipoSub != 0 && idTipoSub != 999999)
                        {
                            var tipoObjSub = _tipoinscricaoService.Get(idTipoSub);
                            valorSub = tipoObjSub != null ? tipoObjSub.Valor : 0m;
                        }
                        else if (idTipoSub == 999999)
                        {
                            valorSub = subevento != null ? (subevento.ValorInscricao / 2m) : 0m;
                        }
                        else
                        {
                            valorSub = subevento != null ? subevento.ValorInscricao : 0m;
                        }

                        for (int j = 0; j < quantidadeSub; j++)
                        {
                            var novaInscricaoSub = new Inscricaopessoasubevento()
                            {
                                IdPessoa = pessoa.Id,
                                IdSubEvento = idSubevento,
                                IdPapel = 4,
                                DataInscricao = DateTime.Now,
                                Status = "S",
                                FrequenciaFinal = 0m,
                                Valor = valorSub,
                            };
                            _inscricaoService.CreateInscricaoSubEvento(novaInscricaoSub);
                            _subeventoService.AtualizarVagasDisponiveis(idSubevento);
                        }
                    }
                }
            }

            TempData["ParticipanteSuccessMessage"] = "Inscrição realizada com sucesso!";
            return RedirectToAction("minhasInscricoes", new { idEvento = idEvento });
        }

        [Authorize]
        [HttpGet]
        [Route("MinhasInscricoes")]
        public async Task<IActionResult> minhasInscricoes(uint? idEvento)
        {
            var inscricaoUser = _inscricaoService.GetAllEventsByUserId(User.Identity.Name);
            var listarEventosModel = inscricaoUser.Select(i => new InscricaoEventoModel
            {
                Id = i.Id,
                IdEvento = i.IdEvento,
                DataInscricao = (DateTime)i.DataInscricao,
                NomeCracha = i.NomeCracha,
                Status = i.Status,
                FrequenciaFinal = i.FrequenciaFinal,
                IdEventoNavigation = i.IdEventoNavigation
            }).ToList();

            ViewBag.EventoId = idEvento ?? listarEventosModel.FirstOrDefault()?.IdEvento;
            return View(listarEventosModel);
        }

    }
}
