using Microsoft.AspNetCore.Http;

namespace Core.Service
{
    public interface IModelocrachaService
    {
        int Create(Modelocracha modelocracha);
        void Edit(Modelocracha modelocracha);
        void Delete(int id);
        Modelocracha Get(int id);
        IEnumerable<Modelocracha> GetAll();
        bool IsImage(IFormFile modelocrachaModelLogotipoFile);
    }
}
