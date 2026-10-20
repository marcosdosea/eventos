using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class TransacaoService : ITransacaoService
    {
        private readonly EventoContext _context;

        public TransacaoService(EventoContext context)
        {
            _context = context;
        }

        public void Executar(Action acao)
        {
            Executar<object?>(() =>
            {
                acao();
                return null;
            });
        }

        public TResult Executar<TResult>(Func<TResult> acao)
        {
            // Provedor não relacional (InMemory): sem transação explícita.
            if (!_context.Database.IsRelational())
            {
                return acao();
            }

            var transaction = _context.Database.BeginTransaction();
            try
            {
                var resultado = acao();
                transaction.Commit();
                return resultado;
            }
            catch
            {
                transaction.Rollback();

                // Evita que um SaveChanges posterior tente reinserir entidades
                // que já estavam pendentes no tracker quando o rollback ocorreu.
                _context.ChangeTracker.Clear();

                throw;
            }
            finally
            {
                transaction.Dispose();
            }
        }
    }
}
