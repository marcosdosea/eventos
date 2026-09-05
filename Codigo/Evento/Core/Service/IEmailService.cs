namespace Core.Service
{
    public interface IEmailService{
        Task<bool> ModeloEmailReset(String token, Pessoa pessoa, String callbackUrl);
    }
}
