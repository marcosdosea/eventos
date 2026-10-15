namespace Core.Service
{
    public interface IEmailService{
        Task<bool> ModeloEmailReset(String token, Pessoa pessoa, String callbackUrl);
        Task<bool> ModeloConfirmEmail(String token, Pessoa pessoa, String callbackUrl);

        Task<bool> ModeloEmail(String token, Pessoa pessoa, String callbackUrl, string email, string contato, string assunto, string caminhoTemplate);
    }
}
