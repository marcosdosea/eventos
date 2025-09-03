using Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventoWeb.Filters
{
    public class NomeUsuarioFilter : IActionFilter
    {
        private readonly IPessoaService _pessoaService;

        public NomeUsuarioFilter(IPessoaService pessoaService)
        {
            _pessoaService = pessoaService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            var user = context.HttpContext.User;

            if (controller != null && user.Identity != null && user.Identity.IsAuthenticated)
            {
                var cpf = user.Identity.Name; // o CPF que está vindo do login
                var pessoa = _pessoaService.GetByCpf(cpf); // precisa existir no seu service
                controller.ViewBag.NomeUsuario = pessoa?.Nome ?? "Usuário";
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
