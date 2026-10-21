namespace Core.Service
{
    /// <summary>
    /// Centraliza o controle transacional das operações críticas (Issue #841).
    ///
    /// A transação deve envolver o caso de uso inteiro (inscrição em lote,
    /// desistência, exclusão de evento), e não um trecho isolado do serviço.
    /// Como os serviços compartilham o mesmo <c>EventoContext</c> (scoped),
    /// abrir a transação aqui faz com que todos os <c>SaveChanges</c> executados
    /// dentro da ação sejam enlistados na mesma transação.
    ///
    /// Em provedores não relacionais (InMemory, usado nos testes unitários)
    /// a ação é executada sem transação explícita, preservando os testes atuais.
    /// </summary>
    public interface ITransacaoService
    {
        void Executar(Action acao);

        TResult Executar<TResult>(Func<TResult> acao);
    }
}
